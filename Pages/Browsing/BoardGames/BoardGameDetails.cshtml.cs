using Board_Game_Software.Data;
using Board_Game_Software.Models;
using Board_Game_Software.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Board_Game_Software.Pages.Browsing.BoardGames
{
    public class BoardGameDetailsModel : PageModel
    {
        private readonly BoardGameDbContext _context;
        private readonly IMongoCollection<BoardGameImages> _imagesCollection;
        private readonly Microsoft.AspNetCore.Identity.UserManager<IdentityUser> _userManager;
        private readonly ICurrentClubService _currentClubService;

        public BoardGameDetailsModel(
            BoardGameDbContext context,
            IMongoClient mongoClient,
            IConfiguration configuration,
            Microsoft.AspNetCore.Identity.UserManager<IdentityUser> userManager,
            ICurrentClubService currentClubService)
        {
            _context = context;
            _userManager = userManager;
            _currentClubService = currentClubService;
            var databaseName = configuration["MongoDbSettings:Database"];
            var database = mongoClient.GetDatabase(databaseName);
            _imagesCollection = database.GetCollection<BoardGameImages>("BoardGameImages");
        }

        public BoardGame BoardGame { get; set; } = default!;
        public string BoardGameFrontImageUrl { get; set; } = string.Empty;
        public Dictionary<long, string?> MarkerImagesBase64 { get; set; } = new();

        public decimal AverageRating { get; set; }
        public int TotalRatings { get; set; }
        public decimal? UserRating { get; set; }
        public long? CurrentUserClaimedPlayerId { get; set; }
        public string? EloMethodName { get; set; }
        public List<BoardGame> Expansions { get; set; } = new();
        public List<BoardGame> BaseGamesForExpansion { get; set; } = new();
        public List<ExpansionMarkerDisplay> ExpansionMarkers { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null) return NotFound();

            var boardGame = await _context.BoardGames
                .Include(bg => bg.FkBgdBoardGameTypeNavigation)
                .Include(bg => bg.FkBgdBoardGameVictoryConditionTypeNavigation)
                .Include(bg => bg.FkBgdPublisherNavigation)
                .Include(bg => bg.BoardGameMarkers)
                    .ThenInclude(marker => marker.FkBgdBoardGameMarkerTypeNavigation)
                .Include(bg => bg.BoardGameShelfSections)
                    .ThenInclude(ss => ss.FkBgdShelfSectionNavigation)
                .Include(bg => bg.PlayerBoardGameStarRatings)
                .Include(bg => bg.BoardGameEloMethods)
                    .ThenInclude(bem => bem.FkBgdEloMethodNavigation)
                .Include(bg => bg.BoardGameExpansionBaseGames)
                    .ThenInclude(link => link.FkBgdExpansionBoardGameNavigation)
                .Include(bg => bg.BoardGameExpansionExpansionGames)
                    .ThenInclude(link => link.FkBgdBoardGameNavigation)
                .FirstOrDefaultAsync(bg => bg.Id == id);

            if (boardGame == null) return NotFound();
            var currentClub = await _currentClubService.GetCurrentClubAsync();
            if (!CanViewGame(boardGame, currentClub)) return NotFound();

            BoardGame = boardGame;

            Expansions = BoardGame.BoardGameExpansionBaseGames
                .Where(link => !link.Inactive && !link.FkBgdExpansionBoardGameNavigation.Inactive)
                .Select(link => link.FkBgdExpansionBoardGameNavigation)
                .OrderByDescending(game => game.PlayerCountMax ?? 0)
                .ThenBy(game => game.BoardGameName)
                .ToList();

            BaseGamesForExpansion = BoardGame.BoardGameExpansionExpansionGames
                .Where(link => !link.Inactive && !link.FkBgdBoardGameNavigation.Inactive)
                .Select(link => link.FkBgdBoardGameNavigation)
                .OrderBy(game => game.BoardGameName)
                .ToList();

            if (Expansions.Any())
            {
                var expansionIds = Expansions.Select(expansion => expansion.Id).ToList();

                ExpansionMarkers = await _context.BoardGameMarkers
                    .AsNoTracking()
                    .Include(marker => marker.FkBgdBoardGameMarkerTypeNavigation)
                    .Include(marker => marker.FkBgdBoardGameNavigation)
                    .Where(marker => !marker.Inactive && expansionIds.Contains(marker.FkBgdBoardGame))
                    .OrderBy(marker => marker.FkBgdBoardGameNavigation.BoardGameName)
                    .ThenBy(marker => marker.FkBgdBoardGameMarkerTypeNavigation!.TypeDesc)
                    .Select(marker => new ExpansionMarkerDisplay
                    {
                        Marker = marker,
                        ExpansionName = marker.FkBgdBoardGameNavigation.BoardGameName
                    })
                    .ToListAsync();
            }

            // Defensive check for Elo Method
            EloMethodName = BoardGame.BoardGameEloMethods?
                .FirstOrDefault(x => !x.Inactive)?
                .FkBgdEloMethodNavigation?.MethodName;

            // Calculate Community Stats
            var activeRatings = BoardGame.PlayerBoardGameStarRatings?.Where(r => !r.Inactive).ToList() ?? new();
            if (activeRatings.Any())
            {
                AverageRating = activeRatings.Average(r => r.StarRating);
                TotalRatings = activeRatings.Count;
            }

            // Identify User
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var player = await _context.Players.FirstOrDefaultAsync(p => p.FkdboAspNetUsers == user.Id);
                if (player != null)
                {
                    CurrentUserClaimedPlayerId = player.Id;
                    UserRating = activeRatings.FirstOrDefault(r => r.FkBgdPlayer == player.Id)?.StarRating;
                }
            }

            await LoadImages();
            return Page();
        }

        public async Task<IActionResult> OnPostRateAsync(long id, int ratingValue)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Forbid();

            var boardGame = await _context.BoardGames.AsNoTracking().FirstOrDefaultAsync(bg => bg.Id == id);
            if (boardGame == null) return NotFound();

            var currentClub = await _currentClubService.GetCurrentClubAsync();
            if (!CanViewGame(boardGame, currentClub)) return NotFound();

            var player = await _context.Players.FirstOrDefaultAsync(p => p.FkdboAspNetUsers == user.Id);
            if (player == null) return RedirectToPage(new { id });

            if (boardGame.FkBgdClub.HasValue)
            {
                var canRateForClub = await _context.PlayerClubs
                    .AsNoTracking()
                    .AnyAsync(pc => !pc.Inactive
                        && pc.FkBgdPlayer == player.Id
                        && pc.FkBgdClub == boardGame.FkBgdClub.Value);

                if (!canRateForClub && !User.IsInRole("Admin")) return Forbid();
            }

            var existing = await _context.PlayerBoardGameStarRatings
                .FirstOrDefaultAsync(r => r.FkBgdBoardGame == id && r.FkBgdPlayer == player.Id);

            string currentUserName = user.UserName ?? "System";

            if (existing != null)
            {
                existing.StarRating = (decimal)ratingValue;
                existing.ModifiedBy = currentUserName;
                existing.TimeModified = DateTime.Now;
                existing.Inactive = false;
            }
            else
            {
                _context.PlayerBoardGameStarRatings.Add(new PlayerBoardGameStarRating
                {
                    FkBgdBoardGame = id,
                    FkBgdPlayer = player.Id,
                    StarRating = (decimal)ratingValue,
                    Gid = Guid.NewGuid(),
                    CreatedBy = currentUserName,
                    ModifiedBy = currentUserName,
                    TimeCreated = DateTime.Now,
                    TimeModified = DateTime.Now,
                    Inactive = false
                });
            }

            await _context.SaveChangesAsync();
            return RedirectToPage(new { id });
        }

        private async Task LoadImages()
        {
            if (BoardGame.Gid != Guid.Empty)
                BoardGameFrontImageUrl = $"/media/boardgame/front/{BoardGame.Gid:D}";

            var markersForImages = BoardGame.BoardGameMarkers
                .Where(marker => !marker.Inactive)
                .Concat(ExpansionMarkers.Select(marker => marker.Marker))
                .ToList();

            if (markersForImages.Any())
            {
                var markerTypeGids = markersForImages
                   .Select(m => (Guid?)m.FkBgdBoardGameMarkerTypeNavigation?.Gid)
                   .Where(g => g.HasValue).Distinct().ToList();

                var filter = Builders<BoardGameImages>.Filter.And(
                    Builders<BoardGameImages>.Filter.Eq(x => x.SQLTable, "bgd.BoardGameMarkerType"),
                    Builders<BoardGameImages>.Filter.In(x => x.GID, markerTypeGids));

                var images = await _imagesCollection.Find(filter).ToListAsync();

                foreach (var marker in markersForImages)
                {
                    var type = marker.FkBgdBoardGameMarkerTypeNavigation;
                    if (type != null && !MarkerImagesBase64.ContainsKey(type.Id))
                    {
                        var img = images.FirstOrDefault(x => x.GID == type.Gid);
                        if (img?.ImageBytes != null)
                            MarkerImagesBase64[type.Id] = $"data:{img.ContentType};base64,{Convert.ToBase64String(img.ImageBytes)}";
                    }
                }
            }
        }

        public string GetInitials(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "?";
            var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 1 ? (parts[0][0].ToString() + parts[1][0].ToString()).ToUpper() : text.Substring(0, Math.Min(2, text.Length)).ToUpper();
        }

        public string GetAvatarColor(string? name)
        {
            if (string.IsNullOrEmpty(name)) return "#6c757d";
            int hash = name.GetHashCode();
            var colors = new[] { "#d32f2f", "#7b1fa2", "#303f9f", "#1976d2", "#00796b", "#388e3c", "#ffa000", "#e64a19" };
            return colors[Math.Abs(hash) % colors.Length];
        }

        private bool CanViewGame(BoardGame boardGame, CurrentClubContext currentClub)
        {
            if (User.IsInRole("Admin") && currentClub.IsPlatformAdminMode)
            {
                return boardGame.FkBgdClub == null;
            }

            if (currentClub.CurrentClubId.HasValue)
            {
                return boardGame.FkBgdClub == currentClub.CurrentClubId.Value;
            }

            return User.IsInRole("Admin");
        }
    }

    public class ExpansionMarkerDisplay
    {
        public BoardGameMarker Marker { get; set; } = null!;
        public string ExpansionName { get; set; } = string.Empty;
    }
}

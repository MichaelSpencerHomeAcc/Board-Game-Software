using Board_Game_Software.Data;
using Board_Game_Software.Models;
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

        public BoardGameDetailsModel(
            BoardGameDbContext context,
            IMongoClient mongoClient,
            IConfiguration configuration,
            Microsoft.AspNetCore.Identity.UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null) return NotFound();

            BoardGame = await _context.BoardGames
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
                .FirstOrDefaultAsync(bg => bg.Id == id);

            if (BoardGame == null) return NotFound();

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

            var player = await _context.Players.FirstOrDefaultAsync(p => p.FkdboAspNetUsers == user.Id);
            if (player == null) return RedirectToPage(new { id });

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
            var frontImageType = await _context.BoardGameImageTypes
                .FirstOrDefaultAsync(bgit => bgit.TypeDesc == "Board Game Front");

            if (frontImageType != null && BoardGame.Gid != Guid.Empty)
            {
                var image = await _imagesCollection.Find(img =>
                    img.GID == BoardGame.Gid && img.ImageTypeGID == frontImageType.Gid)
                    .FirstOrDefaultAsync();

                if (image?.ImageBytes != null)
                    BoardGameFrontImageUrl = $"data:{image.ContentType};base64,{Convert.ToBase64String(image.ImageBytes)}";
            }

            if (BoardGame.BoardGameMarkers?.Any() == true)
            {
                var markerTypeGids = BoardGame.BoardGameMarkers
                   .Select(m => (Guid?)m.FkBgdBoardGameMarkerTypeNavigation?.Gid)
                   .Where(g => g.HasValue).Distinct().ToList();

                var filter = Builders<BoardGameImages>.Filter.And(
                    Builders<BoardGameImages>.Filter.Eq(x => x.SQLTable, "bgd.BoardGameMarkerType"),
                    Builders<BoardGameImages>.Filter.In(x => x.GID, markerTypeGids));

                var images = await _imagesCollection.Find(filter).ToListAsync();

                foreach (var marker in BoardGame.BoardGameMarkers)
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
    }
}
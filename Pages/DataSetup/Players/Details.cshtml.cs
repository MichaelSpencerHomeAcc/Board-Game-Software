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

namespace Board_Game_Software.Pages.DataSetup.Players
{
    public class DetailsModel : PageModel
    {
        private readonly BoardGameDbContext _context;
        private readonly IMongoCollection<BoardGameImages> _imagesCollection;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ICurrentClubService _currentClubService;

        public DetailsModel(
            BoardGameDbContext context,
            IMongoClient mongoClient,
            IConfiguration configuration,
            UserManager<IdentityUser> userManager,
            ICurrentClubService currentClubService)
        {
            _context = context;
            _userManager = userManager;
            _currentClubService = currentClubService;

            var dbName = configuration["MongoDbSettings:Database"];
            var database = mongoClient.GetDatabase(dbName);
            _imagesCollection = database.GetCollection<BoardGameImages>("BoardGameImages");
        }

        public Player Player { get; set; } = null!;

        // FAST: no base64 – just use media endpoint
        public string ProfileImageUrl { get; set; } = string.Empty;
        public bool HasProfileImage { get; set; }

        public bool CanEdit { get; set; }

        public List<PlayerBoardGame> TopTenGames { get; set; } = new();
        public Dictionary<long, string> GameImages { get; set; } = new(); // entryId -> image url
        public PlayerProfileStats ProfileStats { get; private set; } = new();
        public List<ProfileBadgeRow> Badges { get; private set; } = new();
        public List<RecentFormRow> RecentForm { get; private set; } = new();

        // Focus / zoom (defaults)
        public int AvatarX { get; set; } = 50;
        public int AvatarY { get; set; } = 50;
        public int AvatarZoom { get; set; } = 100;
        public int PodiumX { get; set; } = 50;
        public int PodiumY { get; set; } = 50;
        public int PodiumZoom { get; set; } = 100;

        public sealed class PlayerProfileStats
        {
            public int Matches { get; init; }
            public int Wins { get; init; }
            public string FavoriteGame { get; init; } = "None yet";
            public string Nemesis { get; init; } = "None yet";
            public string BestTeammate { get; init; } = "None yet";
            public string StrongestType { get; init; } = "Unknown";
            public decimal BestRatingGain { get; init; }
        }

        public sealed class ProfileBadgeRow
        {
            public string Title { get; init; } = string.Empty;
            public string Detail { get; init; } = string.Empty;
            public DateTime UnlockedAt { get; init; }
        }

        public sealed class RecentFormRow
        {
            public string GameName { get; init; } = string.Empty;
            public DateTime? FinishedDate { get; init; }
            public bool Won { get; init; }
            public decimal? RatingChange { get; init; }
        }

        public async Task<IActionResult> OnGetAsync(long id)
        {
            Player = await _context.Players
                .AsNoTracking()
                .Include(p => p.PlayerBoardGames)
                    .ThenInclude(pbg => pbg.BoardGame)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Player == null) return NotFound();

            // SECURITY CHECK: Admin or owner
            var currentUserId = _userManager.GetUserId(User);
            var currentClub = await _currentClubService.GetCurrentClubAsync();
            var playerClubIds = await _context.PlayerClubs
                .AsNoTracking()
                .Where(pc => pc.FkBgdPlayer == id && !pc.Inactive)
                .Select(pc => pc.FkBgdClub)
                .ToListAsync();
            var belongsToAvailableClub = currentClub.AvailableClubs.Any(c => playerClubIds.Contains(c.ClubId));
            var belongsToManageableClub = currentClub.AvailableClubs.Any(c => playerClubIds.Contains(c.ClubId) && c.Role is "Owner" or "Admin");
            var isAccountOwner = Player.FkdboAspNetUsers == currentUserId;

            if (!User.IsInRole("Admin") && !isAccountOwner && !belongsToAvailableClub)
            {
                return Forbid();
            }

            CanEdit = User.IsInRole("Admin") || isAccountOwner || belongsToManageableClub;

            // Top 10 list
            TopTenGames = Player.PlayerBoardGames
                .Where(x => !x.Inactive)
                .OrderBy(x => x.Rank)
                .Take(10)
                .ToList();

            // 1) Mongo: load player image doc (for focus + "has image")
            await LoadPlayerImageMeta(Player.Gid);

            // 2) Game images: just point to your media endpoint (fast)
            GameImages.Clear();
            foreach (var entry in TopTenGames)
            {
                if (entry.BoardGame == null) continue;
                GameImages[entry.Id] = $"/media/boardgame/front/{entry.BoardGame.Gid}";
            }

            await LoadProfile2Async(id);

            return Page();
        }

        private async Task LoadProfile2Async(long playerId)
        {
            var rows = await _context.BoardGameMatchPlayerResults.AsNoTracking()
                .Where(r => !r.Inactive
                    && r.FkBgdBoardGameMatchPlayerNavigation.FkBgdPlayer == playerId
                    && r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.MatchComplete == true)
                .Select(r => new
                {
                    MatchId = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatch,
                    GameName = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.FkBgdBoardGameNavigation.BoardGameName ?? "Unknown game",
                    GameType = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.FkBgdBoardGameNavigation.FkBgdBoardGameTypeNavigation == null
                        ? null
                        : r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.FkBgdBoardGameNavigation.FkBgdBoardGameTypeNavigation.TypeDesc,
                    FinishedDate = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.FinishedDate,
                    r.Win,
                    r.RatingChangeMu,
                    r.FinalTeam
                })
                .ToListAsync();

            var matchIds = rows.Select(r => r.MatchId).Distinct().ToList();
            var otherPlayers = await _context.BoardGameMatchPlayers.AsNoTracking()
                .Where(mp => matchIds.Contains(mp.FkBgdBoardGameMatch) && mp.FkBgdPlayer != playerId && !mp.Inactive)
                .Select(mp => new
                {
                    mp.FkBgdBoardGameMatch,
                    mp.FkBgdPlayer,
                    Name = ((mp.FkBgdPlayerNavigation.FirstName ?? string.Empty) + " " + (mp.FkBgdPlayerNavigation.LastName ?? string.Empty)).Trim(),
                    Result = mp.BoardGameMatchPlayerResults.Where(r => !r.Inactive).Select(r => new { r.Win, r.FinalTeam }).FirstOrDefault()
                })
                .ToListAsync();

            var lossesByOpponent = new Dictionary<long, (string Name, int Count)>();
            var teammateWins = new Dictionary<long, (string Name, int Count)>();

            foreach (var row in rows)
            {
                var others = otherPlayers.Where(o => o.FkBgdBoardGameMatch == row.MatchId).ToList();
                if (!row.Win)
                {
                    foreach (var winner in others.Where(o => o.Result?.Win == true))
                    {
                        var current = lossesByOpponent.GetValueOrDefault(winner.FkBgdPlayer);
                        lossesByOpponent[winner.FkBgdPlayer] = (winner.Name, current.Count + 1);
                    }
                }

                if (row.Win && row.FinalTeam.HasValue)
                {
                    foreach (var teammate in others.Where(o => o.Result?.FinalTeam == row.FinalTeam))
                    {
                        var current = teammateWins.GetValueOrDefault(teammate.FkBgdPlayer);
                        teammateWins[teammate.FkBgdPlayer] = (teammate.Name, current.Count + 1);
                    }
                }
            }

            ProfileStats = new PlayerProfileStats
            {
                Matches = rows.Select(r => r.MatchId).Distinct().Count(),
                Wins = rows.Count(r => r.Win),
                FavoriteGame = rows.GroupBy(r => r.GameName).OrderByDescending(g => g.Count()).ThenBy(g => g.Key).Select(g => g.Key).FirstOrDefault() ?? "None yet",
                Nemesis = lossesByOpponent.OrderByDescending(x => x.Value.Count).Select(x => x.Value.Name).FirstOrDefault() ?? "None yet",
                BestTeammate = teammateWins.OrderByDescending(x => x.Value.Count).Select(x => x.Value.Name).FirstOrDefault() ?? "None yet",
                StrongestType = rows.Where(r => r.Win && !string.IsNullOrWhiteSpace(r.GameType)).GroupBy(r => r.GameType).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault() ?? "Unknown",
                BestRatingGain = rows.Select(r => r.RatingChangeMu ?? 0).DefaultIfEmpty(0).Max()
            };

            RecentForm = rows.OrderByDescending(r => r.FinishedDate).Take(6).Select(r => new RecentFormRow
            {
                GameName = r.GameName,
                FinishedDate = r.FinishedDate,
                Won = r.Win,
                RatingChange = r.RatingChangeMu
            }).ToList();

            Badges = await _context.PlayerAchievements.AsNoTracking()
                .Where(a => a.FkBgdPlayer == playerId && !a.Inactive)
                .OrderByDescending(a => a.UnlockedAt)
                .Take(8)
                .Select(a => new ProfileBadgeRow { Title = a.BadgeTitle, Detail = a.BadgeDetail, UnlockedAt = a.UnlockedAt })
                .ToListAsync();
        }

        private async Task LoadPlayerImageMeta(Guid playerGid)
        {
            ProfileImageUrl = $"/media/player/{playerGid}";
            HasProfileImage = false;

            var filter = Builders<BoardGameImages>.Filter.And(
                Builders<BoardGameImages>.Filter.Eq(x => x.SQLTable, "bgd.Player"),
                Builders<BoardGameImages>.Filter.Eq(x => x.GID, (Guid?)playerGid)
            );

            // Only pull the metadata we need (fast)
            var doc = await _imagesCollection
                .Find(filter)
                .Project(x => new
                {
                    x.ImageBytes,
                    x.AvatarFocusX,
                    x.AvatarFocusY,
                    x.AvatarZoom,
                    x.PodiumFocusX,
                    x.PodiumFocusY,
                    x.PodiumZoom
                })
                .FirstOrDefaultAsync();

            if (doc == null) return;

            HasProfileImage = doc.ImageBytes != null && doc.ImageBytes.Length > 0;

            // If your Mongo fields are non-nullable ints, these assignments are safe.
            // If any can be 0 / unset, clamp them to sensible ranges.
            AvatarX = ClampPct(doc.AvatarFocusX, 50);
            AvatarY = ClampPct(doc.AvatarFocusY, 50);
            AvatarZoom = ClampZoom(doc.AvatarZoom, 100);

            PodiumX = ClampPct(doc.PodiumFocusX, 50);
            PodiumY = ClampPct(doc.PodiumFocusY, 50);
            PodiumZoom = ClampZoom(doc.PodiumZoom, 100);
        }

        public async Task<IActionResult> OnPostUpdateFocusAsync(
            long id,
            int AvatarX, int AvatarY, int AvatarZoom,
            int PodiumX, int PodiumY, int PodiumZoom)
        {
            var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == id);
            if (player == null) return NotFound();

            // SECURITY RE-CHECK
            var currentUserId = _userManager.GetUserId(User);
            var currentClub = await _currentClubService.GetCurrentClubAsync();
            var playerClubIds = await _context.PlayerClubs
                .AsNoTracking()
                .Where(pc => pc.FkBgdPlayer == id && !pc.Inactive)
                .Select(pc => pc.FkBgdClub)
                .ToListAsync();
            var canManageClubPlayer = currentClub.AvailableClubs.Any(c => playerClubIds.Contains(c.ClubId) && c.Role is "Owner" or "Admin");
            if (!User.IsInRole("Admin") && player.FkdboAspNetUsers != currentUserId && !canManageClubPlayer)
                return Forbid();

            var filter = Builders<BoardGameImages>.Filter.And(
                Builders<BoardGameImages>.Filter.Eq(x => x.SQLTable, "bgd.Player"),
                Builders<BoardGameImages>.Filter.Eq(x => x.GID, (Guid?)player.Gid)
            );

            var update = Builders<BoardGameImages>.Update
                .Set(x => x.AvatarFocusX, ClampPct(AvatarX, 50))
                .Set(x => x.AvatarFocusY, ClampPct(AvatarY, 50))
                .Set(x => x.AvatarZoom, ClampZoom(AvatarZoom, 100))
                .Set(x => x.PodiumFocusX, ClampPct(PodiumX, 50))
                .Set(x => x.PodiumFocusY, ClampPct(PodiumY, 50))
                .Set(x => x.PodiumZoom, ClampZoom(PodiumZoom, 100));

            await _imagesCollection.UpdateOneAsync(filter, update);

            return RedirectToPage(new { id });
        }

        private static int ClampPct(int v, int fallback)
        {
            if (v <= 0) return fallback;
            if (v > 100) return 100;
            return v;
        }

        private static int ClampZoom(int v, int fallback)
        {
            if (v <= 0) return fallback;
            if (v < 100) return 100;
            if (v > 300) return 300;
            return v;
        }

        public string GetInitials(string? f, string? l)
            => $"{(f?.Length > 0 ? f[0] : ' ')}{(l?.Length > 0 ? l[0] : ' ')}"
                .ToUpper()
                .Trim();

        public string GetAvatarColor(string? name)
        {
            if (string.IsNullOrEmpty(name)) return "#6c757d";
            int hash = name.GetHashCode();
            var colors = new[] { "#d32f2f", "#7b1fa2", "#303f9f", "#1976d2", "#00796b", "#388e3c", "#ffa000", "#e64a19" };
            return colors[Math.Abs(hash) % colors.Length];
        }
    }
}

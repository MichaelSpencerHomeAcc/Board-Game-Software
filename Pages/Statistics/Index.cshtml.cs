using Board_Game_Software.Data;
using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Board_Game_Software.Pages.Statistics
{
    public class IndexModel : PageModel
    {
        private readonly BoardGameDbContext _context;
        private readonly IMongoCollection<BoardGameImages> _imagesCollection;

        public IndexModel(BoardGameDbContext context, IMongoClient mongoClient, IConfiguration configuration)
        {
            _context = context;
            var databaseName = configuration["MongoDbSettings:Database"];
            _imagesCollection = mongoClient.GetDatabase(databaseName).GetCollection<BoardGameImages>("BoardGameImages");
        }

        public List<HighScoreEntry> GlobalRecords { get; set; } = new();
        public List<HighScoreEntry> PersonalBests { get; set; } = new();
        public string? PlayerProfileImage { get; set; }
        public SelectList PlayerList { get; set; } = default!;
        public SelectList YearList { get; set; } = default!;
        public SelectList MonthList { get; set; } = default!;

        [BindProperty(SupportsGet = true)] public long? SelectedPlayerId { get; set; }
        [BindProperty(SupportsGet = true)] public int? SelectedYear { get; set; }
        [BindProperty(SupportsGet = true)] public int? SelectedMonth { get; set; }

        public async Task OnGetAsync()
        {
            // Players dropdown (no tracking)
            var players = await _context.Players
                .AsNoTracking()
                .Where(p => !p.Inactive)
                .Select(p => new
                {
                    p.Id,
                    Name = (p.FirstName ?? "") + " " + (p.LastName ?? "")
                })
                .OrderBy(p => p.Name)
                .ToListAsync();

            PlayerList = new SelectList(players, "Id", "Name");

            // Year / Month dropdowns (no tracking)
            var years = await _context.BoardGameMatchPlayerResults
                .AsNoTracking()
                .Select(r => r.TimeCreated.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();

            YearList = new SelectList(years);

            MonthList = new SelectList(
                Enumerable.Range(1, 12).Select(m => new
                {
                    Value = m,
                    Text = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m)
                }),
                "Value", "Text"
            );

            // Base query for point-based games only (no tracking)
            var baseQuery = _context.BoardGameMatchPlayerResults
                .AsNoTracking()
                .Where(r =>
                    r.FkBgdBoardGameMatchPlayerNavigation
                     .FkBgdBoardGameMatchNavigation
                     .FkBgdBoardGameNavigation
                     .FkBgdBoardGameVictoryConditionTypeNavigation.Points == true
                );

            if (SelectedYear.HasValue)
                baseQuery = baseQuery.Where(r => r.TimeCreated.Year == SelectedYear.Value);

            if (SelectedMonth.HasValue)
                baseQuery = baseQuery.Where(r => r.TimeCreated.Month == SelectedMonth.Value);

            // --------------------------
            // 1) GLOBAL RECORDS (Top score per game)
            // --------------------------

            // Pull only the fields we need (avoid dragging whole entities)
            var globalRows = await baseQuery
                .Select(r => new HighScoreEntry
                {
                    GameId = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.FkBgdBoardGame,
                    GameGid = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.FkBgdBoardGameNavigation.Gid,
                    GameName = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.FkBgdBoardGameNavigation.BoardGameName,
                    PlayerName =
                        (r.FkBgdBoardGameMatchPlayerNavigation.FkBgdPlayerNavigation.FirstName ?? "") + " " +
                        (r.FkBgdBoardGameMatchPlayerNavigation.FkBgdPlayerNavigation.LastName ?? ""),
                    PlayerGid = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdPlayerNavigation.Gid,
                    Score = r.FinalScore ?? 0,
                    Date = r.TimeCreated
                })
                .ToListAsync();

            // Top per game in-memory (still OK after trimming columns; next step would be window funcs)
            var topGlobal = globalRows
                .GroupBy(x => x.GameId)
                .Select(g => g.OrderByDescending(x => x.Score).ThenByDescending(x => x.Date).First())
                .OrderBy(x => x.GameName)
                .ToList();

            // Batch load player images for global records
            var globalPlayerGids = topGlobal
                .Where(x => x.PlayerGid.HasValue)
                .Select(x => x.PlayerGid!.Value)
                .Distinct()
                .ToList();

            if (globalPlayerGids.Any())
            {
                var globalImages = await _imagesCollection.Find(Builders<BoardGameImages>.Filter.And(
                    Builders<BoardGameImages>.Filter.Eq(x => x.SQLTable, "bgd.Player"),
                    Builders<BoardGameImages>.Filter.In(x => x.GID, globalPlayerGids.Select(g => (Guid?)g))
                )).ToListAsync();

                foreach (var entry in topGlobal)
                {
                    if (!entry.PlayerGid.HasValue) continue;

                    var img = globalImages.FirstOrDefault(i => i.GID == entry.PlayerGid);
                    if (img?.ImageBytes != null)
                    {
                        entry.PlayerBase64Image = $"data:{img.ContentType};base64,{Convert.ToBase64String(img.ImageBytes)}";
                        entry.AvatarFocusX = img.AvatarFocusX;
                        entry.AvatarFocusY = img.AvatarFocusY;
                        entry.AvatarZoom = img.AvatarZoom;
                    }
                }
            }

            GlobalRecords = topGlobal;

            // --------------------------
            // 2) PERSONAL BESTS
            // --------------------------
            if (!SelectedPlayerId.HasValue)
                return;

            // Load the selected player's profile image (no tracking)
            var player = await _context.Players
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == SelectedPlayerId.Value);

            if (player != null)
                await LoadPlayerImage(player.Gid);

            // Player’s scores in filtered window
            var pScores = await baseQuery
                .Where(r => r.FkBgdBoardGameMatchPlayerNavigation.FkBgdPlayer == SelectedPlayerId.Value)
                .Select(r => new HighScoreEntry
                {
                    GameId = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.FkBgdBoardGame,
                    GameGid = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.FkBgdBoardGameNavigation.Gid,
                    GameName = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.FkBgdBoardGameNavigation.BoardGameName,
                    Score = r.FinalScore ?? 0,
                    Date = r.TimeCreated
                })
                .ToListAsync();

            var bestPerGame = pScores
                .GroupBy(x => x.GameId)
                .Select(g => g.OrderByDescending(x => x.Score).ThenByDescending(x => x.Date).First())
                .ToList();

            if (!bestPerGame.Any())
            {
                PersonalBests = new List<HighScoreEntry>();
                return;
            }

            // Only compute ranking for games that matter (BIG speed win vs whole DB)
            var gameIds = bestPerGame.Select(b => b.GameId).Distinct().ToList();

            // For those games, compute best score per player per game
            var bestScoresAllPlayersForThoseGames = await baseQuery
                .Where(r => gameIds.Contains(
                    r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.FkBgdBoardGame
                ))
                .Select(r => new
                {
                    GameId = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.FkBgdBoardGame,
                    PlayerId = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdPlayer,
                    Score = r.FinalScore ?? 0
                })
                .ToListAsync();

            // Pre-group once (avoid repeated Where/OrderBy in loop)
            var perGameGroups = bestScoresAllPlayersForThoseGames
                .GroupBy(x => x.GameId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Batch load BoardGame Front images for personal bests in ONE Mongo query
            var frontType = await _context.BoardGameImageTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TypeDesc == "Board Game Front");

            if (frontType != null)
            {
                var gameGids = bestPerGame.Select(b => (Guid?)b.GameGid).Distinct().ToList();

                var gameImgs = await _imagesCollection.Find(Builders<BoardGameImages>.Filter.And(
                    Builders<BoardGameImages>.Filter.In(x => x.GID, gameGids),
                    Builders<BoardGameImages>.Filter.Eq(x => x.ImageTypeGID, frontType.Gid)
                )).ToListAsync();

                var imgByGid = gameImgs
                    .Where(i => i.GID.HasValue)
                    .GroupBy(i => i.GID!.Value)
                    .ToDictionary(g => g.Key, g => g.First());

                foreach (var entry in bestPerGame)
                {
                    // Rank (1-based)
                    if (perGameGroups.TryGetValue(entry.GameId, out var list))
                    {
                        var ordered = list.OrderByDescending(x => x.Score).ToList();
                        entry.Rank = ordered.FindIndex(x => x.PlayerId == SelectedPlayerId.Value) + 1;
                    }

                    if (imgByGid.TryGetValue(entry.GameGid, out var img) && img?.ImageBytes != null)
                    {
                        entry.Base64Image = $"data:{img.ContentType};base64,{Convert.ToBase64String(img.ImageBytes)}";
                    }
                }
            }
            else
            {
                // Still compute ranks even if frontType missing
                foreach (var entry in bestPerGame)
                {
                    if (perGameGroups.TryGetValue(entry.GameId, out var list))
                    {
                        var ordered = list.OrderByDescending(x => x.Score).ToList();
                        entry.Rank = ordered.FindIndex(x => x.PlayerId == SelectedPlayerId.Value) + 1;
                    }
                }
            }

            PersonalBests = bestPerGame.OrderBy(x => x.Rank).ToList();
        }

        private async Task LoadPlayerImage(Guid gid)
        {
            var img = await _imagesCollection
                .Find(i => i.SQLTable == "bgd.Player" && i.GID == gid)
                .FirstOrDefaultAsync();

            if (img?.ImageBytes != null)
                PlayerProfileImage = $"data:{img.ContentType};base64,{Convert.ToBase64String(img.ImageBytes)}";
        }

        public class HighScoreEntry
        {
            public long GameId { get; set; }
            public Guid GameGid { get; set; }
            public Guid? PlayerGid { get; set; }
            public string GameName { get; set; } = "";
            public string PlayerName { get; set; } = "";
            public decimal Score { get; set; }
            public DateTime Date { get; set; }
            public int Rank { get; set; }
            public string? Base64Image { get; set; }
            public string? PlayerBase64Image { get; set; }
            public int AvatarFocusX { get; set; } = 50;
            public int AvatarFocusY { get; set; } = 50;
            public int AvatarZoom { get; set; } = 100;
        }
    }
}

using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

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
            var players = await _context.Players.Where(p => !p.Inactive)
                .Select(p => new { p.Id, Name = p.FirstName + " " + p.LastName }).OrderBy(p => p.Name).ToListAsync();
            PlayerList = new SelectList(players, "Id", "Name");

            var years = await _context.BoardGameMatchPlayerResults.Select(r => r.TimeCreated.Year).Distinct().OrderByDescending(y => y).ToListAsync();
            YearList = new SelectList(years);
            MonthList = new SelectList(Enumerable.Range(1, 12).Select(m => new { Value = m, Text = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m) }), "Value", "Text");

            var baseQuery = _context.BoardGameMatchPlayerResults
                .Where(r => r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.FkBgdBoardGameNavigation.FkBgdBoardGameVictoryConditionTypeNavigation.Points == true);

            if (SelectedYear.HasValue) baseQuery = baseQuery.Where(r => r.TimeCreated.Year == SelectedYear.Value);
            if (SelectedMonth.HasValue) baseQuery = baseQuery.Where(r => r.TimeCreated.Month == SelectedMonth.Value);

            // 1. Global Records - Fetching PlayerGid for image lookup
            var globalScores = await baseQuery.Select(r => new HighScoreEntry
            {
                GameId = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.FkBgdBoardGame,
                GameName = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.FkBgdBoardGameNavigation.BoardGameName,
                PlayerName = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdPlayerNavigation.FirstName + " " + r.FkBgdBoardGameMatchPlayerNavigation.FkBgdPlayerNavigation.LastName,
                PlayerGid = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdPlayerNavigation.Gid,
                Score = r.FinalScore ?? 0,
                Date = r.TimeCreated
            }).ToListAsync();

            var topGlobal = globalScores.GroupBy(x => x.GameId)
                .Select(g => g.OrderByDescending(x => x.Score).First())
                .OrderBy(x => x.GameName).ToList();

            // Batch Load Images for Global Records
            var globalPlayerGids = topGlobal.Select(x => x.PlayerGid).Distinct().ToList();
            var globalImages = await _imagesCollection.Find(Builders<BoardGameImages>.Filter.And(
                Builders<BoardGameImages>.Filter.Eq(x => x.SQLTable, "bgd.Player"),
                Builders<BoardGameImages>.Filter.In(x => x.GID, globalPlayerGids.Select(g => (Guid?)g))
            )).ToListAsync();

            foreach (var entry in topGlobal)
            {
                var img = globalImages.FirstOrDefault(i => i.GID == entry.PlayerGid);
                if (img?.ImageBytes != null)
                {
                    entry.PlayerBase64Image = $"data:{img.ContentType};base64,{Convert.ToBase64String(img.ImageBytes)}";
                    entry.AvatarFocusX = img.AvatarFocusX;
                    entry.AvatarFocusY = img.AvatarFocusY;
                    entry.AvatarZoom = img.AvatarZoom;
                }
            }
            GlobalRecords = topGlobal;

            // 2. Personnel Profile logic
            if (SelectedPlayerId.HasValue)
            {
                var player = await _context.Players.FindAsync(SelectedPlayerId);
                if (player != null) await LoadPlayerImage(player.Gid);

                var allScoresForRanking = await _context.BoardGameMatchPlayerResults
                    .Where(r => r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.FkBgdBoardGameNavigation.FkBgdBoardGameVictoryConditionTypeNavigation.Points == true)
                    .Select(r => new {
                        FkBgdBoardGame = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.FkBgdBoardGame,
                        r.FkBgdBoardGameMatchPlayerNavigation.FkBgdPlayer,
                        Score = r.FinalScore ?? 0
                    }).ToListAsync();

                var pScores = await baseQuery.Where(r => r.FkBgdBoardGameMatchPlayerNavigation.FkBgdPlayer == SelectedPlayerId.Value)
                    .Select(r => new HighScoreEntry
                    {
                        GameId = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.FkBgdBoardGame,
                        GameGid = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.FkBgdBoardGameNavigation.Gid,
                        GameName = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.FkBgdBoardGameNavigation.BoardGameName,
                        Score = r.FinalScore ?? 0,
                        Date = r.TimeCreated
                    }).ToListAsync();

                var bestPerGame = pScores.GroupBy(x => x.GameId).Select(g => g.OrderByDescending(x => x.Score).First()).ToList();
                var frontType = await _context.BoardGameImageTypes.FirstOrDefaultAsync(t => t.TypeDesc == "Board Game Front");

                foreach (var entry in bestPerGame)
                {
                    var gameScores = allScoresForRanking.Where(s => s.FkBgdBoardGame == entry.GameId).OrderByDescending(s => s.Score).ToList();
                    entry.Rank = gameScores.FindIndex(s => s.FkBgdPlayer == SelectedPlayerId) + 1;

                    var img = await _imagesCollection.Find(i => i.GID == entry.GameGid && i.ImageTypeGID == frontType.Gid).FirstOrDefaultAsync();
                    if (img?.ImageBytes != null) entry.Base64Image = $"data:{img.ContentType};base64,{Convert.ToBase64String(img.ImageBytes)}";
                }
                PersonalBests = bestPerGame.OrderBy(x => x.Rank).ToList();
            }
        }

        private async Task LoadPlayerImage(Guid gid)
        {
            var img = await _imagesCollection.Find(i => i.SQLTable == "bgd.Player" && i.GID == gid).FirstOrDefaultAsync();
            if (img?.ImageBytes != null) PlayerProfileImage = $"data:image/png;base64,{Convert.ToBase64String(img.ImageBytes)}";
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
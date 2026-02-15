using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

namespace Board_Game_Software.Pages.Statistics
{
    public class GameRankingsModel : PageModel
    {
        private readonly BoardGameDbContext _context;
        private readonly IMongoCollection<BoardGameImages> _imagesCollection;

        public GameRankingsModel(BoardGameDbContext context, IMongoClient mongoClient, IConfiguration configuration)
        {
            _context = context;
            var databaseName = configuration["MongoDbSettings:Database"];
            _imagesCollection = mongoClient.GetDatabase(databaseName).GetCollection<BoardGameImages>("BoardGameImages");
        }

        public List<RankEntry> Leaderboard { get; set; } = new();
        public BoardGame Game { get; set; } = default!;
        public string? GameBoxArt { get; set; }
        public SelectList GameList { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public long? SelectedGameId { get; set; }

        public async Task OnGetAsync(long? id)
        {
            // 1. Setup Game Dropdown
            var games = await _context.BoardGames
                .Where(bg => !bg.Inactive && bg.FkBgdBoardGameVictoryConditionTypeNavigation.Points == true)
                .OrderBy(bg => bg.BoardGameName)
                .Select(bg => new { bg.Id, bg.BoardGameName })
                .ToListAsync();
            GameList = new SelectList(games, "Id", "BoardGameName");

            long? targetId = id ?? SelectedGameId;

            if (targetId.HasValue)
            {
                SelectedGameId = targetId;

                // 2. Fetch Game Info
                Game = await _context.BoardGames
                    .Include(bg => bg.FkBgdPublisherNavigation)
                    .FirstOrDefaultAsync(bg => bg.Id == targetId) ?? new BoardGame();

                await LoadGameImage(Game.Gid);

                // 3. Fetch Leaderboard - Corrected navigation path for Score
                var results = await _context.BoardGameMatchPlayerResults
                    .Where(r => r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.FkBgdBoardGame == targetId)
                    .Select(r => new RankEntry
                    {
                        PlayerId = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdPlayer,
                        PlayerGid = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdPlayerNavigation.Gid,
                        PlayerName = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdPlayerNavigation.FirstName + " " + r.FkBgdBoardGameMatchPlayerNavigation.FkBgdPlayerNavigation.LastName,
                        Score = r.FinalScore ?? 0,
                        Date = r.TimeCreated
                    })
                    .ToListAsync();

                Leaderboard = results
                    .GroupBy(r => r.PlayerId)
                    .Select(g => g.OrderByDescending(x => x.Score).First())
                    .OrderByDescending(x => x.Score)
                    .ToList();

                // 4. Load Player Profile Images & Focus Coordinates
                foreach (var entry in Leaderboard)
                {
                    await LoadPlayerDetails(entry);
                }
            }
        }

        private async Task LoadGameImage(Guid gid)
        {
            var type = await _context.BoardGameImageTypes.FirstOrDefaultAsync(t => t.TypeDesc == "Board Game Front");
            var img = await _imagesCollection.Find(i => i.GID == gid && i.ImageTypeGID == type.Gid).FirstOrDefaultAsync();
            if (img?.ImageBytes != null)
                GameBoxArt = $"data:{img.ContentType};base64,{Convert.ToBase64String(img.ImageBytes)}";
        }

        private async Task LoadPlayerDetails(RankEntry entry)
        {
            var img = await _imagesCollection.Find(i => i.SQLTable == "bgd.Player" && i.GID == entry.PlayerGid).FirstOrDefaultAsync();
            if (img != null && img.ImageBytes != null)
            {
                entry.ProfileImage = $"data:{img.ContentType};base64,{Convert.ToBase64String(img.ImageBytes)}";
                entry.FocusX = img.PodiumFocusX;
                entry.FocusY = img.PodiumFocusY;
                entry.Zoom = img.PodiumZoom;
                entry.AvatarX = img.AvatarFocusX;
                entry.AvatarY = img.AvatarFocusY;
                entry.AvatarZoom = img.AvatarZoom;
            }
        }

        public class RankEntry
        {
            public long PlayerId { get; set; }
            public Guid PlayerGid { get; set; }
            public string PlayerName { get; set; } = "";
            public decimal Score { get; set; }
            public DateTime Date { get; set; }
            public string? ProfileImage { get; set; }
            public int FocusX { get; set; } = 50;
            public int FocusY { get; set; } = 50;
            public int Zoom { get; set; } = 100;
            public int AvatarX { get; set; } = 50;
            public int AvatarY { get; set; } = 50;
            public int AvatarZoom { get; set; } = 100;
        }
    }
}
using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

namespace Board_Game_Software.Pages.GameNight
{
    public class AddPlayersModel : PageModel
    {
        private readonly BoardGameDbContext _db;
        private readonly IMongoCollection<BoardGameImages> _imagesCollection;

        public AddPlayersModel(BoardGameDbContext db, IMongoClient mongoClient, IConfiguration configuration)
        {
            _db = db;
            var databaseName = configuration["MongoDbSettings:Database"];
            var database = mongoClient.GetDatabase(databaseName);
            _imagesCollection = database.GetCollection<BoardGameImages>("BoardGameImages");
        }

        public List<PlayerRow> AllPlayers { get; set; } = new();

        [BindProperty]
        public AddInput Input { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(long id, string? returnUrl)
        {
            Input.NightId = id;
            Input.ReturnUrl = returnUrl;

            await LoadPlayersAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Input.SelectedPlayerIds == null || !Input.SelectedPlayerIds.Any())
            {
                return Redirect(Input.ReturnUrl ?? Url.Page("/GameNight/Details", new { id = Input.NightId }));
            }

            var now = DateTime.UtcNow;
            var userName = User?.Identity?.Name ?? "system";

            var links = Input.SelectedPlayerIds.Distinct().Select(pid => new BoardGameNightPlayer
            {
                Gid = Guid.NewGuid(),
                Inactive = false,
                TimeCreated = now,
                TimeModified = now,
                CreatedBy = userName,
                ModifiedBy = userName,
                FkBgdBoardGameNight = Input.NightId,
                FkBgdPlayer = pid
            });

            _db.AddRange(links);
            await _db.SaveChangesAsync();

            return Redirect(Input.ReturnUrl ?? Url.Page("/GameNight/Details", new { id = Input.NightId }));
        }

        private async Task LoadPlayersAsync()
        {
            // Get IDs already in this night to exclude them
            var existingIds = await _db.Set<BoardGameNightPlayer>()
                .Where(x => x.FkBgdBoardGameNight == Input.NightId && !x.Inactive)
                .Select(x => x.FkBgdPlayer)
                .ToListAsync();

            var players = await _db.Set<Player>()
                .Where(p => !p.Inactive && !existingIds.Contains(p.Id))
                .OrderBy(p => p.FirstName)
                .ToListAsync();

            // Pattern: Use .ToString() for MongoDB comparison
            var gidStrings = players.Select(x => x.Gid.ToString()).ToList();

            var imageDocs = await _imagesCollection.Find(img =>
                img.SQLTable == "bgd.Player" &&
                gidStrings.Contains(img.GID.ToString()))
                .ToListAsync();

            AllPlayers = players.Select(p =>
            {
                var playerGidStr = p.Gid.ToString();
                var imgDoc = imageDocs.FirstOrDefault(x => x.GID.ToString() == playerGidStr);

                return new PlayerRow
                {
                    PlayerId = p.Id,
                    Name = $"{p.FirstName} {p.LastName}".Trim(),
                    AvatarBase64 = imgDoc?.ImageBytes != null
                        ? $"data:image/png;base64,{Convert.ToBase64String(imgDoc.ImageBytes)}"
                        : null
                };
            }).ToList();
        }

        public class AddInput
        {
            public long NightId { get; set; }
            public string? ReturnUrl { get; set; }
            public List<long> SelectedPlayerIds { get; set; } = new();
        }

        public class PlayerRow
        {
            public long PlayerId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string? AvatarBase64 { get; set; }
            public bool Preselected { get; set; }
        }
    }
}
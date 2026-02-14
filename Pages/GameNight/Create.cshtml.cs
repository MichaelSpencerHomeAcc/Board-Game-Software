using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Board_Game_Software.Pages.GameNight
{
    public class CreateModel : PageModel
    {
        private readonly BoardGameDbContext _db;
        private readonly IMongoCollection<BoardGameImages> _imagesCollection;

        public CreateModel(BoardGameDbContext db, IMongoClient mongoClient, IConfiguration configuration)
        {
            _db = db;
            var databaseName = configuration["MongoDbSettings:Database"];
            var database = mongoClient.GetDatabase(databaseName);
            _imagesCollection = database.GetCollection<BoardGameImages>("BoardGameImages");
        }

        public List<PlayerRow> AllPlayers { get; private set; } = new();

        [BindProperty]
        public CreateInput Input { get; set; } = new();

        public async Task OnGetAsync()
        {
            Input.GameNightDate = DateOnly.FromDateTime(DateTime.Today);
            await LoadPlayersAndAvatarsAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Input.SelectedPlayerIds ??= new List<long>();

            if (!ModelState.IsValid)
            {
                await LoadPlayersAndAvatarsAsync();
                return Page();
            }

            var now = DateTime.UtcNow;
            var userName = User?.Identity?.Name ?? "system";

            var night = new BoardGameNight
            {
                Gid = Guid.NewGuid(),
                Inactive = false,
                GameNightDate = Input.GameNightDate,
                Finished = false,
                TimeCreated = now,
                TimeModified = now,
                CreatedBy = userName,
                ModifiedBy = userName
            };

            _db.Add(night);
            await _db.SaveChangesAsync();

            if (Input.SelectedPlayerIds.Any())
            {
                var links = Input.SelectedPlayerIds.Distinct().Select(pid => new BoardGameNightPlayer
                {
                    Gid = Guid.NewGuid(),
                    Inactive = false,
                    TimeCreated = now,
                    TimeModified = now,
                    CreatedBy = userName,
                    ModifiedBy = userName,
                    FkBgdBoardGameNight = night.Id,
                    FkBgdPlayer = pid
                });

                _db.AddRange(links);
                await _db.SaveChangesAsync();
            }

            return RedirectToPage("/GameNight/Details", new { id = night.Id });
        }

        private async Task LoadPlayersAndAvatarsAsync()
        {
            // 1. Get all active players from SQL
            var players = await _db.Set<Player>()
                .Where(p => !p.Inactive)
                .OrderBy(p => p.FirstName)
                .ThenBy(p => p.LastName)
                .ToListAsync();

            // 2. Prepare GID strings (Matching your DetailsModel pattern)
            // No .HasValue check needed since they aren't nullable
            var gidStrings = players
                .Select(x => x.Gid.ToString())
                .ToList();

            // 3. Query MongoDB using gidStrings.Contains(img.GID.ToString())
            // This matches the exact filter logic you linked
            var imageDocs = await _imagesCollection.Find(img =>
                img.SQLTable == "bgd.Player" &&
                gidStrings.Contains(img.GID.ToString()))
                .ToListAsync();

            // 4. Map to the View Model
            AllPlayers = players.Select(p => {
                var playerGidString = p.Gid.ToString();

                // Find the image by comparing GID strings
                var imgDoc = imageDocs.FirstOrDefault(x => x.GID.ToString() == playerGidString);

                string? base64 = null;
                if (imgDoc?.ImageBytes != null)
                {
                    base64 = $"data:image/png;base64,{Convert.ToBase64String(imgDoc.ImageBytes)}";
                }

                return new PlayerRow
                {
                    PlayerId = p.Id,
                    Name = ((p.FirstName ?? "") + " " + (p.LastName ?? "")).Trim(),
                    AvatarBase64 = base64,
                    Preselected = Input.SelectedPlayerIds != null && Input.SelectedPlayerIds.Contains(p.Id)
                };
            }).ToList();
        }

        public class CreateInput
        {
            [Required]
            public DateOnly GameNightDate { get; set; }
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
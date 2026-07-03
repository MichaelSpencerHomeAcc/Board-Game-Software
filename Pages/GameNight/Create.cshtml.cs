using Board_Game_Software.Models;
using Board_Game_Software.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System.ComponentModel.DataAnnotations;

namespace Board_Game_Software.Pages.GameNight
{
    public class CreateModel : PageModel
    {
        private readonly BoardGameDbContext _db;
        private readonly IMongoCollection<BoardGameImages> _imagesCollection;
        private readonly ICurrentClubService _currentClubService;

        public CreateModel(
            BoardGameDbContext db,
            IMongoClient mongoClient,
            IConfiguration configuration,
            ICurrentClubService currentClubService)
        {
            _db = db;
            _currentClubService = currentClubService;
            var databaseName = configuration["MongoDbSettings:Database"];
            var database = mongoClient.GetDatabase(databaseName);
            _imagesCollection = database.GetCollection<BoardGameImages>("BoardGameImages");
        }

        public List<PlayerRow> AllPlayers { get; private set; } = new();

        [BindProperty]
        public CreateInput Input { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.IsInRole("Admin") && !(await _currentClubService.GetCurrentClubAsync()).CurrentClubId.HasValue)
            {
                return Forbid();
            }

            Input.GameNightDate = DateOnly.FromDateTime(DateTime.Today);
            await LoadPlayersAndAvatarsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Input.SelectedPlayerIds ??= new List<long>();
            var currentClub = await _currentClubService.GetCurrentClubAsync();
            if (!User.IsInRole("Admin") && !currentClub.CurrentClubId.HasValue)
            {
                return Forbid();
            }

            var currentClubId = currentClub.CurrentClubId;
            Input.SelectedPlayerIds = await KeepCurrentClubPlayerIdsAsync(Input.SelectedPlayerIds, currentClubId);

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
                FkBgdClub = currentClubId,
                TimeCreated = now,
                TimeModified = now,
                CreatedBy = userName,
                ModifiedBy = userName
            };

            _db.Add(night);
            await _db.SaveChangesAsync();

            if (Input.SelectedPlayerIds.Any())
            {
                var links = Input.SelectedPlayerIds
                    .Distinct()
                    .Select(pid => new BoardGameNightPlayer
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
            var currentClub = await _currentClubService.GetCurrentClubAsync();
            var currentClubId = currentClub.CurrentClubId;

            // 1) SQL (no tracking, lightweight projection)
            var query = _db.Set<Player>()
                .AsNoTracking()
                .Where(p => !p.Inactive);

            if (!User.IsInRole("Admin") || currentClubId.HasValue)
            {
                if (!currentClubId.HasValue)
                {
                    AllPlayers = new List<PlayerRow>();
                    return;
                }

                query = query.Where(p => p.PlayerClubs.Any(pc =>
                    !pc.Inactive &&
                    pc.FkBgdClub == currentClubId.Value));
            }

            var players = await query
                .OrderBy(p => p.FirstName)
                .ThenBy(p => p.LastName)
                .Select(p => new
                {
                    p.Id,
                    p.Gid,
                    p.FirstName,
                    p.LastName
                })
                .ToListAsync();

            if (players.Count == 0)
            {
                AllPlayers = new List<PlayerRow>();
                return;
            }

            // 2) Mongo (ONE query): grab focus + whether bytes exist
            var gids = players.Select(p => (Guid?)p.Gid).ToArray();

            var imgDocs = await _imagesCollection
                .Find(Builders<BoardGameImages>.Filter.Eq(x => x.SQLTable, "bgd.Player") &
                      Builders<BoardGameImages>.Filter.In(x => x.GID, gids))
                .Project(x => new
                {
                    x.GID,
                    x.AvatarFocusX,
                    x.AvatarFocusY,
                    x.ImageBytes
                })
                .ToListAsync();

            var imgMap = imgDocs
                .Where(d => d.GID.HasValue)
                .ToDictionary(
                    d => d.GID!.Value,
                    d => new
                    {
                        Focus = $"{(d.AvatarFocusX == 0 ? 50 : d.AvatarFocusX)}% {(d.AvatarFocusY == 0 ? 50 : d.AvatarFocusY)}%",
                        HasImage = d.ImageBytes != null && d.ImageBytes.Length > 0
                    });

            // 3) Combine
            var selected = Input.SelectedPlayerIds ?? new List<long>();

            AllPlayers = players.Select(p =>
            {
                var name = $"{p.FirstName ?? ""} {p.LastName ?? ""}".Trim();
                if (string.IsNullOrWhiteSpace(name)) name = "Unnamed";

                var found = imgMap.TryGetValue(p.Gid, out var meta);

                return new PlayerRow
                {
                    PlayerId = p.Id,
                    Gid = p.Gid,
                    Name = name,
                    AvatarUrl = $"/media/player/{p.Gid}",
                    FocusStyle = found ? meta!.Focus : "50% 50%",
                    HasImage = found && meta!.HasImage,
                    Preselected = selected.Contains(p.Id)
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
            public Guid Gid { get; set; }
            public string Name { get; set; } = string.Empty;

            public string AvatarUrl { get; set; } = string.Empty;
            public string FocusStyle { get; set; } = "50% 50%";
            public bool HasImage { get; set; }

            public bool Preselected { get; set; }
        }

        private async Task<List<long>> KeepCurrentClubPlayerIdsAsync(IEnumerable<long> playerIds, long? currentClubId)
        {
            var selected = playerIds.Distinct().ToList();
            if (selected.Count == 0)
            {
                return selected;
            }

            if (User.IsInRole("Admin") && !currentClubId.HasValue)
            {
                return selected;
            }

            if (!currentClubId.HasValue)
            {
                return new List<long>();
            }

            return await _db.PlayerClubs
                .AsNoTracking()
                .Where(pc =>
                    !pc.Inactive &&
                    pc.FkBgdClub == currentClubId.Value &&
                    selected.Contains(pc.FkBgdPlayer))
                .Select(pc => pc.FkBgdPlayer)
                .Distinct()
                .ToListAsync();
        }
    }
}

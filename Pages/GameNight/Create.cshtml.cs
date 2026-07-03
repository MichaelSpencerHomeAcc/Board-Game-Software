using Board_Game_Software.Models;
using Board_Game_Software.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Board_Game_Software.Pages.GameNight
{
    public class CreateModel : PageModel
    {
        private readonly BoardGameDbContext _db;
        private readonly ICurrentClubService _currentClubService;

        public CreateModel(
            BoardGameDbContext db,
            ICurrentClubService currentClubService)
        {
            _db = db;
            _currentClubService = currentClubService;
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

            var playerIds = players.Select(p => checked((int)p.Id)).ToList();
            var playerIdsWithImages = await _db.StoredImages
                .AsNoTracking()
                .Where(image => image.OwnerType == ImageService.UserAvatarOwnerType && playerIds.Contains(image.OwnerId))
                .Select(image => image.OwnerId)
                .Distinct()
                .ToListAsync();
            var playerImageSet = playerIdsWithImages.ToHashSet();

            // 3) Combine
            var selected = Input.SelectedPlayerIds ?? new List<long>();

            AllPlayers = players.Select(p =>
            {
                var name = $"{p.FirstName ?? ""} {p.LastName ?? ""}".Trim();
                if (string.IsNullOrWhiteSpace(name)) name = "Unnamed";

                return new PlayerRow
                {
                    PlayerId = p.Id,
                    Gid = p.Gid,
                    Name = name,
                    AvatarUrl = $"/media/player/{p.Gid}",
                    FocusStyle = "50% 50%",
                    HasImage = playerImageSet.Contains(checked((int)p.Id)),
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

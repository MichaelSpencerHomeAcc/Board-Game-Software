using Board_Game_Software.Models;
using Board_Game_Software.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

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
        public List<VisibilityOption> VisibilityOptions { get; private set; } = new();

        [BindProperty]
        public CreateInput Input { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.IsInRole("Admin") && !(await _currentClubService.GetCurrentClubAsync()).CurrentClubId.HasValue)
            {
                return Forbid();
            }

            var currentClub = await _currentClubService.GetCurrentClubAsync();
            Input.StartsAt = DateTime.Today.AddHours(19);
            Input.GameNightDate = DateOnly.FromDateTime(Input.StartsAt);
            Input.Visibility = await GetDefaultVisibilityAsync(currentClub.CurrentClubId);
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
            Input.GameNightDate = DateOnly.FromDateTime(Input.StartsAt);
            if (!GameNightDefaults.IsValidVisibility(Input.Visibility))
            {
                ModelState.AddModelError(nameof(Input.Visibility), "Choose a valid visibility.");
            }
            if (Input.EndsAt.HasValue && Input.EndsAt.Value <= Input.StartsAt)
            {
                ModelState.AddModelError(nameof(Input.EndsAt), "End time must be after the start time.");
            }
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
                Title = string.IsNullOrWhiteSpace(Input.Title) ? null : Input.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(Input.Description) ? null : Input.Description.Trim(),
                StartsAt = Input.StartsAt,
                EndsAt = Input.EndsAt,
                Visibility = Input.Visibility,
                BookingUrl = string.IsNullOrWhiteSpace(Input.BookingUrl) ? null : Input.BookingUrl.Trim(),
                CreatedByUserId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
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
            await LoadVisibilityOptionsAsync(currentClubId);

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

            [StringLength(160)]
            public string? Title { get; set; }

            [StringLength(1000)]
            public string? Description { get; set; }

            [Required]
            public DateTime StartsAt { get; set; }

            public DateTime? EndsAt { get; set; }

            [Required]
            public string Visibility { get; set; } = GameNightDefaults.MembersOnlyVisibility;

            [StringLength(500)]
            [Url]
            public string? BookingUrl { get; set; }

            public List<long> SelectedPlayerIds { get; set; } = new();
        }

        public class VisibilityOption
        {
            public string Value { get; init; } = string.Empty;
            public string Label { get; init; } = string.Empty;
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

        private async Task LoadVisibilityOptionsAsync(long? currentClubId)
        {
            var clubType = await _db.Clubs.AsNoTracking()
                .Where(c => currentClubId.HasValue && c.Id == currentClubId.Value && !c.Inactive)
                .Select(c => c.ClubType)
                .FirstOrDefaultAsync();

            var values = clubType == ClubDefaults.PrivateGroupType
                ? [GameNightDefaults.PrivateVisibility]
                : GameNightDefaults.VisibilityLevels;

            VisibilityOptions = values
                .Select(value => new VisibilityOption
                {
                    Value = value,
                    Label = GameNightDefaults.GetDisplayName(value)
                })
                .ToList();
        }

        private async Task<string> GetDefaultVisibilityAsync(long? currentClubId)
        {
            if (!currentClubId.HasValue)
            {
                return GameNightDefaults.PrivateVisibility;
            }

            var club = await _db.Clubs.AsNoTracking()
                .Where(c => c.Id == currentClubId.Value && !c.Inactive)
                .Select(c => new { c.ClubType, c.DefaultGameNightVisibility })
                .FirstOrDefaultAsync();

            if (club?.ClubType == ClubDefaults.PrivateGroupType)
            {
                return GameNightDefaults.PrivateVisibility;
            }

            return GameNightDefaults.IsValidVisibility(club?.DefaultGameNightVisibility)
                ? club!.DefaultGameNightVisibility
                : GameNightDefaults.MembersOnlyVisibility;
        }
    }
}

using Board_Game_Software.Models;
using Board_Game_Software.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Board_Game_Software.Pages.GameNight
{
    public class AddPlayersModel : PageModel
    {
        private readonly BoardGameDbContext _db;
        private readonly ICurrentClubService _currentClubService;

        public AddPlayersModel(
            BoardGameDbContext db,
            ICurrentClubService currentClubService)
        {
            _db = db;
            _currentClubService = currentClubService;
        }

        public List<PlayerRow> AllPlayers { get; set; } = new();

        [BindProperty]
        public AddInput Input { get; set; } = new();

        [BindProperty]
        public QuickAddInput QuickAdd { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(long id, string? returnUrl)
        {
            var night = await _db.BoardGameNights.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id);
            if (night == null) return NotFound();
            if (!await CanManageNightAsync(night)) return Forbid();
            if (night.Finished) return RedirectToPage("/GameNight/Details", new { id });

            Input.NightId = id;
            Input.ReturnUrl = returnUrl;

            await LoadPlayersAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var fallbackUrl = Url.Page("/GameNight/Details", new { id = Input.NightId })
                ?? $"/GameNight/Details/{Input.NightId}";

            if (Input.SelectedPlayerIds == null || !Input.SelectedPlayerIds.Any())
            {
                return Redirect(Input.ReturnUrl ?? fallbackUrl);
            }

            var night = await _db.BoardGameNights.AsNoTracking().FirstOrDefaultAsync(n => n.Id == Input.NightId);
            if (night == null) return NotFound();
            if (!await CanManageNightAsync(night)) return Forbid();
            if (night.Finished) return Redirect(Input.ReturnUrl ?? fallbackUrl);

            var validPlayerIds = await GetAvailablePlayerQuery(night)
                .Where(p => Input.SelectedPlayerIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToListAsync();

            if (!validPlayerIds.Any())
            {
                return Redirect(Input.ReturnUrl ?? fallbackUrl);
            }

            var now = DateTime.UtcNow;
            var userName = User?.Identity?.Name ?? "system";

            var links = validPlayerIds.Distinct().Select(pid => new BoardGameNightPlayer
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

            return Redirect(Input.ReturnUrl ?? fallbackUrl);
        }

        public async Task<IActionResult> OnPostQuickAddAsync()
        {
            var fallbackUrl = Url.Page("/GameNight/Details", new { id = QuickAdd.NightId })
                ?? $"/GameNight/Details/{QuickAdd.NightId}";

            var night = await _db.BoardGameNights.AsNoTracking().FirstOrDefaultAsync(n => n.Id == QuickAdd.NightId);
            if (night == null) return NotFound();
            if (!await CanManageNightAsync(night)) return Forbid();
            if (night.Finished) return Redirect(QuickAdd.ReturnUrl ?? fallbackUrl);

            QuickAdd.FirstName = QuickAdd.FirstName?.Trim();
            QuickAdd.LastName = QuickAdd.LastName?.Trim();

            if (string.IsNullOrWhiteSpace(QuickAdd.FirstName) && string.IsNullOrWhiteSpace(QuickAdd.LastName))
            {
                ModelState.AddModelError(nameof(QuickAdd.FirstName), "Enter at least a first or last name.");
                Input.NightId = QuickAdd.NightId;
                Input.ReturnUrl = QuickAdd.ReturnUrl;
                await LoadPlayersAsync();
                return Page();
            }

            var now = DateTime.UtcNow;
            var userName = User?.Identity?.Name ?? "system";

            var player = new Player
            {
                Gid = Guid.NewGuid(),
                FirstName = QuickAdd.FirstName,
                LastName = QuickAdd.LastName,
                FkBgdClub = night.FkBgdClub,
                Inactive = false,
                TimeCreated = now,
                TimeModified = now,
                CreatedBy = userName,
                ModifiedBy = userName
            };

            _db.Players.Add(player);
            await _db.SaveChangesAsync();

            if (night.FkBgdClub.HasValue)
            {
                _db.PlayerClubs.Add(new PlayerClub
                {
                    Gid = Guid.NewGuid(),
                    Inactive = false,
                    TimeCreated = now,
                    TimeModified = now,
                    CreatedBy = userName,
                    ModifiedBy = userName,
                    FkBgdPlayer = player.Id,
                    FkBgdClub = night.FkBgdClub.Value,
                    JoinedAt = now
                });
            }

            _db.BoardGameNightPlayers.Add(new BoardGameNightPlayer
            {
                Gid = Guid.NewGuid(),
                Inactive = false,
                TimeCreated = now,
                TimeModified = now,
                CreatedBy = userName,
                ModifiedBy = userName,
                FkBgdBoardGameNight = QuickAdd.NightId,
                FkBgdPlayer = player.Id
            });

            await _db.SaveChangesAsync();
            return Redirect(QuickAdd.ReturnUrl ?? fallbackUrl);
        }

        private async Task LoadPlayersAsync()
        {
            var night = await _db.BoardGameNights.AsNoTracking().FirstOrDefaultAsync(n => n.Id == Input.NightId);
            if (night == null)
            {
                AllPlayers = new List<PlayerRow>();
                return;
            }

            // Get IDs already in this night to exclude them
            var existingIds = await _db.Set<BoardGameNightPlayer>()
                .Where(x => x.FkBgdBoardGameNight == Input.NightId && !x.Inactive)
                .Select(x => x.FkBgdPlayer)
                .ToListAsync();

            var players = await GetAvailablePlayerQuery(night)
                .Where(p => !p.Inactive && !existingIds.Contains(p.Id))
                .OrderBy(p => p.FirstName)
                .ToListAsync();

            AllPlayers = players.Select(p =>
            {
                return new PlayerRow
                {
                    PlayerId = p.Id,
                    Name = $"{p.FirstName} {p.LastName}".Trim(),
                    AvatarUrl = $"/media/player/{p.Gid}"
                };
            }).ToList();
        }

        private IQueryable<Player> GetAvailablePlayerQuery(BoardGameNight night)
        {
            var query = _db.Set<Player>().AsNoTracking().Where(p => !p.Inactive);

            if (night.FkBgdClub.HasValue)
            {
                var clubId = night.FkBgdClub.Value;
                query = query.Where(p => p.PlayerClubs.Any(pc => !pc.Inactive && pc.FkBgdClub == clubId));
            }

            return query;
        }

        private async Task<bool> CanManageNightAsync(BoardGameNight night)
        {
            if (User.IsInRole("Admin")) return true;

            var currentClub = await _currentClubService.GetCurrentClubAsync();
            return night.FkBgdClub.HasValue
                && night.FkBgdClub == currentClub.CurrentClubId
                && currentClub.CanManageCurrentClub;
        }

        public class AddInput
        {
            public long NightId { get; set; }
            public string? ReturnUrl { get; set; }
            public List<long> SelectedPlayerIds { get; set; } = new();
        }

        public class QuickAddInput
        {
            public long NightId { get; set; }
            public string? ReturnUrl { get; set; }
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
        }

        public class PlayerRow
        {
            public long PlayerId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string? AvatarUrl { get; set; }
            public bool Preselected { get; set; }
        }
    }
}

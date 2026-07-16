using Board_Game_Software.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Board_Game_Software.Pages.Admin.ManageUsers
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly BoardGameDbContext _db;

        public IndexModel(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, BoardGameDbContext db)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _db = db;
        }

        public List<UserRow> Users { get; set; } = new();
        public List<string> AllRoles { get; set; } = new();
        public List<SelectListItem> TierOptions { get; set; } = new();
        public SelectList PlayerOptions { get; set; } = default!;
        public int AdminCount { get; private set; }
        public int LinkedCount { get; private set; }
        public int UnlinkedPlayerCount { get; private set; }

        [BindProperty] public string UserId { get; set; } = string.Empty;
        [BindProperty] public List<string> SelectedRoles { get; set; } = new();
        [BindProperty] public long? PlayerId { get; set; }
        [BindProperty] public string SubscriptionTier { get; set; } = AccountTierDefaults.FreePlayer;
        [BindProperty] public bool IsComped { get; set; }
        [BindProperty] public string? TierNotes { get; set; }

        [TempData] public string? StatusMessage { get; set; }

        public sealed class UserRow
        {
            public string Id { get; init; } = string.Empty;
            public string Email { get; init; } = string.Empty;
            public bool EmailConfirmed { get; init; }
            public DateTimeOffset? LockoutEnd { get; init; }
            public List<string> Roles { get; init; } = new();
            public long? PlayerId { get; init; }
            public string? PlayerName { get; init; }
            public string SubscriptionTier { get; init; } = AccountTierDefaults.FreePlayer;
            public string TierLabel => AccountTierDefaults.GetDisplayName(SubscriptionTier);
            public bool IsComped { get; init; }
            public string? TierNotes { get; init; }
        }

        public async Task OnGetAsync()
        {
            AllRoles = await _roleManager.Roles
                .Select(r => r.Name!)
                .Where(name => name != null)
                .OrderBy(name => name)
                .ToListAsync();

            TierOptions = AccountTierDefaults.Tiers
                .Select(tier => new SelectListItem(AccountTierDefaults.GetDisplayName(tier), tier))
                .ToList();

            var playerLinks = await _db.Players.AsNoTracking()
                .Where(p => !p.Inactive)
                .Select(p => new
                {
                    p.Id,
                    p.FkdboAspNetUsers,
                    Name = (p.FirstName + " " + p.LastName).Trim()
                })
                .ToListAsync();

            var playerByUserId = playerLinks
                .Where(p => !string.IsNullOrWhiteSpace(p.FkdboAspNetUsers))
                .GroupBy(p => p.FkdboAspNetUsers!)
                .ToDictionary(g => g.Key, g => g.First());

            var identityUsers = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();
            var identityUserIds = identityUsers.Select(u => u.Id).ToList();
            var accountTiers = await _db.UserAccountTiers
                .AsNoTracking()
                .Where(t => !t.Inactive && identityUserIds.Contains(t.UserId))
                .ToDictionaryAsync(t => t.UserId);
            var rows = new List<UserRow>();

            foreach (var user in identityUsers)
            {
                var roles = (await _userManager.GetRolesAsync(user)).OrderBy(r => r).ToList();
                playerByUserId.TryGetValue(user.Id, out var player);
                accountTiers.TryGetValue(user.Id, out var accountTier);

                rows.Add(new UserRow
                {
                    Id = user.Id,
                    Email = user.Email ?? user.UserName ?? user.Id,
                    EmailConfirmed = user.EmailConfirmed,
                    LockoutEnd = user.LockoutEnd,
                    Roles = roles,
                    PlayerId = player?.Id,
                    PlayerName = player?.Name,
                    SubscriptionTier = accountTier?.SubscriptionTier ?? AccountTierDefaults.FreePlayer,
                    IsComped = accountTier?.IsComped ?? false,
                    TierNotes = accountTier?.Notes
                });
            }

            Users = rows;
            AdminCount = rows.Count(u => u.Roles.Contains("Admin"));
            LinkedCount = rows.Count(u => u.PlayerId.HasValue);

            var linkedPlayerIds = rows.Where(u => u.PlayerId.HasValue).Select(u => u.PlayerId!.Value).ToHashSet();
            var availablePlayers = playerLinks
                .Where(p => !linkedPlayerIds.Contains(p.Id))
                .OrderBy(p => p.Name)
                .ToList();

            UnlinkedPlayerCount = availablePlayers.Count;
            PlayerOptions = new SelectList(availablePlayers, "Id", "Name");
        }

        public async Task<IActionResult> OnPostUpdateRolesAsync()
        {
            var user = await _userManager.FindByIdAsync(UserId);
            if (user == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            if (UserId == currentUserId && !SelectedRoles.Contains("Admin"))
            {
                ModelState.AddModelError(string.Empty, "You cannot remove your own Admin role.");
                await OnGetAsync();
                return Page();
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles.Except(SelectedRoles));
            await _userManager.AddToRolesAsync(user, SelectedRoles.Except(currentRoles));

            StatusMessage = $"Updated roles for {user.Email ?? user.UserName}.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateTierAsync()
        {
            var user = await _userManager.FindByIdAsync(UserId);
            if (user == null) return NotFound();

            if (!AccountTierDefaults.IsValidTier(SubscriptionTier))
            {
                ModelState.AddModelError(string.Empty, "Choose a valid subscription tier.");
                await OnGetAsync();
                return Page();
            }

            var now = DateTime.UtcNow;
            var actor = User.Identity?.Name ?? "system";
            var accountTier = await _db.UserAccountTiers
                .FirstOrDefaultAsync(t => !t.Inactive && t.UserId == UserId);

            if (accountTier == null)
            {
                accountTier = new UserAccountTier
                {
                    Gid = Guid.NewGuid(),
                    UserId = UserId,
                    CreatedBy = actor,
                    TimeCreated = now
                };
                _db.UserAccountTiers.Add(accountTier);
            }

            accountTier.SubscriptionTier = SubscriptionTier;
            accountTier.IsComped = IsComped;
            accountTier.Notes = string.IsNullOrWhiteSpace(TierNotes) ? null : TierNotes.Trim();
            accountTier.ModifiedBy = actor;
            accountTier.TimeModified = now;

            await _db.SaveChangesAsync();

            StatusMessage = $"Updated tier for {user.Email ?? user.UserName}.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostLinkPlayerAsync()
        {
            if (string.IsNullOrWhiteSpace(UserId) || !PlayerId.HasValue) return RedirectToPage();

            var user = await _userManager.FindByIdAsync(UserId);
            var player = await _db.Players.FirstOrDefaultAsync(p => p.Id == PlayerId.Value);
            if (user == null || player == null) return NotFound();

            var existingForUser = await _db.Players.Where(p => p.FkdboAspNetUsers == user.Id).ToListAsync();
            foreach (var linked in existingForUser)
            {
                linked.FkdboAspNetUsers = null;
                linked.ModifiedBy = User.Identity?.Name ?? "system";
                linked.TimeModified = DateTime.UtcNow;
            }

            player.FkdboAspNetUsers = user.Id;
            player.ModifiedBy = User.Identity?.Name ?? "system";
            player.TimeModified = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            StatusMessage = $"Linked {user.Email ?? user.UserName} to {(player.FirstName + " " + player.LastName).Trim()}.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUnlinkPlayerAsync()
        {
            if (string.IsNullOrWhiteSpace(UserId)) return RedirectToPage();

            var linkedPlayers = await _db.Players.Where(p => p.FkdboAspNetUsers == UserId).ToListAsync();
            foreach (var player in linkedPlayers)
            {
                player.FkdboAspNetUsers = null;
                player.ModifiedBy = User.Identity?.Name ?? "system";
                player.TimeModified = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            StatusMessage = "Player link removed.";
            return RedirectToPage();
        }
    }
}

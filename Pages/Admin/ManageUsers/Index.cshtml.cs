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
        public SelectList PlayerOptions { get; set; } = default!;
        public int AdminCount { get; private set; }
        public int LinkedCount { get; private set; }
        public int UnlinkedPlayerCount { get; private set; }

        [BindProperty] public string UserId { get; set; } = string.Empty;
        [BindProperty] public List<string> SelectedRoles { get; set; } = new();
        [BindProperty] public long? PlayerId { get; set; }

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
        }

        public async Task OnGetAsync()
        {
            AllRoles = await _roleManager.Roles
                .Select(r => r.Name!)
                .Where(name => name != null)
                .OrderBy(name => name)
                .ToListAsync();

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
            var rows = new List<UserRow>();

            foreach (var user in identityUsers)
            {
                var roles = (await _userManager.GetRolesAsync(user)).OrderBy(r => r).ToList();
                playerByUserId.TryGetValue(user.Id, out var player);

                rows.Add(new UserRow
                {
                    Id = user.Id,
                    Email = user.Email ?? user.UserName ?? user.Id,
                    EmailConfirmed = user.EmailConfirmed,
                    LockoutEnd = user.LockoutEnd,
                    Roles = roles,
                    PlayerId = player?.Id,
                    PlayerName = player?.Name
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

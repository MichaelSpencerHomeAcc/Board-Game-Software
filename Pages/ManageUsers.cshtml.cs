using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Board_Game_Software.Pages
{
    [Authorize(Roles = "Admin")]
    public class ManageUsersModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public ManageUsersModel(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public List<IdentityUser> Users { get; set; } = new();
        public List<string> AllRoles { get; set; } = new();

        [BindProperty]
        public string UserId { get; set; }

        [BindProperty]
        public List<string> SelectedRoles { get; set; } = new();

        public async Task OnGetAsync()
        {
            Users = _userManager.Users.ToList();
            AllRoles = _roleManager.Roles.Select(r => r.Name).ToList();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.FindByIdAsync(UserId);
            if (user == null)
            {
                return NotFound();
            }

            // Prevent removing Admin from yourself
            var currentUserId = _userManager.GetUserId(User);
            if (UserId == currentUserId && !SelectedRoles.Contains("Admin"))
            {
                ModelState.AddModelError(string.Empty, "You cannot remove your own Admin role.");
                await OnGetAsync(); // repopulate data
                return Page();
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            var rolesToAdd = SelectedRoles.Except(currentRoles);
            var rolesToRemove = currentRoles.Except(SelectedRoles);

            await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            await _userManager.AddToRolesAsync(user, rolesToAdd);

            return RedirectToPage(); // Refresh page
        }


        // ✅ This method allows Razor to check role membership
        public async Task<bool> IsUserInRoleAsync(IdentityUser user, string role)
        {
            return await _userManager.IsInRoleAsync(user, role);
        }
    }
}

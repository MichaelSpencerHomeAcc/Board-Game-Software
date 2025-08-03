using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Threading.Tasks;

namespace Board_Game_Software.Pages.DataSetup.Players
{
    public class AddModel : PageModel
    {
        private readonly BoardGameDbContext _context;

        [BindProperty]
        public Player Player { get; set; } = new();

        public AddModel(BoardGameDbContext context)
        {
            _context = context;
        }

        public void OnGet()
        {
            // Nothing special to load here
        }

        public async Task<IActionResult> OnPostAsync()
        {

            Player.CreatedBy = User.Identity?.Name ?? "system";
            Player.TimeCreated = DateTime.UtcNow;
            Player.ModifiedBy = Player.CreatedBy;
            Player.TimeModified = Player.TimeCreated;

            if (!ModelState.IsValid)
            {
                foreach (var entry in ModelState)
                {
                    var key = entry.Key;
                    foreach (var error in entry.Value.Errors)
                    {
                        Console.WriteLine($"Validation error on '{key}': {error.ErrorMessage}");
                    }
                }
            }

            Player.Gid = Guid.NewGuid();
            Player.Inactive = false;
            Player.TimeModified = DateTime.UtcNow;
            Player.ModifiedBy = "System";

            _context.Players.Add(Player);
            await _context.SaveChangesAsync();

            return RedirectToPage("Index");
        }
    }
}

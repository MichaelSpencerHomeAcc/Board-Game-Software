using Board_Game_Software.Data;
using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Board_Game_Software.Pages.DataSetup.Shelves
{
    public class EditModel : PageModel
    {
        private readonly BoardGameDbContext _context;

        public EditModel(BoardGameDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Shelf Shelf { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null) return NotFound();

            Shelf = await _context.Shelves.FirstOrDefaultAsync(m => m.Id == id);

            if (Shelf == null) return NotFound();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(long id)
        {
            var shelfToUpdate = await _context.Shelves.FindAsync(id);

            if (shelfToUpdate == null) return NotFound();

            // Remove validation for fields handled by the system or not on the form
            ModelState.Remove("Shelf.CreatedBy");
            ModelState.Remove("Shelf.ModifiedBy");
            ModelState.Remove("Shelf.ShelfSections");

            if (await TryUpdateModelAsync<Shelf>(
                shelfToUpdate,
                "Shelf",
                s => s.ShelfName,
                s => s.Inactive))
            {
                shelfToUpdate.ModifiedBy = User.Identity?.Name ?? "system";
                shelfToUpdate.TimeModified = DateTime.Now;

                try
                {
                    await _context.SaveChangesAsync();
                    return RedirectToPage("./Index");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Shelves.Any(e => e.Id == id)) return NotFound();
                    throw;
                }
            }

            return Page();
        }

        public string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "S";
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) return name.Substring(0, Math.Min(2, name.Length)).ToUpper();
            return (parts[0][0].ToString() + parts[1][0].ToString()).ToUpper();
        }

        public string GetAvatarColor(string name)
        {
            if (string.IsNullOrEmpty(name)) return "#6c757d";
            int hash = 0;
            foreach (char c in name) hash = c + ((hash << 5) - hash);
            var colors = new[] { "#d32f2f", "#c2185b", "#7b1fa2", "#512da8", "#303f9f", "#1976d2", "#0288d1", "#0097a7", "#00796b", "#388e3c", "#689f38", "#fbc02d", "#ffa000", "#f57c00", "#e64a19", "#5d4037", "#616161", "#455a64" };
            return colors[Math.Abs(hash) % colors.Length];
        }
    }
}
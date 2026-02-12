using Board_Game_Software.Data;
using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Board_Game_Software.Pages.DataSetup.Shelves
{
    public class IndexModel : PageModel
    {
        private readonly BoardGameDbContext _context;

        public IndexModel(BoardGameDbContext context)
        {
            _context = context;
        }

        public IList<Shelf> Shelves { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; } = string.Empty;

        public async Task OnGetAsync()
        {
            var query = _context.Shelves
                .Include(s => s.ShelfSections)
                .Where(s => !s.Inactive) // Only show active shelves
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(s => s.ShelfName.Contains(SearchTerm));
            }

            Shelves = await query
                .OrderBy(s => s.ShelfName)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id)
        {
            var shelf = await _context.Shelves
                .Include(s => s.ShelfSections)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (shelf == null) return NotFound();

            // Safety: Prevent deletion if shelf has sections
            if (shelf.ShelfSections.Any())
            {
                return RedirectToPage("./Index");
            }

            shelf.Inactive = true; // Use soft delete consistent with model
            await _context.SaveChangesAsync();
            return RedirectToPage("./Index");
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
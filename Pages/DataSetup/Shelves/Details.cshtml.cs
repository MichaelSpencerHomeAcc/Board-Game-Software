using Board_Game_Software.Data;
using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Board_Game_Software.Pages.DataSetup.Shelves
{
    public class DetailsModel : PageModel
    {
        private readonly BoardGameDbContext _context;

        public DetailsModel(BoardGameDbContext context)
        {
            _context = context;
        }

        public Shelf Shelf { get; set; } = default!;
        public List<ShelfSection> ActiveSections { get; set; } = new();
        public int MaxSections { get; set; }

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null) return NotFound();

            Shelf = await _context.Shelves
                .Include(s => s.ShelfSections)
                    .ThenInclude(ss => ss.BoardGameShelfSections)
                        // FIX: Use the Navigation Property (the object), not the FK (the long)
                        .ThenInclude(bgss => bgss.FkBgdBoardGameNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Shelf == null) return NotFound();

            ActiveSections = Shelf.ShelfSections
                .Where(ss => !ss.Inactive)
                .ToList();

            MaxSections = ActiveSections.Any()
                ? (ActiveSections.Max(s => (int?)s.SectionNumber) ?? 1)
                : 1;

            return Page();
        }

        public string GetInitials(string name) => string.IsNullOrWhiteSpace(name) ? "S" : (name.Length > 1 ? name.Substring(0, 2).ToUpper() : name.ToUpper());

        public string GetAvatarColor(string name)
        {
            if (string.IsNullOrEmpty(name)) return "#6c757d";
            int hash = name.GetHashCode();
            var colors = new[] { "#d32f2f", "#c2185b", "#7b1fa2", "#512da8", "#303f9f", "#1976d2", "#0288d1", "#0097a7", "#00796b", "#388e3c", "#689f38", "#fbc02d", "#ffa000", "#f57c00", "#e64a19", "#5d4037" };
            return colors[Math.Abs(hash) % colors.Length];
        }
    }
}
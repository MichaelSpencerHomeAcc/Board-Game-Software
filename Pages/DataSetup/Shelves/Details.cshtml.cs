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
        public int MaxRows { get; set; }
        public int NextRow { get; set; }
        public int NextColumn { get; set; }

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null) return NotFound();

            Shelf = await _context.Shelves
                .Include(s => s.ShelfSections)
                    .ThenInclude(ss => ss.BoardGameShelfSections)
                        .ThenInclude(bgss => bgss.FkBgdBoardGameNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Shelf == null) return NotFound();

            ActiveSections = Shelf.ShelfSections
                .Where(ss => !ss.Inactive)
                .ToList();

            // Calculate Grid Dimensions
            MaxSections = ActiveSections.Any()
                ? (ActiveSections.Max(s => (int?)s.SectionNumber) ?? 1)
                : 1;

            // FIX: Dynamic Row Calculation so B1 (Row 2) shows up
            int recordedRows = (int?)(Shelf.TotalRows) ?? 1;
            int actualMaxRow = ActiveSections.Any() ? ActiveSections.Max(s => (int)s.RowNumber) : 1;
            MaxRows = Math.Max(recordedRows, actualMaxRow);

            // Button Logic
            if (ActiveSections.Any())
            {
                var maxR = ActiveSections.Max(s => (int?)s.RowNumber) ?? 1;
                var maxC = ActiveSections.Where(s => (int)s.RowNumber == maxR).Max(s => (int?)s.SectionNumber) ?? 0;
                NextRow = (int)maxR;
                NextColumn = (int)maxC + 1;
            }
            else
            {
                NextRow = 1;
                NextColumn = 1;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostDeleteSectionAsync(long sectionId)
        {
            var section = await _context.ShelfSections
                .Include(ss => ss.BoardGameShelfSections)
                .FirstOrDefaultAsync(ss => ss.Id == sectionId);

            if (section == null) return NotFound();

            if (section.BoardGameShelfSections.Any())
            {
                TempData["Error"] = "Cannot delete a section that contains games.";
                return RedirectToPage(new { id = section.FkBgdShelf });
            }

            _context.ShelfSections.Remove(section);
            await _context.SaveChangesAsync();

            return RedirectToPage(new { id = section.FkBgdShelf });
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
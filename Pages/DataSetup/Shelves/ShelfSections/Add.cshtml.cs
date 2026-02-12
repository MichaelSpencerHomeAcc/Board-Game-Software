using Board_Game_Software.Data;
using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Board_Game_Software.Pages.DataSetup.Shelves.ShelfSections
{
    public class AddModel : PageModel
    {
        private readonly BoardGameDbContext _context;

        public AddModel(BoardGameDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public ShelfSection ShelfSection { get; set; } = default!;
        public Shelf Shelf { get; set; } = default!;
        public List<ShelfSection> ExistingSections { get; set; } = new();
        public int MaxSections { get; set; }
        public int MaxRows { get; set; }

        public async Task<IActionResult> OnGetAsync(long shelfId, int? row, int? col)
        {
            Shelf = await _context.Shelves
                .Include(s => s.ShelfSections)
                .FirstOrDefaultAsync(m => m.Id == shelfId);

            if (Shelf == null) return NotFound();

            ExistingSections = Shelf.ShelfSections.Where(ss => !ss.Inactive).ToList();

            // Set grid boundaries
            int currentMaxR = ExistingSections.Any() ? ExistingSections.Max(s => (int)s.RowNumber) : 1;
            int currentMaxC = ExistingSections.Any() ? ExistingSections.Max(s => (int)s.SectionNumber) : 1;

            MaxRows = Math.Max(Math.Max(currentMaxR, row ?? 1), 3);
            MaxSections = Math.Max(Math.Max(currentMaxC, col ?? 1), 3);

            ShelfSection = new ShelfSection
            {
                FkBgdShelf = shelfId,
                RowNumber = (byte)(row ?? 1),
                SectionNumber = (byte)(col ?? 1),
                WidthCm = 30,
                HeightCm = 30,
                SectionName = $"{(char)(64 + (row ?? 1))}{col ?? 1}"
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // 1. Audit Info
            string currentUserName = User.Identity?.Name ?? "System";
            ShelfSection.CreatedBy = currentUserName;
            ShelfSection.ModifiedBy = currentUserName;
            ShelfSection.TimeCreated = DateTime.Now;
            ShelfSection.TimeModified = DateTime.Now;
            ShelfSection.Gid = Guid.NewGuid();

            // 2. Load existing sections for adjacency and boundary checks
            var currentSections = await _context.ShelfSections
                .Where(ss => ss.FkBgdShelf == ShelfSection.FkBgdShelf && !ss.Inactive)
                .ToListAsync();

            if (currentSections.Any())
            {
                // Orthogonal Check: Must be exactly 1 away in Row OR Column, but not both (diagonal)
                bool isOrthogonal = currentSections.Any(ex =>
                    (Math.Abs(ShelfSection.RowNumber - ex.RowNumber) == 1 && ShelfSection.SectionNumber == ex.SectionNumber) ||
                    (Math.Abs(ShelfSection.SectionNumber - ex.SectionNumber) == 1 && ShelfSection.RowNumber == ex.RowNumber)
                );

                if (!isOrthogonal)
                {
                    ModelState.AddModelError(string.Empty, "Invalid Placement: Sections must be placed directly next to existing ones (no diagonals or gaps).");
                }
            }

            // 3. Standard Clean-up and Save
            ModelState.Remove("ShelfSection.FkBgdShelfNavigation");
            ModelState.Remove("ShelfSection.CreatedBy");
            ModelState.Remove("ShelfSection.ModifiedBy");

            if (!ModelState.IsValid)
            {
                Shelf = await _context.Shelves.Include(s => s.ShelfSections).FirstOrDefaultAsync(m => m.Id == ShelfSection.FkBgdShelf);
                ExistingSections = Shelf?.ShelfSections.Where(ss => !ss.Inactive).ToList() ?? new();
                return Page();
            }

            try
            {
                _context.ShelfSections.Add(ShelfSection);
                await _context.SaveChangesAsync();
                return RedirectToPage("/DataSetup/Shelves/Details", new { id = ShelfSection.FkBgdShelf });
            }
            catch (DbUpdateException ex)
            {
                var msg = ex.InnerException?.InnerException?.Message ?? ex.InnerException?.Message ?? ex.Message;
                ModelState.AddModelError(string.Empty, $"Database Error: {msg}");
                Shelf = await _context.Shelves.Include(s => s.ShelfSections).FirstOrDefaultAsync(m => m.Id == ShelfSection.FkBgdShelf);
                return Page();
            }
        }
    }
}
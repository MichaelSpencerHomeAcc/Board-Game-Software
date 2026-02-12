using Board_Game_Software.Data;
using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Board_Game_Software.Pages.DataSetup.Shelves
{
    public class AddModel : PageModel
    {
        private readonly BoardGameDbContext _context;

        public AddModel(BoardGameDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Shelf Shelf { get; set; } = default!;

        public IActionResult OnGet()
        {
            Shelf = new Shelf { TotalRows = 1 };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // 1. Manually set audit fields for the Shelf
            string user = User.Identity?.Name ?? "System";
            Shelf.CreatedBy = user;
            Shelf.ModifiedBy = user;
            Shelf.TimeCreated = DateTime.Now;
            Shelf.TimeModified = DateTime.Now;
            Shelf.Gid = Guid.NewGuid();

            // 2. Clear the 'Required' complaints for these fields
            ModelState.Remove("Shelf.CreatedBy");
            ModelState.Remove("Shelf.ModifiedBy");

            if (!ModelState.IsValid) return Page();

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 3. Save the Shelf
                _context.Shelves.Add(Shelf);
                await _context.SaveChangesAsync();

                // 4. Create the 'A1' starter section
                var firstSection = new ShelfSection
                {
                    FkBgdShelf = Shelf.Id,
                    SectionName = "A1",
                    RowNumber = 1,
                    SectionNumber = 1,
                    WidthCm = 30,
                    HeightCm = 30,
                    Gid = Guid.NewGuid(),
                    CreatedBy = user,
                    ModifiedBy = user,
                    TimeCreated = DateTime.Now,
                    TimeModified = DateTime.Now
                };

                _context.ShelfSections.Add(firstSection);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return RedirectToPage("./Details", new { id = Shelf.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var msg = ex.InnerException?.Message ?? ex.Message;
                ModelState.AddModelError(string.Empty, $"Database Error: {msg}");
                return Page();
            }
        }
    }
}
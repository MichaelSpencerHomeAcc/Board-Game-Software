using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Board_Game_Software.Pages.DataSetup.Publishers
{
    public class IndexModel : PageModel
    {
        private readonly BoardGameDbContext _context;

        public IndexModel(BoardGameDbContext context)
        {
            _context = context;
        }

        // Initialize with default! to avoid null issues
        public IList<Publisher> Publishers { get; set; } = default!;

        public async Task OnGetAsync()
        {
            Publishers = await _context.Publishers
                .Where(p => !p.Inactive)
                .OrderBy(p => p.PublisherName)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync(long id)
        {
            var publisher = await _context.Publishers.FindAsync(id);

            if (publisher == null)
            {
                return NotFound();
            }

            // Remove or soft delete as per your logic
            _context.Publishers.Remove(publisher);
            await _context.SaveChangesAsync();

            // Option 1: Reload list and return page to avoid null
            Publishers = await _context.Publishers
                .Where(p => !p.Inactive)
                .OrderBy(p => p.PublisherName)
                .ToListAsync();

            return Page();

            // Option 2: Alternatively, redirect to get handler
            // return RedirectToPage();
        }
    }
}

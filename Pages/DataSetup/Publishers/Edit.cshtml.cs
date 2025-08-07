using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Board_Game_Software.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Board_Game_Software.Pages.DataSetup.Publishers
{
    public class EditModel : PageModel
    {
        private readonly BoardGameDbContext _context;

        public EditModel(BoardGameDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Publisher Publisher { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        public async Task<IActionResult> OnGetAsync(long id)
        {
            Publisher = await _context.Publishers.FindAsync(id);

            if (Publisher == null)
            {
                return NotFound();
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Attach(Publisher).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Publishers.Any(e => e.Id == Publisher.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            if (!string.IsNullOrWhiteSpace(ReturnUrl))
            {
                // If ReturnUrl is a relative local URL, redirect there
                if (Url.IsLocalUrl(ReturnUrl))
                {
                    return Redirect(ReturnUrl);
                }
            }

            // Default fallback to index
            return RedirectToPage("Index");
        }
    }
}

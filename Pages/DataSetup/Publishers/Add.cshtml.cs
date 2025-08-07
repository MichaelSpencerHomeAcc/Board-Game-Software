using System.Threading.Tasks;
using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Board_Game_Software.Pages.DataSetup.Publishers
{
    public class AddModel : PageModel
    {
        private readonly BoardGameDbContext _context;

        public AddModel(BoardGameDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Publisher Publisher { get; set; } = new Publisher();

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }


            var now = DateTime.UtcNow;

            Publisher.TimeCreated = now;
            Publisher.TimeModified = now;

            _context.Publishers.Add(Publisher);
            await _context.SaveChangesAsync();

            return RedirectToPage("Index");
        }
    }
}

using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Board_Game_Software.Pages.DataSetup.Publishers
{
    public class IndexModel : PageModel
    {
        private readonly BoardGameDbContext _context;

        public IndexModel(BoardGameDbContext context)
        {
            _context = context;
        }

        public sealed class PublisherRow
        {
            public long Id { get; init; }
            public Guid Gid { get; init; }
            public string PublisherName { get; init; } = string.Empty;
            public string? Description { get; init; }

            public string LogoUrl => $"/media/publisher/{Gid}";
        }

        public IList<PublisherRow> Publishers { get; private set; } = new List<PublisherRow>();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.VwPublishers
                .AsNoTracking()
                .Where(p => !p.Inactive);

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim();
                query = query.Where(p => p.PublisherName.Contains(term));
            }

            Publishers = await query
                .OrderBy(p => p.PublisherName)
                .Select(p => new PublisherRow
                {
                    Id = p.Id,
                    Gid = p.Gid,
                    PublisherName = p.PublisherName ?? string.Empty,
                    Description = p.Description
                })
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id)
        {
            var publisher = await _context.Publishers.FindAsync(id);
            if (publisher == null) return NotFound();

            _context.Publishers.Remove(publisher);
            await _context.SaveChangesAsync();

            return RedirectToPage(new { search = SearchTerm });
        }
    }
}

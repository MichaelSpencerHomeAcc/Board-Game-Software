using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Board_Game_Software.Pages.DataSetup.BoardGameMarkerTypes
{
    public class IndexModel : PageModel
    {
        private readonly BoardGameDbContext _context;

        public IndexModel(BoardGameDbContext context)
        {
            _context = context;
        }

        public sealed class MarkerTypeRow
        {
            public long Id { get; init; }
            public Guid Gid { get; init; }
            public string TypeDesc { get; init; } = string.Empty;
            public string? AlignmentDesc { get; init; }
            public string? AdditionalDesc { get; init; }
        }

        public IList<MarkerTypeRow> MarkerTypes { get; private set; } = new List<MarkerTypeRow>();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        public int? DeleteLinkedCount { get; set; }
        public bool ShowDeleteError => DeleteLinkedCount.HasValue && DeleteLinkedCount.Value > 0;

        public async Task OnGetAsync(string? search)
        {
            SearchTerm = search;

            var query = _context.BoardGameMarkerTypes
                .AsNoTracking()
                .Where(m => !m.Inactive);

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim();

                // NOTE: keeping your exact logic, but without Includes
                query = query.Where(m =>
                    m.TypeDesc.Contains(term) ||
                    (m.FkBgdMarkerAlignmentTypeNavigation != null && m.FkBgdMarkerAlignmentTypeNavigation.TypeDesc == term) ||
                    (m.FkBgdMarkerAdditionalTypeNavigation != null && m.FkBgdMarkerAdditionalTypeNavigation.TypeDesc == term)
                );
            }

            MarkerTypes = await query
                .OrderBy(mt => mt.TypeDesc)
                .Select(m => new MarkerTypeRow
                {
                    Id = m.Id,
                    Gid = m.Gid,
                    TypeDesc = m.TypeDesc,
                    AlignmentDesc = m.FkBgdMarkerAlignmentTypeNavigation != null ? m.FkBgdMarkerAlignmentTypeNavigation.TypeDesc : null,
                    AdditionalDesc = m.FkBgdMarkerAdditionalTypeNavigation != null ? m.FkBgdMarkerAdditionalTypeNavigation.TypeDesc : null
                })
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id)
        {
            // tracked for update
            var markerType = await _context.BoardGameMarkerTypes.FindAsync(id);
            if (markerType == null) return NotFound();

            var linkedCount = await _context.BoardGameMarkers
                .AsNoTracking()
                .CountAsync(bgm => bgm.FkBgdBoardGameMarkerType == id && !bgm.Inactive);

            if (linkedCount > 0)
            {
                DeleteLinkedCount = linkedCount;
                await OnGetAsync(SearchTerm); // refresh list
                return Page();
            }

            markerType.Inactive = true;
            await _context.SaveChangesAsync();
            return RedirectToPage(new { search = SearchTerm });
        }
    }
}

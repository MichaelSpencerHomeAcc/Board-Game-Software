using Board_Game_Software.Models;
using Board_Game_Software.Services;
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
        private readonly ICurrentClubService _currentClubService;

        public IndexModel(BoardGameDbContext context, ICurrentClubService currentClubService)
        {
            _context = context;
            _currentClubService = currentClubService;
        }

        public sealed class MarkerTypeRow
        {
            public long Id { get; init; }
            public Guid Gid { get; init; }
            public string TypeDesc { get; init; } = string.Empty;
            public string? AlignmentDesc { get; init; }
            public string? AdditionalDesc { get; init; }
            public string ScopeName { get; init; } = "Global";
            public bool IsGlobal { get; init; }
        }

        public IList<MarkerTypeRow> MarkerTypes { get; private set; } = new List<MarkerTypeRow>();
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; } = 25;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        public int? DeleteLinkedCount { get; set; }
        public bool ShowDeleteError => DeleteLinkedCount.HasValue && DeleteLinkedCount.Value > 0;

        public async Task OnGetAsync(string? search, int pageNumber = 1)
        {
            SearchTerm = search;
            PageNumber = Math.Max(1, pageNumber);
            var club = await _currentClubService.GetCurrentClubAsync();

            var query = _context.BoardGameMarkerTypes
                .AsNoTracking()
                .Where(m => !m.Inactive);

            if (!club.IsPlatformAdminMode)
            {
                var clubId = club.CurrentClubId;
                query = query.Where(m => m.FkBgdClub == null || m.FkBgdClub == clubId);
            }

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

            TotalCount = await query.CountAsync();
            PageNumber = Math.Min(PageNumber, Math.Max(TotalPages, 1));

            MarkerTypes = await query
                .OrderBy(mt => mt.TypeDesc)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .Select(m => new MarkerTypeRow
                {
                    Id = m.Id,
                    Gid = m.Gid,
                    TypeDesc = m.TypeDesc,
                    AlignmentDesc = m.FkBgdMarkerAlignmentTypeNavigation != null ? m.FkBgdMarkerAlignmentTypeNavigation.TypeDesc : null,
                    AdditionalDesc = m.FkBgdMarkerAdditionalTypeNavigation != null ? m.FkBgdMarkerAdditionalTypeNavigation.TypeDesc : null,
                    ScopeName = m.FkBgdClubNavigation != null ? m.FkBgdClubNavigation.ClubName : "Global",
                    IsGlobal = m.FkBgdClub == null
                })
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id)
        {
            // tracked for update
            var club = await _currentClubService.GetCurrentClubAsync();
            var markerType = await _context.BoardGameMarkerTypes
                .FirstOrDefaultAsync(m => m.Id == id
                    && ((club.IsPlatformAdminMode && m.FkBgdClub == null)
                        || (!club.IsPlatformAdminMode && m.FkBgdClub == club.CurrentClubId)));
            if (markerType == null) return NotFound();

            var linkedCount = await _context.BoardGameMarkers
                .AsNoTracking()
                .CountAsync(bgm => bgm.FkBgdBoardGameMarkerType == id && !bgm.Inactive);

            if (linkedCount > 0)
            {
                DeleteLinkedCount = linkedCount;
                await OnGetAsync(SearchTerm, PageNumber); // refresh list
                return Page();
            }

            markerType.Inactive = true;
            await _context.SaveChangesAsync();
            return RedirectToPage(new { search = SearchTerm, pageNumber = PageNumber });
        }
    }
}

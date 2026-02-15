using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Board_Game_Software.Pages.DataSetup.BoardGameMarkerTypes
{
    public class IndexModel : PageModel
    {
        private readonly BoardGameDbContext _context;
        private readonly IMongoCollection<BoardGameImages> _imagesCollection;

        public IndexModel(BoardGameDbContext context, IMongoClient mongoClient, IConfiguration config)
        {
            _context = context;
            var dbName = config["MongoDbSettings:Database"];
            var database = mongoClient.GetDatabase(dbName);
            _imagesCollection = database.GetCollection<BoardGameImages>("BoardGameImages");
        }

        public IList<BoardGameMarkerType> MarkerTypes { get; set; } = default!;
        public Dictionary<long, string?> MarkerImagesBase64 { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        public int? DeleteLinkedCount { get; set; }
        public bool ShowDeleteError => DeleteLinkedCount.HasValue && DeleteLinkedCount.Value > 0;

        public async Task OnGetAsync(string? search)
        {
            SearchTerm = search;

            // Read-only list page: NO TRACKING (big win)
            var query = _context.BoardGameMarkerTypes
                .AsNoTracking()
                .Include(m => m.FkBgdMarkerAlignmentTypeNavigation)
                .Include(m => m.FkBgdMarkerAdditionalTypeNavigation)
                .Where(m => !m.Inactive);

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim();

                // Safer null-guards + keeps SQL clean
                query = query.Where(m =>
                    m.TypeDesc.Contains(term) ||
                    (m.FkBgdMarkerAlignmentTypeNavigation != null && m.FkBgdMarkerAlignmentTypeNavigation.TypeDesc == term) ||
                    (m.FkBgdMarkerAdditionalTypeNavigation != null && m.FkBgdMarkerAdditionalTypeNavigation.TypeDesc == term)
                );
            }

            MarkerTypes = await query
                .OrderBy(mt => mt.TypeDesc)
                .ToListAsync();

            // Image Fetching (Mongo) - use Guid values, not strings
            // Image URLs (fast, cacheable, no base64 payload)
            MarkerImagesBase64.Clear();

            foreach (var marker in MarkerTypes)
            {
                // Just point to media endpoint instead of embedding image
                MarkerImagesBase64[marker.Id] = $"/media/marker-type/{marker.Gid}";
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id)
        {
            // keep this tracked because we are updating it
            var markerType = await _context.BoardGameMarkerTypes.FindAsync(id);
            if (markerType == null) return NotFound();

            var linkedCount = await _context.BoardGameMarkers
                .AsNoTracking()
                .CountAsync(bgm => bgm.FkBgdBoardGameMarkerType == id && !bgm.Inactive);

            if (linkedCount > 0)
            {
                DeleteLinkedCount = linkedCount;
                await OnGetAsync(SearchTerm); // Refresh list with current search
                return Page();
            }

            markerType.Inactive = true;
            await _context.SaveChangesAsync();
            return RedirectToPage(new { search = SearchTerm });
        }
    }
}

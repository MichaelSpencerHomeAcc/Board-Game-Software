using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
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

            var query = _context.BoardGameMarkerTypes
                .Include(m => m.FkBgdMarkerAlignmentTypeNavigation)
                .Include(m => m.FkBgdMarkerAdditionalTypeNavigation)
                .Where(m => !m.Inactive);

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                // Partial match for Description, Full match for Alignment or Additional Type
                query = query.Where(m =>
                    m.TypeDesc.Contains(SearchTerm) ||
                    m.FkBgdMarkerAlignmentTypeNavigation.TypeDesc == SearchTerm ||
                    m.FkBgdMarkerAdditionalTypeNavigation.TypeDesc == SearchTerm
                );
            }

            MarkerTypes = await query
                .OrderBy(mt => mt.TypeDesc)
                .ToListAsync();

            // Image Fetching
            var gids = MarkerTypes.Select(m => m.Gid.ToString()).ToList();
            if (gids.Any())
            {
                var filter = Builders<BoardGameImages>.Filter.And(
                    Builders<BoardGameImages>.Filter.Eq(x => x.SQLTable, "bgd.BoardGameMarkerType"),
                    Builders<BoardGameImages>.Filter.In("GID", gids)
                );

                var imageDocs = await _imagesCollection.Find(filter).ToListAsync();
                var imagesDict = imageDocs
                    .Where(img => img.ImageBytes != null && img.GID.HasValue)
                    .ToDictionary(img => img.GID.Value.ToString(), img => $"data:{img.ContentType};base64,{Convert.ToBase64String(img.ImageBytes)}");

                foreach (var marker in MarkerTypes)
                {
                    var gidStr = marker.Gid.ToString();
                    MarkerImagesBase64[marker.Id] = imagesDict.ContainsKey(gidStr) ? imagesDict[gidStr] : null;
                }
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id)
        {
            var markerType = await _context.BoardGameMarkerTypes.FindAsync(id);
            if (markerType == null) return NotFound();

            var linkedCount = await _context.BoardGameMarkers
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
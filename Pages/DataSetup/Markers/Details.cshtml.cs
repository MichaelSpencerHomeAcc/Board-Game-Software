using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;

namespace Board_Game_Software.Pages.DataSetup.BoardGameMarkerTypes
{
    public class DetailsModel : PageModel
    {
        private readonly BoardGameDbContext _context;
        private readonly IMongoCollection<BoardGameImages> _imagesCollection;
        public IList<(BoardGame Game, string? ImageBase64)> RelatedBoardGamesWithImages { get; set; } = new List<(BoardGame, string?)>();

        public DetailsModel(BoardGameDbContext context, IMongoClient mongoClient, IConfiguration config)
        {
            _context = context;
            var dbName = config["MongoDbSettings:Database"];
            var database = mongoClient.GetDatabase(dbName);
            _imagesCollection = database.GetCollection<BoardGameImages>("BoardGameImages");
        }

        public BoardGameMarkerType? MarkerType { get; set; }
        public string? MarkerImageBase64 { get; set; }

        public async Task<IActionResult> OnGetAsync(long id)
        {
            MarkerType = await _context.BoardGameMarkerTypes
                .Include(m => m.FkBgdMarkerAlignmentTypeNavigation)
                .Include(m => m.FkBgdMarkerAdditionalTypeNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (MarkerType == null)
            {
                return NotFound();
            }

            await LoadMarkerImage(MarkerType.Gid);

            // Load related board games that use this marker type
            var relatedGames = await _context.BoardGameMarkers
                .Where(bgm => bgm.FkBgdBoardGameMarkerType == id && !bgm.Inactive)
                .Include(bgm => bgm.FkBgdBoardGameNavigation)
                .Select(bgm => bgm.FkBgdBoardGameNavigation)
                .OrderBy(game => game.BoardGameName)  // Order by name here
                .ToListAsync();

            // Load images from MongoDB similarly to BoardGameDetails.cshtml.cs
            foreach (var game in relatedGames)
            {
                string? base64Image = null;

                if (game.Gid != Guid.Empty)
                {
                    var frontImageType = await _context.BoardGameImageTypes
                        .FirstOrDefaultAsync(t => t.TypeDesc == "Board Game Front");

                    if (frontImageType != null)
                    {
                        var filter = Builders<BoardGameImages>.Filter.And(
                            Builders<BoardGameImages>.Filter.Eq(x => x.GID, game.Gid),
                            Builders<BoardGameImages>.Filter.Eq(x => x.ImageTypeGID, frontImageType.Gid)
                        );

                        var imageDoc = await _imagesCollection.Find(filter).FirstOrDefaultAsync();

                        if (imageDoc?.ImageBytes != null)
                        {
                            base64Image = $"data:{imageDoc.ContentType};base64,{Convert.ToBase64String(imageDoc.ImageBytes)}";
                        }
                    }
                }

                RelatedBoardGamesWithImages.Add((game, base64Image));
            }

            return Page();
        }
        private async Task LoadMarkerImage(Guid gid)
        {
            var filter = Builders<BoardGameImages>.Filter.And(
                Builders<BoardGameImages>.Filter.Eq(x => x.SQLTable, "bgd.BoardGameMarkerType"),
                Builders<BoardGameImages>.Filter.Eq(x => x.GID, gid)
            );

            var imageDoc = await _imagesCollection.Find(filter).FirstOrDefaultAsync();

            if (imageDoc?.ImageBytes != null)
                MarkerImageBase64 = $"data:{imageDoc.ContentType};base64,{Convert.ToBase64String(imageDoc.ImageBytes)}";
            else
                MarkerImageBase64 = null;
        }
    }
}

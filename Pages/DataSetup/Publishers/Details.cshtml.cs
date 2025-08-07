using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Board_Game_Software.Pages.DataSetup.Publishers
{
    public class DetailsModel : PageModel
    {
        private readonly BoardGameDbContext _context;
        private readonly IMongoCollection<BoardGameImages> _imagesCollection;

        public DetailsModel(BoardGameDbContext context, IMongoClient mongoClient, IConfiguration config)
        {
            _context = context;
            var dbName = config["MongoDbSettings:Database"];
            var database = mongoClient.GetDatabase(dbName);
            _imagesCollection = database.GetCollection<BoardGameImages>("BoardGameImages");
        }

        public Publisher Publisher { get; set; }
        public IList<(BoardGame Game, string? ImageBase64)> RelatedBoardGamesWithImages { get; set; } = new List<(BoardGame, string?)>();

        public async Task<IActionResult> OnGetAsync(long id)
        {
            Publisher = await _context.Publishers
                .FirstOrDefaultAsync(p => p.Id == id);

            if (Publisher == null)
            {
                return NotFound();
            }

            // Load related board games by this publisher
            var relatedGames = await _context.BoardGames
                .Where(bg => bg.FkBgdPublisher == id)
                .OrderBy(bg => bg.BoardGameName)
                .ToListAsync();

            // Load images from MongoDB (similar logic to your existing code)
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
    }
}

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

        public Publisher Publisher { get; set; } = default!;
        public string? PublisherLogoBase64 { get; set; }
        public IList<(BoardGame Game, string? ImageBase64)> RelatedBoardGamesWithImages { get; set; } = new List<(BoardGame, string?)>();

        public async Task<IActionResult> OnGetAsync(long id)
        {
            Publisher = await _context.Publishers
                .FirstOrDefaultAsync(p => p.Id == id);

            if (Publisher == null)
            {
                return NotFound();
            }

            // Load Publisher Logo
            var logoType = await _context.BoardGameImageTypes.FirstOrDefaultAsync(t => t.TypeDesc == "Image");
            if (logoType != null)
            {
                var logoDoc = await _imagesCollection.Find(x => x.GID == Publisher.Gid && x.ImageTypeGID == logoType.Gid).FirstOrDefaultAsync();
                if (logoDoc?.ImageBytes != null)
                {
                    PublisherLogoBase64 = $"data:{logoDoc.ContentType};base64,{Convert.ToBase64String(logoDoc.ImageBytes)}";
                }
            }

            // Load Related Games
            var relatedGames = await _context.BoardGames
                .Where(bg => bg.FkBgdPublisher == id)
                .OrderBy(bg => bg.BoardGameName)
                .ToListAsync();

            var frontImageType = await _context.BoardGameImageTypes.FirstOrDefaultAsync(t => t.TypeDesc == "Board Game Front");

            foreach (var game in relatedGames)
            {
                string? base64Image = null;
                if (game.Gid != Guid.Empty && frontImageType != null)
                {
                    var imageDoc = await _imagesCollection.Find(x => x.GID == game.Gid && x.ImageTypeGID == frontImageType.Gid).FirstOrDefaultAsync();
                    if (imageDoc?.ImageBytes != null)
                    {
                        base64Image = $"data:{imageDoc.ContentType};base64,{Convert.ToBase64String(imageDoc.ImageBytes)}";
                    }
                }
                RelatedBoardGamesWithImages.Add((game, base64Image));
            }

            return Page();
        }
    }
}
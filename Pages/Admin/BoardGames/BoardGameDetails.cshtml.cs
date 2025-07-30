using Board_Game_Software.Data;
using Board_Game_Software.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Threading.Tasks;

namespace Board_Game_Software.Pages.Admin.BoardGames
{
    [Authorize(Roles = "Admin")]
    public class BoardGameDetailsModel : PageModel
    {
        private readonly BoardGameDbContext _context;
        private readonly IMongoCollection<BoardGameImages> _boardGameImages;

        public BoardGameDetailsModel(BoardGameDbContext context, IMongoClient mongoClient, IConfiguration configuration)
        {
            _context = context;
            var databaseName = configuration["MongoDbSettings:Database"];
            var database = mongoClient.GetDatabase(databaseName);

            // Your Mongo collection name (update if different)
            _boardGameImages = database.GetCollection<BoardGameImages>("BoardGameImages");
        }

        public BoardGame BoardGame { get; set; }
        public string BoardGameFrontImageUrl { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            BoardGame = await _context.BoardGames
                .Include(bg => bg.FkBgdBoardGameTypeNavigation)
                .Include(bg => bg.FkBgdBoardGameVictoryConditionTypeNavigation)
                .Include(bg => bg.FkBgdPublisherNavigation)
                .Include(bg => bg.BoardGameMarkers)
                    .ThenInclude(marker => marker.FkBgdBoardGameMarkerTypeNavigation)
                .FirstOrDefaultAsync(bg => bg.Id == id);

            if (BoardGame == null)
            {
                return NotFound();
            }

            var frontImageType = await _context.BoardGameImageTypes
                .FirstOrDefaultAsync(bgit => bgit.TypeDesc == "Board Game Front");

            if (frontImageType != null && BoardGame.Gid != null)
            {
                var image = await _boardGameImages.Find(img =>
                    img.GID == BoardGame.Gid && img.ImageTypeGID == frontImageType.Gid)
                    .FirstOrDefaultAsync();

                if (image != null)
                {
                    if (image.ImageBytes != null)
                    {
                        var base64 = Convert.ToBase64String(image.ImageBytes);
                        BoardGameFrontImageUrl = $"data:{image.ContentType};base64,{base64}";
                    }
                }
            }

            return Page();
        }
    }
}

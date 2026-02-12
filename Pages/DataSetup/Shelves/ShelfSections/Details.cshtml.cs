using Board_Game_Software.Data;
using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Microsoft.Extensions.Configuration;

namespace Board_Game_Software.Pages.DataSetup.Shelves.ShelfSections
{
    public class DetailsModel : PageModel
    {
        private readonly BoardGameDbContext _context;
        private readonly IMongoCollection<BoardGameImages> _boardGameImages;

        public DetailsModel(BoardGameDbContext context, IMongoClient mongoClient, IConfiguration configuration)
        {
            _context = context;
            var databaseName = configuration["MongoDbSettings:Database"];
            var database = mongoClient.GetDatabase(databaseName);
            _boardGameImages = database.GetCollection<BoardGameImages>("BoardGameImages");
        }

        public ShelfSection ShelfSection { get; set; } = default!;
        public decimal RemainingWidth { get; set; }
        public double PercentFull { get; set; }

        // Dictionary to store the Front Images for the games in this section
        public Dictionary<long, string> GameImages { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null) return NotFound();

            ShelfSection = await _context.ShelfSections
                .Include(s => s.FkBgdShelfNavigation)
                .Include(s => s.BoardGameShelfSections)
                    .ThenInclude(bgss => bgss.FkBgdBoardGameNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (ShelfSection == null) return NotFound();

            // 1. Calculate space
            decimal usedWidth = ShelfSection.BoardGameShelfSections
                .Where(bgss => bgss.FkBgdBoardGameNavigation != null)
                .Sum(bgss => bgss.FkBgdBoardGameNavigation.WidthCm ?? 0m);

            RemainingWidth = ShelfSection.WidthCm - usedWidth;
            PercentFull = ShelfSection.WidthCm > 0 ? (double)((usedWidth / ShelfSection.WidthCm) * 100) : 0;

            // 2. Fetch Images from MongoDB
            var frontImageType = await _context.BoardGameImageTypes
                .FirstOrDefaultAsync(bgit => bgit.TypeDesc == "Board Game Front");

            if (frontImageType != null && ShelfSection.BoardGameShelfSections.Any())
            {
                var gameGids = ShelfSection.BoardGameShelfSections
                    .Select(bgss => bgss.FkBgdBoardGameNavigation?.Gid)
                    .Where(gid => gid.HasValue && gid != Guid.Empty)
                    .ToList();

                var filter = Builders<BoardGameImages>.Filter.And(
                    Builders<BoardGameImages>.Filter.In(img => img.GID, gameGids),
                    Builders<BoardGameImages>.Filter.Eq(img => img.ImageTypeGID, frontImageType.Gid)
                );

                var images = await _boardGameImages.Find(filter).ToListAsync();

                foreach (var bgss in ShelfSection.BoardGameShelfSections)
                {
                    var game = bgss.FkBgdBoardGameNavigation;
                    if (game != null)
                    {
                        var img = images.FirstOrDefault(i => i.GID == game.Gid);
                        if (img?.ImageBytes != null)
                        {
                            GameImages[game.Id] = $"data:{img.ContentType};base64,{Convert.ToBase64String(img.ImageBytes)}";
                        }
                    }
                }
            }

            return Page();
        }
        public async Task<IActionResult> OnPostRemoveGameAsync(long sectionId, long gameId)
        {
            var link = await _context.BoardGameShelfSections
                .FirstOrDefaultAsync(x => x.FkBgdShelfSection == sectionId && x.FkBgdBoardGame == gameId);

            if (link != null)
            {
                _context.BoardGameShelfSections.Remove(link);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage(new { id = sectionId });
        }
    }
}
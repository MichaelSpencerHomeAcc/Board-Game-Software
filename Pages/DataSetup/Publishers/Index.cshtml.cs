using Board_Game_Software.Data;
using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

namespace Board_Game_Software.Pages.DataSetup.Publishers
{
    public class IndexModel : PageModel
    {
        private readonly BoardGameDbContext _context;
        private readonly IMongoCollection<BoardGameImages> _boardGameImages;

        public IndexModel(BoardGameDbContext context, IMongoClient mongoClient, IConfiguration configuration)
        {
            _context = context;
            var databaseName = configuration["MongoDbSettings:Database"];
            var database = mongoClient.GetDatabase(databaseName);
            _boardGameImages = database.GetCollection<BoardGameImages>("BoardGameImages");
        }

        public IList<Publisher> Publishers { get; set; } = default!;
        public Dictionary<long, string> PublisherLogosBase64 { get; set; } = new();
        public bool ShowDeleteError { get; set; } = false;
        public int DeleteLinkedCount { get; set; } = 0;

        public async Task OnGetAsync(bool? deleteError, int? linkedCount)
        {
            ShowDeleteError = deleteError ?? false;
            DeleteLinkedCount = linkedCount ?? 0;

            Publishers = await _context.Publishers
                .OrderBy(p => p.PublisherName)
                .ToListAsync();

            if (Publishers.Any())
            {
                var gids = Publishers.Select(p => p.Gid.ToString()).ToList();

                var filter = Builders<BoardGameImages>.Filter.In("GID", gids);

                var imageDocs = await _boardGameImages.Find(filter).ToListAsync();

                foreach (var pub in Publishers)
                {
                    var doc = imageDocs.FirstOrDefault(img => img.GID.ToString() == pub.Gid.ToString());
                    if (doc?.ImageBytes != null)
                    {
                        PublisherLogosBase64[pub.Id] = $"data:{doc.ContentType};base64,{Convert.ToBase64String(doc.ImageBytes)}";
                    }
                }
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id)
        {
            var publisher = await _context.Publishers
                .Include(p => p.BoardGames)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (publisher == null) return NotFound();

            if (publisher.BoardGames != null && publisher.BoardGames.Any())
            {
                return RedirectToPage(new { deleteError = true, linkedCount = publisher.BoardGames.Count });
            }

            _context.Publishers.Remove(publisher);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}
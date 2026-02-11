using Board_Game_Software.Data;
using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public IList<VwPublisher> Publishers { get; set; } = default!;
        public Dictionary<long, string> PublisherImagesBase64 { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        public async Task OnGetAsync()
        {
            // 1. Get the "Image" Type GID (Required for Publishers)
            // This matches the logic in your Edit.cshtml.cs
            var imageType = await _context.BoardGameImageTypes
                .FirstOrDefaultAsync(t => t.TypeDesc == "Image");

            var imageTypeGid = imageType?.Gid ?? Guid.Empty;

            // 2. Start Query
            var query = _context.VwPublishers.AsQueryable();

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                query = query.Where(p => p.PublisherName.Contains(SearchTerm));
            }

            Publishers = await query
                .OrderBy(p => p.PublisherName)
                .ToListAsync();

            // 3. Fetch Images using ImageTypeGID
            if (imageTypeGid != Guid.Empty)
            {
                var publisherGids = Publishers.Select(p => p.Gid).ToList();

                // Optimized: Fetch all relevant images in one Mongo query
                var filter = Builders<BoardGameImages>.Filter.And(
                    Builders<BoardGameImages>.Filter.In(x => x.GID, publisherGids.Select(g => (Guid?)g)),
                    Builders<BoardGameImages>.Filter.Eq(x => x.ImageTypeGID, (Guid?)imageTypeGid)
                );

                var images = await _boardGameImages.Find(filter).ToListAsync();

                foreach (var pub in Publishers)
                {
                    var img = images.FirstOrDefault(x => x.GID == pub.Gid);
                    if (img != null && img.ImageBytes != null)
                    {
                        PublisherImagesBase64[pub.Id] = $"data:{img.ContentType};base64,{Convert.ToBase64String(img.ImageBytes)}";
                    }
                }
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id)
        {
            var publisher = await _context.Publishers.FindAsync(id);
            if (publisher == null) return NotFound();

            // Clean up using ImageTypeGID logic
            var imageType = await _context.BoardGameImageTypes.FirstOrDefaultAsync(t => t.TypeDesc == "Image");
            if (imageType != null)
            {
                await _boardGameImages.DeleteManyAsync(img => img.GID == publisher.Gid && img.ImageTypeGID == imageType.Gid);
            }

            _context.Publishers.Remove(publisher);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}
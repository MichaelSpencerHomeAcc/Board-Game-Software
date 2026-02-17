using Board_Game_Software.Data;
using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Board_Game_Software.Pages.DataSetup.Publishers
{
    public class AddModel : PageModel
    {
        private readonly BoardGameDbContext _context;
        private readonly IMongoCollection<BoardGameImages> _boardGameImages;

        public AddModel(BoardGameDbContext context, IMongoClient mongoClient, IConfiguration configuration)
        {
            _context = context;
            var databaseName = configuration["MongoDbSettings:Database"];
            var database = mongoClient.GetDatabase(databaseName);
            _boardGameImages = database.GetCollection<BoardGameImages>("BoardGameImages");
        }

        [BindProperty]
        public Publisher Publisher { get; set; } = new();

        [BindProperty]
        public IFormFile? ImageUpload { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Set metadata following the MarkerType pattern
            Publisher.CreatedBy = User.Identity?.Name ?? "system";
            Publisher.TimeCreated = DateTime.UtcNow;
            Publisher.ModifiedBy = Publisher.CreatedBy;
            Publisher.TimeModified = Publisher.TimeCreated;
            Publisher.Gid = Guid.NewGuid();
            Publisher.Inactive = false;

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // 1. Save Publisher to SQL Server
            _context.Publishers.Add(Publisher);
            await _context.SaveChangesAsync();

            // 2. Handle Image Upload to MongoDB
            if (ImageUpload != null && ImageUpload.Length > 0)
            {
                using var ms = new MemoryStream();
                await ImageUpload.CopyToAsync(ms);
                var imageBytes = ms.ToArray();

                // Look for the "Image" type in your SQL lookup table
                var imageType = await _context.BoardGameImageTypes
                    .FirstOrDefaultAsync(t => t.TypeDesc == "Image");

                if (imageType != null)
                {
                    // Clean up any existing records (Safe practice for GUID based storage)
                    await _boardGameImages.DeleteManyAsync(img =>
                        img.GID == Publisher.Gid && img.ImageTypeGID == imageType.Gid);

                    var newImage = new BoardGameImages
                    {
                        GID = Publisher.Gid,
                        SQLTable = "bgd.Publisher",
                        ImageTypeGID = imageType.Gid,
                        ImageBytes = imageBytes,
                        ContentType = ImageUpload.ContentType,
                        Description = $"{Publisher.PublisherName} Logo"
                    };

                    await _boardGameImages.InsertOneAsync(newImage);
                }
            }

            return RedirectToPage("./Index");
        }
    }
}
using Board_Game_Software.Models;
using Board_Game_Software.Settings; // Make sure this is here to find MongoDbSettings
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options; // Required for IOptions
using MongoDB.Driver;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Board_Game_Software.Pages.DataSetup.Players
{
    public class AddModel : PageModel
    {
        private readonly BoardGameDbContext _context;
        private readonly IMongoCollection<BoardGameImages> _boardGameImages;

        // UPDATED CONSTRUCTOR: Uses IOptions to safely pull from your Program.cs setup
        public AddModel(
            BoardGameDbContext context,
            IMongoClient mongoClient,
            IOptions<MongoDbSettings> mongoSettings)
        {
            _context = context;

            // Access the database name safely from the settings object
            var databaseName = mongoSettings.Value.Database;
            var database = mongoClient.GetDatabase(databaseName);

            _boardGameImages = database.GetCollection<BoardGameImages>("BoardGameImages");
        }

        [BindProperty]
        public Player Player { get; set; } = new();

        [BindProperty]
        public IFormFile? Upload { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Set metadata
            Player.CreatedBy = User.Identity?.Name ?? "system";
            Player.TimeCreated = DateTime.UtcNow;
            Player.ModifiedBy = Player.CreatedBy;
            Player.TimeModified = Player.TimeCreated;
            Player.Gid = Guid.NewGuid();
            Player.Inactive = false;

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                foreach (var error in errors)
                {
                    Console.WriteLine($"ModelState error: {error}");
                }

                ViewData["ModelErrors"] = errors;
                return Page();
            }

            // 1. Save Player to SQL
            _context.Players.Add(Player);
            await _context.SaveChangesAsync();

            // 2. Handle Image Upload to MongoDB
            if (Upload != null && Upload.Length > 0)
            {
                using var ms = new MemoryStream();
                await Upload.CopyToAsync(ms);
                var imageBytes = ms.ToArray();

                var newImage = new BoardGameImages
                {
                    GID = Player.Gid,
                    SQLTable = "bgd.Player", // Matches the Player table schema
                    ImageBytes = imageBytes,
                    ContentType = Upload.ContentType,
                    Description = "Profile Picture"
                };

                await _boardGameImages.InsertOneAsync(newImage);
            }

            return RedirectToPage("./Index");
        }
    }
}
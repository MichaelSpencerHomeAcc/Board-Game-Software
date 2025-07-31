using Board_Game_Software.Data;
using Board_Game_Software.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Board_Game_Software.Pages.Browsing.BoardGames
{
    [Authorize(Roles = "Admin")]
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
        public BoardGame BoardGame { get; set; } = new();

        [BindProperty]
        public IFormFile? ImageUpload { get; set; }

        public SelectList BoardGameTypes { get; set; }
        public SelectList VictoryConditions { get; set; }
        public SelectList Publishers { get; set; }

        public async Task<IActionResult> OnGet()
        {
            await LoadSelectLists();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            BoardGame.CreatedBy = User.Identity?.Name ?? "system";
            BoardGame.TimeCreated = DateTime.UtcNow;
            BoardGame.ModifiedBy = BoardGame.CreatedBy;
            BoardGame.TimeModified = BoardGame.TimeCreated;

            if (!ModelState.IsValid)
            {
                // Collect all errors
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                // For debugging, either log or throw or show on page
                foreach (var error in errors)
                {
                    Console.WriteLine($"ModelState error: {error}");
                }

                // Optionally, pass errors to the ViewData so you can display them on the page
                ViewData["ModelErrors"] = errors;

                await LoadSelectLists();
                return Page();
            }

            // Add new board game to SQL DB
            BoardGame.Gid = Guid.NewGuid();
            _context.BoardGames.Add(BoardGame);
            await _context.SaveChangesAsync();

            // Handle image upload to MongoDB
            if (ImageUpload != null && ImageUpload.Length > 0)
            {
                using var ms = new MemoryStream();
                await ImageUpload.CopyToAsync(ms);
                var imageBytes = ms.ToArray();

                var frontImageType = await _context.BoardGameImageTypes
                    .FirstOrDefaultAsync(t => t.TypeDesc == "Board Game Front");

                if (frontImageType != null)
                {
                    // Remove existing images if any (just in case)
                    await _boardGameImages.DeleteManyAsync(img =>
                        img.GID == BoardGame.Gid && img.ImageTypeGID == frontImageType.Gid);

                    var newImage = new BoardGameImages
                    {
                        GID = BoardGame.Gid,
                        ImageTypeGID = frontImageType.Gid,
                        ImageBytes = imageBytes,
                        ContentType = ImageUpload.ContentType,
                        Description = "Board Game Front Image"
                    };

                    await _boardGameImages.InsertOneAsync(newImage);
                }
            }

            return RedirectToPage("./Index");
        }

        private async Task LoadSelectLists()
        {
            BoardGameTypes = new SelectList(await _context.BoardGameTypes.Where(t => !t.Inactive).ToListAsync(), "Id", "TypeDesc");
            VictoryConditions = new SelectList(await _context.BoardGameVictoryConditionTypes.Where(t => !t.Inactive).ToListAsync(), "Id", "TypeDesc");
            Publishers = new SelectList(await _context.Publishers.Where(p => !p.Inactive).ToListAsync(), "Id", "PublisherName");
        }
    }
}

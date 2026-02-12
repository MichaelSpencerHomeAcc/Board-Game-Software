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

        public VwPublisher Publisher { get; set; } = default!;

        // Hero Image
        public string? PublisherLogoBase64 { get; set; }

        // List of games + Dictionary for their images
        public IList<BoardGame> RelatedGames { get; set; } = new List<BoardGame>();
        public Dictionary<long, string> RelatedGameImages { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(long id)
        {
            // 1. Fetch Publisher (Using View or Entity, here keeping your View usage if strictly read-only, 
            // but usually we want the Entity for relationships. Let's use the View for display properties)
            Publisher = await _context.VwPublishers.FirstOrDefaultAsync(p => p.Id == id);

            if (Publisher == null) return NotFound();

            // 2. Fetch Publisher Logo
            // Note: In your Edit page, you used "Image" as the TypeDesc for Publisher logos.
            var logoType = await _context.BoardGameImageTypes.FirstOrDefaultAsync(t => t.TypeDesc == "Image");
            if (logoType != null)
            {
                var logoDoc = await _boardGameImages
                    .Find(x => x.GID == Publisher.Gid && x.ImageTypeGID == logoType.Gid)
                    .FirstOrDefaultAsync();

                if (logoDoc?.ImageBytes != null)
                {
                    PublisherLogoBase64 = $"data:{logoDoc.ContentType};base64,{Convert.ToBase64String(logoDoc.ImageBytes)}";
                }
            }

            // 3. Fetch Related Games
            // We need to query the BoardGames table using the Publisher ID
            RelatedGames = await _context.BoardGames
                .Where(bg => bg.FkBgdPublisher == id)
                .OrderBy(bg => bg.BoardGameName)
                .ToListAsync();

            // 4. Fetch Images for Related Games
            if (RelatedGames.Any())
            {
                var frontImageType = await _context.BoardGameImageTypes
                    .FirstOrDefaultAsync(t => t.TypeDesc == "Board Game Front");

                if (frontImageType != null)
                {
                    var gameGids = RelatedGames.Select(g => (Guid?)g.Gid).ToList();

                    var filter = Builders<BoardGameImages>.Filter.And(
                        Builders<BoardGameImages>.Filter.Eq(x => x.ImageTypeGID, frontImageType.Gid),
                        Builders<BoardGameImages>.Filter.In(x => x.GID, gameGids)
                    );

                    var images = await _boardGameImages.Find(filter).ToListAsync();

                    foreach (var game in RelatedGames)
                    {
                        var img = images.FirstOrDefault(x => x.GID == game.Gid);
                        if (img != null && img.ImageBytes != null)
                        {
                            RelatedGameImages[game.Id] = $"data:{img.ContentType};base64,{Convert.ToBase64String(img.ImageBytes)}";
                        }
                    }
                }
            }

            return Page();
        }

        // --- Helper Methods ---
        public string GetInitials(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "?";
            return name.Substring(0, Math.Min(2, name.Length)).ToUpper();
        }

        public string GetAvatarColor(string? name)
        {
            if (string.IsNullOrEmpty(name)) return "#6c757d";
            int hash = 0;
            foreach (char c in name) hash = c + ((hash << 5) - hash);
            var colors = new[] { "#d32f2f", "#c2185b", "#7b1fa2", "#512da8", "#303f9f", "#1976d2", "#0288d1", "#0097a7", "#00796b", "#388e3c", "#689f38", "#fbc02d", "#ffa000", "#f57c00", "#e64a19", "#5d4037", "#616161", "#455a64" };
            return colors[Math.Abs(hash) % colors.Length];
        }
    }
}
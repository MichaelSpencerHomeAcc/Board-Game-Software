using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Board_Game_Software.Data;
using Board_Game_Software.Models;
using MongoDB.Driver;
using Microsoft.Extensions.Configuration;

namespace Board_Game_Software.Pages.DataSetup.BoardGameMarkerTypes
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

        public BoardGameMarkerType BoardGameMarkerType { get; set; } = default!;
        public IList<BoardGameMarker> BoardGameMarkers { get; set; } = default!;

        // Holds the main image for the Marker Type
        public string? MarkerTypeImageBase64 { get; set; }

        // Holds the COVER IMAGES for the linked Board Games
        public Dictionary<long, string> BoardGameImagesBase64 { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null) return NotFound();

            // 1. Fetch Main Marker Type
            var boardgamemarkertype = await _context.BoardGameMarkerTypes
                .Include(b => b.FkBgdMarkerAdditionalTypeNavigation)
                .Include(b => b.FkBgdMarkerAlignmentTypeNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (boardgamemarkertype == null) return NotFound();
            BoardGameMarkerType = boardgamemarkertype;

            // 2. Fetch Associated Markers AND the Linked Board Game
            BoardGameMarkers = await _context.BoardGameMarkers
                .Include(m => m.FkBgdBoardGameNavigation) // <--- Crucial: Load the Game info
                .Where(m => m.FkBgdBoardGameMarkerType == id)
                .ToListAsync();

            // 3. Fetch Main Image (Marker Type)
            var typeImage = await _boardGameImages
                .Find(x => x.SQLTable == "bgd.BoardGameMarkerType" && x.GID == BoardGameMarkerType.Gid)
                .FirstOrDefaultAsync();

            if (typeImage != null && typeImage.ImageBytes != null)
            {
                MarkerTypeImageBase64 = $"data:{typeImage.ContentType};base64,{Convert.ToBase64String(typeImage.ImageBytes)}";
            }

            // 4. Fetch Images for the LINKED BOARD GAMES
            if (BoardGameMarkers.Any())
            {
                // Get the GIDs of the *Games*, not the markers
                var gameGids = BoardGameMarkers
                    .Select(m => (Guid?)m.FkBgdBoardGameNavigation.Gid)
                    .Distinct()
                    .ToList();

                // Look up images for the Board Games
                var filter = Builders<BoardGameImages>.Filter.And(
                    Builders<BoardGameImages>.Filter.Eq(x => x.SQLTable, "bgd.BoardGame"),
                    Builders<BoardGameImages>.Filter.In(x => x.GID, gameGids)
                );

                var images = await _boardGameImages.Find(filter).ToListAsync();

                // Map images back to the Marker ID (so the view can easily find the image for the row)
                foreach (var marker in BoardGameMarkers)
                {
                    var gameGid = marker.FkBgdBoardGameNavigation.Gid;
                    // Just take the first image found for this game
                    var img = images.FirstOrDefault(x => x.GID == gameGid);

                    if (img != null && img.ImageBytes != null)
                    {
                        BoardGameImagesBase64[marker.Id] = $"data:{img.ContentType};base64,{Convert.ToBase64String(img.ImageBytes)}";
                    }
                }
            }

            return Page();
        }

        // --- UI Helper Methods ---
        public string GetInitials(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "?";
            var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) return text.Substring(0, Math.Min(2, text.Length)).ToUpper();
            return (parts[0][0].ToString() + parts[1][0].ToString()).ToUpper();
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
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

namespace Board_Game_Software.Pages.Browsing.BoardGames
{
    public class BoardGameDetailsModel : PageModel
    {
        private readonly BoardGameDbContext _context;
        private readonly IMongoCollection<BoardGameImages> _boardGameImages;

        public BoardGameDetailsModel(BoardGameDbContext context, IMongoClient mongoClient, IConfiguration configuration)
        {
            _context = context;
            var databaseName = configuration["MongoDbSettings:Database"];
            var database = mongoClient.GetDatabase(databaseName);
            _boardGameImages = database.GetCollection<BoardGameImages>("BoardGameImages");
        }

        public BoardGame BoardGame { get; set; } = default!;
        public string BoardGameFrontImageUrl { get; set; } = string.Empty;

        // Dictionary for Marker Type Images
        public Dictionary<long, string?> MarkerImagesBase64 { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null) return NotFound();

            BoardGame = await _context.BoardGames
                .Include(bg => bg.FkBgdBoardGameTypeNavigation)
                .Include(bg => bg.FkBgdBoardGameVictoryConditionTypeNavigation)
                .Include(bg => bg.FkBgdPublisherNavigation)
                .Include(bg => bg.BoardGameMarkers)
                    .ThenInclude(marker => marker.FkBgdBoardGameMarkerTypeNavigation)
                .Include(bg => bg.BoardGameShelfSections)
                    .ThenInclude(ss => ss.FkBgdShelfSectionNavigation)
                .FirstOrDefaultAsync(bg => bg.Id == id);

            if (BoardGame == null)
            {
                return NotFound();
            }

            // 1. Fetch Front Image
            var frontImageType = await _context.BoardGameImageTypes
                .FirstOrDefaultAsync(bgit => bgit.TypeDesc == "Board Game Front");

            if (frontImageType != null && BoardGame.Gid != Guid.Empty)
            {
                var image = await _boardGameImages.Find(img =>
                    img.GID == BoardGame.Gid && img.ImageTypeGID == frontImageType.Gid)
                    .FirstOrDefaultAsync();

                if (image?.ImageBytes != null)
                {
                    BoardGameFrontImageUrl = $"data:{image.ContentType};base64,{Convert.ToBase64String(image.ImageBytes)}";
                }
            }

            // 2. Fetch Marker Images
            // Optimization: Fetch all unique marker type GIDs first, then query Mongo once
            if (BoardGame.BoardGameMarkers.Any())
            {
                var markerTypeGids = BoardGame.BoardGameMarkers
                   .Select(m => (Guid?)m.FkBgdBoardGameMarkerTypeNavigation?.Gid)
                   .Where(g => g.HasValue)
                   .Distinct()
                   .ToList();

                if (markerTypeGids.Any())
                {
                    var filter = Builders<BoardGameImages>.Filter.And(
                        Builders<BoardGameImages>.Filter.Eq(x => x.SQLTable, "bgd.BoardGameMarkerType"),
                        Builders<BoardGameImages>.Filter.In(x => x.GID, markerTypeGids)
                    );

                    var images = await _boardGameImages.Find(filter).ToListAsync();

                    foreach (var marker in BoardGame.BoardGameMarkers)
                    {
                        var type = marker.FkBgdBoardGameMarkerTypeNavigation;
                        if (type != null && !MarkerImagesBase64.ContainsKey(type.Id))
                        {
                            var img = images.FirstOrDefault(x => x.GID == type.Gid);
                            if (img != null && img.ImageBytes != null)
                            {
                                MarkerImagesBase64[type.Id] = $"data:{img.ContentType};base64,{Convert.ToBase64String(img.ImageBytes)}";
                            }
                            else
                            {
                                MarkerImagesBase64[type.Id] = null;
                            }
                        }
                    }
                }
            }

            return Page();
        }

        // --- Helper Methods ---
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
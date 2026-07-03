using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Board_Game_Software.Data;
using Board_Game_Software.Models;
using Board_Game_Software.Services;

namespace Board_Game_Software.Pages.DataSetup.BoardGameMarkerTypes
{
    public class DetailsModel : PageModel
    {
        private readonly BoardGameDbContext _context;
        private readonly ICurrentClubService _currentClubService;

        public DetailsModel(BoardGameDbContext context, ICurrentClubService currentClubService)
        {
            _context = context;
            _currentClubService = currentClubService;
        }

        public BoardGameMarkerType BoardGameMarkerType { get; set; } = default!;
        public IList<BoardGameMarker> BoardGameMarkers { get; set; } = default!;

        // Holds the main image for the Marker Type
        public string? MarkerTypeImageUrl { get; set; }

        // Holds the COVER IMAGES for the linked Board Games
        public Dictionary<long, string> BoardGameImageUrls { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null) return NotFound();
            var club = await _currentClubService.GetCurrentClubAsync();

            // 1. Fetch Main Marker Type
            var boardgamemarkertype = await _context.BoardGameMarkerTypes
                .Include(b => b.FkBgdMarkerAdditionalTypeNavigation)
                .Include(b => b.FkBgdMarkerAlignmentTypeNavigation)
                .FirstOrDefaultAsync(m => m.Id == id
                    && (club.IsPlatformAdminMode || m.FkBgdClub == null || m.FkBgdClub == club.CurrentClubId));

            if (boardgamemarkertype == null) return NotFound();
            BoardGameMarkerType = boardgamemarkertype;

            // 2. Fetch Associated Markers AND the Linked Board Game
            BoardGameMarkers = await _context.BoardGameMarkers
                .Include(m => m.FkBgdBoardGameNavigation) // <--- Crucial: Load the Game info
                .Where(m => !m.Inactive
                    && m.FkBgdBoardGameMarkerType == id
                    && m.FkBgdBoardGameNavigation != null
                    && !m.FkBgdBoardGameNavigation.Inactive
                    && (club.IsPlatformAdminMode
                        ? m.FkBgdBoardGameNavigation.FkBgdClub == null
                        : m.FkBgdBoardGameNavigation.FkBgdClub == club.CurrentClubId))
                .OrderBy(m => m.FkBgdBoardGameNavigation!.BoardGameName)
                .ToListAsync();

            MarkerTypeImageUrl = $"/media/marker-type/{BoardGameMarkerType.Gid:D}";

            // 4. Fetch Images for the LINKED BOARD GAMES
            if (BoardGameMarkers.Any())
            {
                foreach (var marker in BoardGameMarkers)
                {
                    BoardGameImageUrls[marker.Id] = $"/media/boardgame/front/{marker.FkBgdBoardGameNavigation.Gid:D}";
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

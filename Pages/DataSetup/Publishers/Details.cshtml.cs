using Board_Game_Software.Data;
using Board_Game_Software.Models;
using Board_Game_Software.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Board_Game_Software.Pages.DataSetup.Publishers
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

        public Publisher Publisher { get; set; } = default!;

        // Hero Image
        public string? PublisherLogoUrl { get; set; }

        // List of games + Dictionary for their images
        public IList<BoardGame> RelatedGames { get; set; } = new List<BoardGame>();
        public Dictionary<long, string> RelatedGameImages { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(long id)
        {
            var currentClub = await _currentClubService.GetCurrentClubAsync();
            var publisher = await _context.Publishers
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id
                    && (currentClub.IsPlatformAdminMode || p.FkBgdClub == null || p.FkBgdClub == currentClub.CurrentClubId));

            if (publisher == null) return NotFound();

            Publisher = publisher;

            PublisherLogoUrl = $"/media/publisher/{Publisher.Gid:D}";

            // 3. Fetch Related Games
            // We need to query the BoardGames table using the Publisher ID
            var relatedGameQuery = _context.BoardGames
                .AsNoTracking()
                .Where(bg => !bg.Inactive && bg.FkBgdPublisher == id);

            if (currentClub.IsPlatformAdminMode)
            {
                relatedGameQuery = relatedGameQuery.Where(bg => bg.FkBgdClub == null);
            }
            else if (currentClub.CurrentClubId.HasValue)
            {
                var currentClubId = currentClub.CurrentClubId.Value;
                relatedGameQuery = relatedGameQuery.Where(bg => bg.FkBgdClub == currentClubId);
            }
            else
            {
                relatedGameQuery = relatedGameQuery.Where(bg => false);
            }

            RelatedGames = await relatedGameQuery
                .OrderBy(bg => bg.BoardGameName)
                .ToListAsync();

            foreach (var game in RelatedGames)
            {
                RelatedGameImages[game.Id] = $"/media/boardgame/front/{game.Gid:D}";
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

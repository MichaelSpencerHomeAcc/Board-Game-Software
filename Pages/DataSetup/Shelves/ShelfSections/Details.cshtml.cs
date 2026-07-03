using Board_Game_Software.Data;
using Board_Game_Software.Models;
using Board_Game_Software.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Board_Game_Software.Pages.DataSetup.Shelves.ShelfSections
{
    public class DetailsModel : PageModel
    {
        private readonly BoardGameDbContext _context;
        private readonly ICurrentClubService _currentClubService;

        public DetailsModel(
            BoardGameDbContext context,
            ICurrentClubService currentClubService)
        {
            _context = context;
            _currentClubService = currentClubService;
        }

        public ShelfSection ShelfSection { get; set; } = default!;
        public decimal RemainingWidth { get; set; }
        public double PercentFull { get; set; }

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null) return NotFound();
            var currentClub = await _currentClubService.GetCurrentClubAsync();
            if (!currentClub.CurrentClubId.HasValue) return Forbid();

            var shelfSection = await _context.ShelfSections
                .Include(s => s.FkBgdShelfNavigation)
                .Include(s => s.BoardGameShelfSections)
                    .ThenInclude(bgss => bgss.FkBgdBoardGameNavigation)
                .FirstOrDefaultAsync(m => m.Id == id && m.FkBgdShelfNavigation.FkBgdClub == currentClub.CurrentClubId.Value);

            if (shelfSection == null) return NotFound();

            ShelfSection = shelfSection;

            // 1. Calculate space
            decimal usedWidth = ShelfSection.BoardGameShelfSections
                .Where(bgss => bgss.FkBgdBoardGameNavigation != null)
                .Sum(bgss => bgss.FkBgdBoardGameNavigation.WidthCm ?? 0m);

            RemainingWidth = ShelfSection.WidthCm - usedWidth;
            PercentFull = ShelfSection.WidthCm > 0 ? (double)((usedWidth / ShelfSection.WidthCm) * 100) : 0;

            return Page();
        }
        public async Task<IActionResult> OnPostRemoveGameAsync(long sectionId, long gameId)
        {
            var currentClub = await _currentClubService.GetCurrentClubAsync();
            if (!currentClub.CurrentClubId.HasValue) return Forbid();

            var link = await _context.BoardGameShelfSections
                .Include(x => x.FkBgdShelfSectionNavigation)
                    .ThenInclude(ss => ss.FkBgdShelfNavigation)
                .FirstOrDefaultAsync(x => x.FkBgdShelfSection == sectionId
                    && x.FkBgdBoardGame == gameId
                    && x.FkBgdShelfSectionNavigation.FkBgdShelfNavigation.FkBgdClub == currentClub.CurrentClubId.Value);

            if (link != null)
            {
                _context.BoardGameShelfSections.Remove(link);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage(new { id = sectionId });
        }
    }
}

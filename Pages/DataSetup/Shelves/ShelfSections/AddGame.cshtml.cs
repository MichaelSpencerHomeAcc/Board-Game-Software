using Board_Game_Software.Data;
using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Board_Game_Software.Pages.DataSetup.Shelves.ShelfSections
{
    public class AddGameModel : PageModel
    {
        private readonly BoardGameDbContext _context;

        public AddGameModel(BoardGameDbContext context)
        {
            _context = context;
        }

        public ShelfSection ShelfSection { get; set; } = default!;
        public List<SelectListItem> AvailableGames { get; set; } = new();
        public Dictionary<long, string> OccupiedGames { get; set; } = new();
        public Dictionary<long, decimal> GameWidths { get; set; } = new();
        public decimal RemainingWidth { get; set; }

        [BindProperty]
        public long SelectedGameId { get; set; }

        public async Task<IActionResult> OnGetAsync(long id)
        {
            ShelfSection = await _context.ShelfSections
                .Include(s => s.FkBgdShelfNavigation)
                .Include(s => s.BoardGameShelfSections)
                    .ThenInclude(bgss => bgss.FkBgdBoardGameNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (ShelfSection == null) return NotFound();

            // 1. Calculate Space
            decimal usedWidth = ShelfSection.BoardGameShelfSections
                .Sum(bgss => bgss.FkBgdBoardGameNavigation?.WidthCm ?? 0m);
            RemainingWidth = (ShelfSection.WidthCm) - usedWidth;

            // 2. Map occupied games for relocation warnings
            var assignedLinks = await _context.BoardGameShelfSections
                .Include(bgss => bgss.FkBgdShelfSectionNavigation)
                    .ThenInclude(ss => ss.FkBgdShelfNavigation)
                .ToListAsync();

            OccupiedGames = assignedLinks
                .GroupBy(x => x.FkBgdBoardGame)
                .ToDictionary(
                    g => g.Key,
                    g => {
                        var first = g.First().FkBgdShelfSectionNavigation;
                        return $"{first?.FkBgdShelfNavigation?.ShelfName} / {first?.SectionName}";
                    }
                );

            // 3. Load games and widths
            var allGames = await _context.BoardGames.OrderBy(bg => bg.BoardGameName).ToListAsync();
            AvailableGames = allGames.Select(bg => new SelectListItem
            {
                Value = bg.Id.ToString(),
                Text = bg.BoardGameName + (bg.WidthCm.HasValue ? $" ({bg.WidthCm}cm)" : "")
            }).ToList();

            GameWidths = allGames.ToDictionary(bg => bg.Id, bg => bg.WidthCm ?? 0m);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(long id)
        {
            if (SelectedGameId == 0) return await OnGetAsync(id);

            var section = await _context.ShelfSections
                .Include(s => s.BoardGameShelfSections)
                    .ThenInclude(bgss => bgss.FkBgdBoardGameNavigation)
                .FirstOrDefaultAsync(s => s.Id == id);

            var gameToMove = await _context.BoardGames.FirstOrDefaultAsync(bg => bg.Id == SelectedGameId);

            if (section == null || gameToMove == null) return NotFound();

            // Validation: Width check
            if (gameToMove.WidthCm == null || gameToMove.WidthCm <= 0)
            {
                ModelState.AddModelError(string.Empty, $"Incomplete Data: '{gameToMove.BoardGameName}' needs a recorded width.");
                return await OnGetAsync(id);
            }

            // Validation: Capacity check
            decimal currentUsed = section.BoardGameShelfSections.Sum(x => x.FkBgdBoardGameNavigation?.WidthCm ?? 0m);
            if (currentUsed + (gameToMove.WidthCm ?? 0m) > section.WidthCm)
            {
                ModelState.AddModelError(string.Empty, $"Capacity Error: {gameToMove.BoardGameName} ({gameToMove.WidthCm}cm) won't fit in the remaining {section.WidthCm - currentUsed}cm.");
                return await OnGetAsync(id);
            }

            // Remove existing link if moving from another shelf
            var existingLink = await _context.BoardGameShelfSections.FirstOrDefaultAsync(x => x.FkBgdBoardGame == SelectedGameId);
            if (existingLink != null) _context.BoardGameShelfSections.Remove(existingLink);

            try
            {
                string user = User.Identity?.Name ?? "System";
                var newLink = new BoardGameShelfSection
                {
                    FkBgdBoardGame = SelectedGameId,
                    FkBgdShelfSection = id,
                    TimeCreated = DateTime.Now,
                    CreatedBy = user,
                    ModifiedBy = user,
                    Gid = Guid.NewGuid()
                };

                // Safety Loop: Fix any DateTime.MinValue overflows
                var props = newLink.GetType().GetProperties();
                foreach (var p in props.Where(x => x.PropertyType == typeof(DateTime) || x.PropertyType == typeof(DateTime?)))
                {
                    var val = p.GetValue(newLink);
                    if (val is DateTime dt && dt == DateTime.MinValue)
                    {
                        p.SetValue(newLink, DateTime.Now);
                    }
                }

                _context.BoardGameShelfSections.Add(newLink);
                await _context.SaveChangesAsync();
                return RedirectToPage("./Details", new { id = id });
            }
            catch (DbUpdateException ex)
            {
                // DEEP DEBUG LOGIC: Digs into the database engine's specific complaint
                var errorMessage = ex.InnerException?.InnerException?.Message
                                   ?? ex.InnerException?.Message
                                   ?? ex.Message;

                ModelState.AddModelError(string.Empty, $"Database Error: {errorMessage}");
                return await OnGetAsync(id);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"General Error: {ex.Message}");
                return await OnGetAsync(id);
            }
        }
    }
}
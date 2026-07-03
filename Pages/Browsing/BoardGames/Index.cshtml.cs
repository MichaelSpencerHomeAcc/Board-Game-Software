using Board_Game_Software.Data;
using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Board_Game_Software.Pages.Browsing.BoardGames
{
    public class IndexModel : PageModel
    {
        private readonly BoardGameDbContext _context;

        public IndexModel(BoardGameDbContext context)
        {
            _context = context;
        }

        public List<BoardGameViewModel> BoardGames { get; set; } = new();
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; } = 25;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; } = string.Empty;

        public async Task OnGetAsync(string? search, int pageNumber = 1)
        {
            SearchTerm = search ?? string.Empty;
            PageNumber = Math.Max(1, pageNumber);
            var linkedExpansionIds = _context.BoardGameExpansions
                .Where(link => !link.Inactive)
                .Select(link => link.FkBgdExpansionBoardGame);

            // Read-only index: NO TRACKING
            var query = _context.BoardGames
                .AsNoTracking()
                .Include(bg => bg.FkBgdPublisherNavigation)
                .Include(bg => bg.FkBgdBoardGameTypeNavigation)
                .Include(bg => bg.BoardGameExpansionBaseGames)
                    .ThenInclude(link => link.FkBgdExpansionBoardGameNavigation)
                .Where(bg => !bg.Inactive
                    && !bg.IsExpansion
                    && !linkedExpansionIds.Contains(bg.Id));

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var cleanSearch = SearchTerm.Replace("min", "", StringComparison.OrdinalIgnoreCase).Trim();
                bool isNumber = int.TryParse(cleanSearch, out int numericValue);

                if (isNumber)
                {
                    query = query.Where(bg =>
                        (bg.PlayerCountMin <= numericValue && numericValue <= bg.PlayerCountMax)
                        || bg.BoardGameExpansionBaseGames.Any(link =>
                            !link.Inactive
                            && !link.FkBgdExpansionBoardGameNavigation.Inactive
                            && link.FkBgdExpansionBoardGameNavigation.PlayerCountMin <= numericValue
                            && numericValue <= link.FkBgdExpansionBoardGameNavigation.PlayerCountMax)
                        || (bg.PlayingTimeMinInMinutes <= numericValue && numericValue <= bg.PlayingTimeMaxInMinutes)
                        || bg.BoardGameName.Contains(SearchTerm)
                        || (bg.FkBgdBoardGameTypeNavigation != null && bg.FkBgdBoardGameTypeNavigation.TypeDesc.Contains(SearchTerm))
                    );
                }
                else
                {
                    query = query.Where(bg =>
                        bg.BoardGameName.Contains(SearchTerm)
                        || (bg.FkBgdBoardGameTypeNavigation != null && bg.FkBgdBoardGameTypeNavigation.TypeDesc.Contains(SearchTerm))
                    );
                }
            }

            TotalCount = await query.CountAsync();
            PageNumber = Math.Min(PageNumber, Math.Max(TotalPages, 1));

            var games = await query
                .OrderBy(bg => bg.BoardGameName)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            BoardGames = games.Select(game => new BoardGameViewModel
            {
                BoardGame = game,
                ImageUrl = $"/media/boardgame/front/{game.Gid}",
                ExpansionNames = game.BoardGameExpansionBaseGames
                    .Where(link => !link.Inactive && !link.FkBgdExpansionBoardGameNavigation.Inactive)
                    .Select(link => link.FkBgdExpansionBoardGameNavigation.BoardGameName)
                    .OrderBy(name => name)
                    .ToList(),
                ExpansionPlayerSummaries = game.BoardGameExpansionBaseGames
                    .Where(link => !link.Inactive && !link.FkBgdExpansionBoardGameNavigation.Inactive)
                    .Select(link => BuildExpansionPlayerSummary(game, link.FkBgdExpansionBoardGameNavigation))
                    .Where(summary => summary != null)
                    .Select(summary => summary!)
                    .OrderByDescending(summary => summary.CombinedMaxPlayers)
                    .ThenBy(summary => summary.ExpansionName)
                    .ToList()
            }).ToList();
        }

        public async Task<JsonResult> OnGetSearchAsync(string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return new JsonResult(new List<object>());

            var cleanSearch = term.Replace("min", "", StringComparison.OrdinalIgnoreCase).Trim();
            bool isNumber = int.TryParse(cleanSearch, out int numericValue);
            var linkedExpansionIds = _context.BoardGameExpansions
                .Where(link => !link.Inactive)
                .Select(link => link.FkBgdExpansionBoardGame);

            var query = _context.BoardGames
                .AsNoTracking()
                .Include(bg => bg.FkBgdBoardGameTypeNavigation)
                .Include(bg => bg.BoardGameExpansionBaseGames)
                    .ThenInclude(link => link.FkBgdExpansionBoardGameNavigation)
                .Where(bg => !bg.Inactive
                    && !bg.IsExpansion
                    && !linkedExpansionIds.Contains(bg.Id));

            if (isNumber)
            {
                query = query.Where(bg =>
                    (bg.PlayerCountMin <= numericValue && numericValue <= bg.PlayerCountMax)
                    || bg.BoardGameExpansionBaseGames.Any(link =>
                        !link.Inactive
                        && !link.FkBgdExpansionBoardGameNavigation.Inactive
                        && link.FkBgdExpansionBoardGameNavigation.PlayerCountMin <= numericValue
                        && numericValue <= link.FkBgdExpansionBoardGameNavigation.PlayerCountMax)
                    || (bg.PlayingTimeMinInMinutes <= numericValue && numericValue <= bg.PlayingTimeMaxInMinutes)
                    || bg.BoardGameName.Contains(term)
                    || (bg.FkBgdBoardGameTypeNavigation != null && bg.FkBgdBoardGameTypeNavigation.TypeDesc.Contains(term))
                );
            }
            else
            {
                query = query.Where(bg =>
                    bg.BoardGameName.Contains(term) ||
                    (bg.FkBgdBoardGameTypeNavigation != null && bg.FkBgdBoardGameTypeNavigation.TypeDesc.Contains(term))
                );
            }

            var suggestions = await query
                .OrderBy(bg => bg.BoardGameName)
                .Select(bg => new { name = bg.BoardGameName, type = bg.FkBgdBoardGameTypeNavigation != null ? bg.FkBgdBoardGameTypeNavigation.TypeDesc : "" })
                .Take(10)
                .ToListAsync();

            return new JsonResult(suggestions);
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id, string? search, int pageNumber = 1)
        {
            if (!User.IsInRole("Admin")) return Forbid();

            var game = await _context.BoardGames.FirstOrDefaultAsync(bg => bg.Id == id);
            if (game == null) return NotFound();

            game.Inactive = true;
            game.ModifiedBy = User.Identity?.Name ?? "System";
            game.TimeModified = DateTime.Now;

            await _context.SaveChangesAsync();
            return RedirectToPage(new { search, pageNumber });
        }

        private static ExpansionPlayerSummary? BuildExpansionPlayerSummary(BoardGame baseGame, BoardGame expansion)
        {
            if (!baseGame.PlayerCountMin.HasValue || !baseGame.PlayerCountMax.HasValue ||
                !expansion.PlayerCountMin.HasValue || !expansion.PlayerCountMax.HasValue)
            {
                return null;
            }

            var combinedMin = Math.Min(baseGame.PlayerCountMin.Value, expansion.PlayerCountMin.Value);
            var combinedMax = Math.Max(baseGame.PlayerCountMax.Value, expansion.PlayerCountMax.Value);

            if (combinedMin == baseGame.PlayerCountMin.Value && combinedMax == baseGame.PlayerCountMax.Value)
            {
                return null;
            }

            return new ExpansionPlayerSummary
            {
                ExpansionName = expansion.BoardGameName,
                CombinedPlayerRange = FormatPlayerRange(combinedMin, combinedMax),
                CombinedMaxPlayers = combinedMax
            };
        }

        private static string FormatPlayerRange(byte minPlayers, byte maxPlayers)
        {
            return minPlayers == maxPlayers ? minPlayers.ToString() : $"{minPlayers}-{maxPlayers}";
        }
    }

    public class BoardGameViewModel
    {
        public BoardGame BoardGame { get; set; } = null!;
        public string? ImageUrl { get; set; }
        public List<string> ExpansionNames { get; set; } = new();
        public List<ExpansionPlayerSummary> ExpansionPlayerSummaries { get; set; } = new();
    }

    public class ExpansionPlayerSummary
    {
        public string ExpansionName { get; set; } = string.Empty;
        public string CombinedPlayerRange { get; set; } = string.Empty;
        public int CombinedMaxPlayers { get; set; }
    }
}

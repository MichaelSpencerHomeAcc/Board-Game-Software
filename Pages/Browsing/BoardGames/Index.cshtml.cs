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

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; } = string.Empty;

        public async Task OnGetAsync(string? search)
        {
            SearchTerm = search ?? string.Empty;

            // Read-only index: NO TRACKING
            var query = _context.BoardGames
                .AsNoTracking()
                .Include(bg => bg.FkBgdPublisherNavigation)
                .Include(bg => bg.FkBgdBoardGameTypeNavigation)
                .Where(bg => !bg.Inactive);

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var cleanSearch = SearchTerm.Replace("min", "", StringComparison.OrdinalIgnoreCase).Trim();
                bool isNumber = int.TryParse(cleanSearch, out int numericValue);

                if (isNumber)
                {
                    query = query.Where(bg =>
                        (bg.PlayerCountMin <= numericValue && numericValue <= bg.PlayerCountMax)
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

            var games = await query
                .OrderBy(bg => bg.BoardGameName)
                .Take(50)
                .ToListAsync();

            BoardGames = games.Select(game => new BoardGameViewModel
            {
                BoardGame = game,
                ImageUrl = $"/media/boardgame/front/{game.Gid}"
            }).ToList();
        }

        public async Task<JsonResult> OnGetSearchAsync(string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return new JsonResult(new List<object>());

            var cleanSearch = term.Replace("min", "", StringComparison.OrdinalIgnoreCase).Trim();
            bool isNumber = int.TryParse(cleanSearch, out int numericValue);

            var query = _context.BoardGames
                .AsNoTracking()
                .Include(bg => bg.FkBgdBoardGameTypeNavigation)
                .Where(bg => !bg.Inactive);

            if (isNumber)
            {
                query = query.Where(bg =>
                    (bg.PlayerCountMin <= numericValue && numericValue <= bg.PlayerCountMax)
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
    }

    public class BoardGameViewModel
    {
        public BoardGame BoardGame { get; set; } = null!;
        public string? ImageUrl { get; set; }
    }
}

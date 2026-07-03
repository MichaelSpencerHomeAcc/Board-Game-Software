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

namespace Board_Game_Software.Pages.Browsing.BoardGames
{
    public class IndexModel : PageModel
    {
        private readonly BoardGameDbContext _context;
        private readonly ICurrentClubService _currentClubService;

        public IndexModel(BoardGameDbContext context, ICurrentClubService currentClubService)
        {
            _context = context;
            _currentClubService = currentClubService;
        }

        public List<BoardGameViewModel> BoardGames { get; set; } = new();
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; } = 25;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
        public CurrentClubContext CurrentClub { get; private set; } = CurrentClubContext.Empty;
        public bool CanImportTemplates => CurrentClub.CurrentClubId.HasValue && (User.IsInRole("Admin") || CurrentClub.CanManageCurrentClub);

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public bool Templates { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        public async Task OnGetAsync(string? search, int pageNumber = 1, bool templates = false)
        {
            SearchTerm = search ?? string.Empty;
            PageNumber = Math.Max(1, pageNumber);
            CurrentClub = await _currentClubService.GetCurrentClubAsync();
            Templates = templates && CurrentClub.CurrentClubId.HasValue;
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

            query = ApplyClubScope(query, CurrentClub);

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
            var currentClub = await _currentClubService.GetCurrentClubAsync();

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

            query = ApplyClubScope(query, currentClub);

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
            var currentClub = await _currentClubService.GetCurrentClubAsync();
            if (!CanManageGame(game, currentClub)) return NotFound();

            game.Inactive = true;
            game.ModifiedBy = User.Identity?.Name ?? "System";
            game.TimeModified = DateTime.Now;

            await _context.SaveChangesAsync();
            return RedirectToPage(new { search, pageNumber });
        }

        public async Task<IActionResult> OnPostImportTemplateAsync(long id, string? search, int pageNumber = 1)
        {
            CurrentClub = await _currentClubService.GetCurrentClubAsync();
            if (!CurrentClub.CurrentClubId.HasValue || !(User.IsInRole("Admin") || CurrentClub.CanManageCurrentClub))
            {
                return Forbid();
            }

            var currentClubId = CurrentClub.CurrentClubId.Value;
            var template = await _context.BoardGames
                .AsNoTracking()
                .Include(bg => bg.BoardGameMarkers.Where(m => !m.Inactive))
                .Include(bg => bg.BoardGameEloMethods.Where(m => !m.Inactive))
                .FirstOrDefaultAsync(bg => bg.Id == id && !bg.Inactive && bg.FkBgdClub == null);
            if (template == null) return NotFound();

            var alreadyImported = await _context.BoardGames.AnyAsync(bg =>
                !bg.Inactive &&
                bg.FkBgdClub == currentClubId &&
                (bg.FkBgdTemplateBoardGame == template.Id || bg.BoardGameName == template.BoardGameName));

            if (alreadyImported)
            {
                StatusMessage = $"{template.BoardGameName} is already in this club's library.";
                return RedirectToPage(new { search, pageNumber, templates = true });
            }

            var now = DateTime.UtcNow;
            var actor = User.Identity?.Name ?? "system";
            var clubGame = new BoardGame
            {
                Gid = Guid.NewGuid(),
                Inactive = false,
                CreatedBy = actor,
                ModifiedBy = actor,
                TimeCreated = now,
                TimeModified = now,
                BoardGameName = template.BoardGameName,
                FkBgdBoardGameType = template.FkBgdBoardGameType,
                FkBgdBoardGameVictoryConditionType = template.FkBgdBoardGameVictoryConditionType,
                FkBgdPublisher = template.FkBgdPublisher,
                FkBgdClub = currentClubId,
                FkBgdTemplateBoardGame = template.Id,
                PlayerCountMin = template.PlayerCountMin,
                PlayerCountMax = template.PlayerCountMax,
                PlayingTimeMinInMinutes = template.PlayingTimeMinInMinutes,
                PlayingTimeMaxInMinutes = template.PlayingTimeMaxInMinutes,
                ComplexityRating = template.ComplexityRating,
                ReleaseDate = template.ReleaseDate,
                HasMarkers = template.HasMarkers,
                IsExpansion = template.IsExpansion,
                HeightCm = template.HeightCm,
                WidthCm = template.WidthCm,
                BoardGameSummary = template.BoardGameSummary,
                HowToPlayHyperlink = template.HowToPlayHyperlink
            };

            _context.BoardGames.Add(clubGame);
            await _context.SaveChangesAsync();

            var markerCopies = template.BoardGameMarkers
                .Where(marker => !marker.Inactive)
                .Select(marker => new BoardGameMarker
                {
                    Gid = Guid.NewGuid(),
                    Inactive = false,
                    CreatedBy = actor,
                    ModifiedBy = actor,
                    TimeCreated = now,
                    TimeModified = now,
                    FkBgdBoardGame = clubGame.Id,
                    FkBgdBoardGameMarkerType = marker.FkBgdBoardGameMarkerType
                });

            var eloCopies = template.BoardGameEloMethods
                .Where(method => !method.Inactive)
                .Select(method => new BoardGameEloMethod
                {
                    Gid = Guid.NewGuid(),
                    Inactive = false,
                    CreatedBy = actor,
                    ModifiedBy = actor,
                    TimeCreated = now,
                    TimeModified = now,
                    FkBgdBoardGame = clubGame.Id,
                    FkBgdEloMethod = method.FkBgdEloMethod,
                    ExpectedWinRatioTeamA = method.ExpectedWinRatioTeamA,
                    Notes = method.Notes
                });

            _context.BoardGameMarkers.AddRange(markerCopies);
            _context.BoardGameEloMethods.AddRange(eloCopies);
            await _context.SaveChangesAsync();

            StatusMessage = $"{template.BoardGameName} was added to {CurrentClub.CurrentClubName}.";
            return RedirectToPage(new { search, pageNumber, templates = true });
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

        private IQueryable<BoardGame> ApplyClubScope(IQueryable<BoardGame> query, CurrentClubContext currentClub)
        {
            if (Templates && currentClub.CurrentClubId.HasValue)
            {
                var currentClubId = currentClub.CurrentClubId.Value;
                var importedTemplateIds = _context.BoardGames
                    .Where(bg => !bg.Inactive && bg.FkBgdClub == currentClubId && bg.FkBgdTemplateBoardGame.HasValue)
                    .Select(bg => bg.FkBgdTemplateBoardGame!.Value);

                var importedNames = _context.BoardGames
                    .Where(bg => !bg.Inactive && bg.FkBgdClub == currentClubId)
                    .Select(bg => bg.BoardGameName);

                return query.Where(bg =>
                    bg.FkBgdClub == null &&
                    !_context.BoardGames.Any(other =>
                        !other.Inactive &&
                        other.FkBgdClub == null &&
                        other.BoardGameName == bg.BoardGameName &&
                        other.IsExpansion == bg.IsExpansion &&
                        other.Id < bg.Id) &&
                    !importedTemplateIds.Contains(bg.Id) &&
                    !importedNames.Contains(bg.BoardGameName));
            }

            if (User.IsInRole("Admin") && currentClub.IsPlatformAdminMode)
            {
                return query.Where(bg =>
                    bg.FkBgdClub == null &&
                    !_context.BoardGames.Any(other =>
                        !other.Inactive &&
                        other.FkBgdClub == null &&
                        other.BoardGameName == bg.BoardGameName &&
                        other.IsExpansion == bg.IsExpansion &&
                        other.Id < bg.Id));
            }

            if (currentClub.CurrentClubId.HasValue)
            {
                var currentClubId = currentClub.CurrentClubId.Value;
                return query.Where(bg =>
                    bg.FkBgdClub == currentClubId &&
                    !_context.BoardGames.Any(other =>
                        !other.Inactive &&
                        other.FkBgdClub == currentClubId &&
                        other.BoardGameName == bg.BoardGameName &&
                        other.IsExpansion == bg.IsExpansion &&
                        other.Id < bg.Id));
            }

            if (User.IsInRole("Admin"))
            {
                return query;
            }

            return query.Where(bg => false);
        }

        private static bool CanManageGame(BoardGame boardGame, CurrentClubContext currentClub)
        {
            if (currentClub.IsPlatformAdminMode)
            {
                return boardGame.FkBgdClub == null;
            }

            return currentClub.CurrentClubId.HasValue
                && boardGame.FkBgdClub == currentClub.CurrentClubId.Value;
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

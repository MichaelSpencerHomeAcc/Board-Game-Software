using BoardGameClubSoftware.Storage;
using Board_Game_Software.Data;
using Board_Game_Software.Models;
using Board_Game_Software.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Board_Game_Software.Pages.Browsing.BoardGames
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly BoardGameDbContext _context;
        private readonly ICurrentClubService _currentClubService;
        private readonly IImageUploadValidator _imageUploadValidator;
        private readonly ImageService _imageService;

        public EditModel(
            BoardGameDbContext context,
            ICurrentClubService currentClubService,
            IImageUploadValidator imageUploadValidator,
            ImageService imageService)
        {
            _context = context;
            _currentClubService = currentClubService;
            _imageUploadValidator = imageUploadValidator;
            _imageService = imageService;
        }

        [BindProperty]
        public BoardGame BoardGame { get; set; } = default!;

        [BindProperty]
        public IFormFile? ImageUpload { get; set; }

        [BindProperty]
        public List<long> MarkerTypeIds { get; set; } = new();

        [BindProperty]
        public List<long> ExpansionBaseGameIds { get; set; } = new();

        [BindProperty]
        public long? SelectedEloMethodId { get; set; }

        public List<BoardGameMarker> ExistingMarkers { get; set; } = new();
        public List<MarkerTypeViewModel> AvailableMarkerTypes { get; set; } = new();
        public List<long> ExistingExpansionBaseGameIds { get; set; } = new();

        public SelectList BoardGameTypes { get; set; } = default!;
        public SelectList VictoryConditions { get; set; } = default!;
        public SelectList Publishers { get; set; } = default!;
        public SelectList EloMethods { get; set; } = default!;
        public SelectList BaseGames { get; set; } = default!;
        public string? CurrentImageUrl { get; set; }
        public List<string> DebugModelStateErrors { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(long id)
        {
            var boardGame = await _context.BoardGames
                .AsNoTracking()
                .Include(bg => bg.BoardGameEloMethods)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (boardGame == null) return NotFound();
            var currentClub = await _currentClubService.GetCurrentClubAsync();
            if (!CanManageGame(boardGame, currentClub)) return NotFound();

            BoardGame = boardGame;

            SelectedEloMethodId = BoardGame.BoardGameEloMethods
                .FirstOrDefault(x => !x.Inactive)?.FkBgdEloMethod;

            await ReloadPageData(id);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(long id)
        {
            var gameToUpdate = await _context.BoardGames
                .Include(bg => bg.BoardGameEloMethods)
                .Include(bg => bg.BoardGameExpansionExpansionGames)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (gameToUpdate == null) return NotFound();
            if (gameToUpdate.GameStatus is BoardGameDefaults.RejectedStatus or BoardGameDefaults.MergedStatus) return NotFound();
            var currentClub = await _currentClubService.GetCurrentClubAsync();
            if (!CanManageGame(gameToUpdate, currentClub)) return NotFound();

            var existingMarkers = await _context.BoardGameMarkers
                .Where(m => m.FkBgdBoardGame == id)
                .ToListAsync();

            var updated = await TryUpdateModelAsync(gameToUpdate, "BoardGame",
                b => b.BoardGameName, b => b.FkBgdBoardGameType, b => b.PlayerCountMin,
                b => b.PlayerCountMax, b => b.ReleaseDate, b => b.PlayingTimeMinInMinutes,
                b => b.PlayingTimeMaxInMinutes, b => b.HasMarkers, b => b.ComplexityRating,
                b => b.HeightCm, b => b.WidthCm, b => b.IsExpansion, b => b.BoardGameSummary,
                b => b.HowToPlayHyperlink, b => b.FkBgdBoardGameVictoryConditionType,
                b => b.FkBgdPublisher);

            RemoveNavigationModelStateErrors();

            ImageUploadValidationResult? imageValidation = null;
            if (ImageUpload != null && ImageUpload.Length > 0)
            {
                imageValidation = _imageUploadValidator.Validate(ImageUpload);
                if (!imageValidation.IsValid)
                {
                    ModelState.AddModelError(nameof(ImageUpload), imageValidation.ErrorMessage!);
                }
            }

            if (ModelState.IsValid)
            {
                var now = DateTime.Now;
                var actor = User.Identity?.Name ?? "system";

                if (gameToUpdate.IsExpansion && (ExpansionBaseGameIds == null || !ExpansionBaseGameIds.Any()))
                {
                    ModelState.AddModelError(nameof(ExpansionBaseGameIds), "Select at least one base game for this expansion.");
                    BoardGame = gameToUpdate;
                    await ReloadPageData(id);
                    return Page();
                }

                gameToUpdate.ModifiedBy = actor;
                gameToUpdate.TimeModified = now;
                gameToUpdate.NormalizedName = BoardGameDefaults.NormalizeName(gameToUpdate.BoardGameName);

                // ELO LOGIC
                var currentEloLink = gameToUpdate.BoardGameEloMethods.FirstOrDefault(x => !x.Inactive);
                if (SelectedEloMethodId.HasValue)
                {
                    if (currentEloLink == null)
                    {
                        _context.BoardGameEloMethods.Add(new BoardGameEloMethod
                        {
                            Gid = Guid.NewGuid(),
                            FkBgdBoardGame = id,
                            FkBgdEloMethod = SelectedEloMethodId.Value,
                            CreatedBy = actor,
                            TimeCreated = now,
                            ModifiedBy = actor,
                            TimeModified = now
                        });
                    }
                    else if (currentEloLink.FkBgdEloMethod != SelectedEloMethodId.Value)
                    {
                        currentEloLink.FkBgdEloMethod = SelectedEloMethodId.Value;
                        currentEloLink.ModifiedBy = actor;
                        currentEloLink.TimeModified = now;
                    }
                }
                else if (currentEloLink != null)
                {
                    currentEloLink.Inactive = true;
                    currentEloLink.ModifiedBy = actor;
                    currentEloLink.TimeModified = now;
                }

                // MARKERS SYNC (fix)
                var desiredTypeIds = (MarkerTypeIds ?? new List<long>())
                    .Distinct()
                    .ToHashSet();
                var allowedTypeIds = (await _context.BoardGameMarkerTypes
                    .Where(t => !t.Inactive && (t.FkBgdClub == null || t.FkBgdClub == gameToUpdate.FkBgdClub))
                    .Select(t => t.Id)
                    .ToListAsync())
                    .ToHashSet();
                desiredTypeIds.IntersectWith(allowedTypeIds);

                if (!gameToUpdate.HasMarkers)
                {
                    foreach (var m in existingMarkers.Where(m => !m.Inactive))
                    {
                        m.Inactive = true;
                        m.ModifiedBy = actor;
                        m.TimeModified = now;
                    }
                }
                else
                {
                    foreach (var m in existingMarkers.Where(m => !m.Inactive))
                    {
                        if (!m.FkBgdBoardGameMarkerType.HasValue ||
                            !desiredTypeIds.Contains(m.FkBgdBoardGameMarkerType.Value))
                        {
                            m.Inactive = true;
                            m.ModifiedBy = actor;
                            m.TimeModified = now;
                        }
                    }

                    foreach (var typeId in desiredTypeIds)
                    {
                        var existing = existingMarkers.FirstOrDefault(m => m.FkBgdBoardGameMarkerType == typeId);

                        if (existing == null)
                        {
                            _context.BoardGameMarkers.Add(new BoardGameMarker
                            {
                                Gid = Guid.NewGuid(),
                                FkBgdBoardGame = id,
                                FkBgdBoardGameMarkerType = typeId,
                                Inactive = false,
                                CreatedBy = actor,
                                TimeCreated = now,
                                ModifiedBy = actor,
                                TimeModified = now
                            });
                        }
                        else if (existing.Inactive)
                        {
                            existing.Inactive = false;
                            existing.ModifiedBy = actor;
                            existing.TimeModified = now;
                        }
                    }
                }

                SyncExpansionLinks(gameToUpdate, actor, now);

                if (ImageUpload != null && ImageUpload.Length > 0)
                {
                    try
                    {
                        await _imageService.UploadGameCoverAsync(
                            checked((int)gameToUpdate.Id),
                            ImageUpload,
                            actor,
                            HttpContext.RequestAborted);
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError(nameof(ImageUpload), $"Image upload failed: {ex.Message}");
                        BoardGame = gameToUpdate;
                        CaptureModelStateDebug();
                        await ReloadPageData(id);
                        return Page();
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToPage("./BoardGameDetails", new { id = gameToUpdate.Id });
            }

            BoardGame = gameToUpdate;
            CaptureModelStateDebug();
            await ReloadPageData(id);
            return Page();
        }

        private void RemoveNavigationModelStateErrors()
        {
            foreach (var key in ModelState.Keys
                .Where(key => key.EndsWith("Navigation", StringComparison.Ordinal)
                    || key.Contains(".FkBgdBoardGameNavigation", StringComparison.Ordinal)
                    || key.Contains(".FkBgdExpansionBoardGameNavigation", StringComparison.Ordinal))
                .ToList())
            {
                ModelState.Remove(key);
            }
        }

        private void CaptureModelStateDebug()
        {
            DebugModelStateErrors = ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .SelectMany(entry => entry.Value!.Errors.Select(error =>
                    $"{entry.Key}: {error.ErrorMessage}"))
                .ToList();
        }

        private async Task ReloadPageData(long id)
        {
            ExistingMarkers = await _context.BoardGameMarkers
                .AsNoTracking()
                .Include(m => m.FkBgdBoardGameMarkerTypeNavigation)
                .Where(m => m.FkBgdBoardGame == id)
                .ToListAsync();

            await LoadSelectLists();
            await LoadExpansionBaseGames(id);
            await LoadMarkerTypes();
            if (BoardGame != null) await LoadCurrentImageUrl(BoardGame.Id);
        }

        private void SyncExpansionLinks(BoardGame gameToUpdate, string actor, DateTime now)
        {
            var desiredBaseGameIds = gameToUpdate.IsExpansion
                ? (ExpansionBaseGameIds ?? new List<long>())
                    .Where(baseGameId => baseGameId != gameToUpdate.Id)
                    .Distinct()
                    .ToHashSet()
                : new HashSet<long>();

            foreach (var link in gameToUpdate.BoardGameExpansionExpansionGames)
            {
                if (!desiredBaseGameIds.Contains(link.FkBgdBoardGame) && !link.Inactive)
                {
                    link.Inactive = true;
                    link.ModifiedBy = actor;
                    link.TimeModified = now;
                }
            }

            foreach (var baseGameId in desiredBaseGameIds)
            {
                var existing = gameToUpdate.BoardGameExpansionExpansionGames
                    .FirstOrDefault(link => link.FkBgdBoardGame == baseGameId);

                if (existing == null)
                {
                    _context.BoardGameExpansions.Add(new BoardGameExpansion
                    {
                        Gid = Guid.NewGuid(),
                        FkBgdBoardGame = baseGameId,
                        FkBgdExpansionBoardGame = gameToUpdate.Id,
                        Inactive = false,
                        CreatedBy = actor,
                        TimeCreated = now,
                        ModifiedBy = actor,
                        TimeModified = now
                    });
                }
                else if (existing.Inactive)
                {
                    existing.Inactive = false;
                    existing.ModifiedBy = actor;
                    existing.TimeModified = now;
                }
            }
        }

        private async Task LoadSelectLists()
        {
            var catalogClubId = BoardGame?.FkBgdClub ?? await GetCurrentCatalogClubIdAsync();
            BoardGameTypes = new SelectList(await _context.BoardGameTypes.AsNoTracking().Where(t => !t.Inactive).OrderBy(t => t.TypeDesc).ToListAsync(), "Id", "TypeDesc");
            VictoryConditions = new SelectList(await _context.BoardGameVictoryConditionTypes.AsNoTracking().Where(t => !t.Inactive).OrderBy(t => t.TypeDesc).ToListAsync(), "Id", "TypeDesc");
            Publishers = new SelectList(await _context.Publishers.AsNoTracking().Where(p => !p.Inactive && (p.FkBgdClub == null || p.FkBgdClub == catalogClubId)).OrderBy(p => p.PublisherName).ToListAsync(), "Id", "PublisherName");
            EloMethods = new SelectList(await _context.EloMethods.AsNoTracking().Where(e => !e.Inactive).OrderBy(e => e.MethodName).ToListAsync(), "Id", "MethodName");
        }

        private async Task LoadExpansionBaseGames(long id)
        {
            var catalogClubId = BoardGame?.FkBgdClub ?? await GetCurrentCatalogClubIdAsync();
            var linkedExpansionIds = _context.BoardGameExpansions
                .Where(link => !link.Inactive)
                .Select(link => link.FkBgdExpansionBoardGame);

            ExistingExpansionBaseGameIds = await _context.BoardGameExpansions
                .AsNoTracking()
                .Where(link => !link.Inactive && link.FkBgdExpansionBoardGame == id)
                .Select(link => link.FkBgdBoardGame)
                .ToListAsync();

            BaseGames = new SelectList(
                await _context.BoardGames
                    .AsNoTracking()
                    .Where(bg => !bg.Inactive
                        && !bg.IsExpansion
                        && bg.FkBgdClub == catalogClubId
                        && bg.Id != id
                        && !linkedExpansionIds.Contains(bg.Id))
                    .OrderBy(bg => bg.BoardGameName)
                    .ToListAsync(),
                "Id",
                "BoardGameName",
                ExistingExpansionBaseGameIds);
        }

        // PERFORMANCE FIX: Batch stored image lookups (no N+1)
        private async Task LoadMarkerTypes()
        {
            AvailableMarkerTypes.Clear();
            var existingIds = ExistingMarkers.Where(m => !m.Inactive).Select(m => m.FkBgdBoardGameMarkerType).ToHashSet();
            var catalogClubId = BoardGame?.FkBgdClub ?? await GetCurrentCatalogClubIdAsync();

            var types = await _context.BoardGameMarkerTypes
                .AsNoTracking()
                .Where(t => !t.Inactive && !existingIds.Contains(t.Id) && (t.FkBgdClub == null || t.FkBgdClub == catalogClubId))
                .OrderBy(t => t.TypeDesc)
                .ToListAsync();

            var typeIds = types.Select(t => checked((int)t.Id)).ToList();
            var imgById = await _context.StoredImages
                .AsNoTracking()
                .Where(image => image.OwnerType == ImageService.MarkerTypeImageOwnerType && typeIds.Contains(image.OwnerId))
                .GroupBy(image => image.OwnerId)
                .Select(group => group.OrderByDescending(image => image.CreatedAtUtc).First())
                .ToDictionaryAsync(image => image.OwnerId);

            foreach (var type in types)
            {
                imgById.TryGetValue(checked((int)type.Id), out var img);

                AvailableMarkerTypes.Add(new MarkerTypeViewModel
                {
                    Id = type.Id,
                    TypeDesc = type.TypeDesc,
                    ImageUrl = img?.PublicUrl
                });
            }
        }

        private async Task LoadCurrentImageUrl(long id)
        {
            CurrentImageUrl = await _context.StoredImages
                .AsNoTracking()
                .Where(image => image.OwnerType == ImageService.GameCoverOwnerType && image.OwnerId == checked((int)id))
                .OrderByDescending(image => image.CreatedAtUtc)
                .Select(image => image.PublicUrl)
                .FirstOrDefaultAsync();
        }

        private async Task<long?> GetCurrentCatalogClubIdAsync()
        {
            var club = await _currentClubService.GetCurrentClubAsync();
            return club.HasClub && !club.IsPlatformAdminMode ? club.CurrentClubId : null;
        }

        private bool CanManageGame(BoardGame boardGame, CurrentClubContext currentClub)
        {
            if (currentClub.IsPlatformAdminMode)
            {
                return User.IsInRole("Admin") && boardGame.FkBgdClub == null;
            }

            return (User.IsInRole("Admin") || currentClub.CanManageCurrentClub)
                && currentClub.CurrentClubId.HasValue
                && boardGame.FkBgdClub == currentClub.CurrentClubId.Value;
        }
    }

    public class MarkerTypeViewModel
    {
        public long Id { get; set; }
        public string TypeDesc { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
    }
}

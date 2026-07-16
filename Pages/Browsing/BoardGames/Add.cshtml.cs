using BoardGameClubSoftware.Storage;
using Board_Game_Software.Data;
using Board_Game_Software.Models;
using Board_Game_Software.Pages.Browsing.BoardGames;
using Board_Game_Software.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Board_Game_Software.Pages.Browsing.BoardGames
{
    [Authorize]
    public class AddModel : PageModel
    {
        private readonly BoardGameDbContext _context;
        private readonly ICurrentClubService _currentClubService;
        private readonly IImageUploadValidator _imageUploadValidator;
        private readonly ImageService _imageService;

        public AddModel(
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
        public BoardGame BoardGame { get; set; } = new();

        [BindProperty]
        public IFormFile? ImageUpload { get; set; }

        [BindProperty]
        public List<long> MarkerTypeIds { get; set; } = new();

        [BindProperty]
        public List<long> ExpansionBaseGameIds { get; set; } = new();

        [BindProperty]
        public long? SelectedEloMethodId { get; set; }

        [BindProperty]
        public bool ConfirmAddDespiteMatches { get; set; }

        public List<MarkerTypeViewModel> AvailableMarkerTypes { get; set; } = new();
        public List<DuplicateCandidate> DuplicateCandidates { get; private set; } = new();
        public SelectList BoardGameTypes { get; set; } = default!;
        public SelectList VictoryConditions { get; set; } = default!;
        public SelectList Publishers { get; set; } = default!;
        public SelectList EloMethods { get; set; } = default!;
        public SelectList BaseGames { get; set; } = default!;

        public async Task<IActionResult> OnGet(string? missingName)
        {
            if (!await CanAddGameAsync())
            {
                return Forbid();
            }

            if (!string.IsNullOrWhiteSpace(missingName))
            {
                BoardGame.BoardGameName = missingName.Trim();
            }

            await LoadSelectLists();
            await LoadAvailableMarkerTypes();
            await LoadDuplicateCandidatesAsync(BoardGame.BoardGameName);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!await CanAddGameAsync())
            {
                return Forbid();
            }

            // 1. Set Audit and Identity Fields
            string user = User.Identity?.Name ?? "system";
            BoardGame.CreatedBy = user;
            BoardGame.ModifiedBy = user;
            BoardGame.TimeCreated = DateTime.Now;
            BoardGame.TimeModified = DateTime.Now;
            BoardGame.Gid = Guid.NewGuid();
            BoardGame.FkBgdClub = await GetCurrentCatalogClubIdAsync();
            BoardGame.NormalizedName = BoardGameDefaults.NormalizeName(BoardGame.BoardGameName);
            BoardGame.SubmittedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (BoardGame.FkBgdClub.HasValue)
            {
                BoardGame.GameStatus = BoardGameDefaults.PendingStatus;
                BoardGame.GameSource = BoardGameDefaults.ClubSubmittedSource;
                BoardGame.LocalGameStatus = BoardGameDefaults.LocalOnlyStatus;
            }
            else
            {
                BoardGame.GameStatus = BoardGameDefaults.ApprovedStatus;
                BoardGame.GameSource = BoardGameDefaults.AdminCreatedSource;
                BoardGame.LocalGameStatus = null;
            }

            // 2. Bypass Validation for background-set fields
            ModelState.Remove("BoardGame.CreatedBy");
            ModelState.Remove("BoardGame.ModifiedBy");
            ModelState.Remove("BoardGame.Gid");
            ModelState.Remove("BoardGame.NormalizedName");
            ModelState.Remove("BoardGame.GameStatus");
            ModelState.Remove("BoardGame.GameSource");

            if (BoardGame.IsExpansion && (ExpansionBaseGameIds == null || !ExpansionBaseGameIds.Any()))
            {
                ModelState.AddModelError(nameof(ExpansionBaseGameIds), "Select at least one base game for this expansion.");
            }

            await LoadDuplicateCandidatesAsync(BoardGame.BoardGameName);
            if (DuplicateCandidates.Any() && !ConfirmAddDespiteMatches)
            {
                ModelState.AddModelError(nameof(BoardGame.BoardGameName), "Possible matching games were found. Choose an existing game or confirm that this is a separate game.");
            }

            ImageUploadValidationResult? imageValidation = null;
            if (ImageUpload != null && ImageUpload.Length > 0)
            {
                imageValidation = _imageUploadValidator.Validate(ImageUpload);
                if (!imageValidation.IsValid)
                {
                    ModelState.AddModelError(nameof(ImageUpload), imageValidation.ErrorMessage!);
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadSelectLists();
                await LoadAvailableMarkerTypes();
                return Page();
            }

            // Using a transaction to keep SQL changes together.
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 3. Add Game to SQL
                _context.BoardGames.Add(BoardGame);
                await _context.SaveChangesAsync();

                // 4. Handle Elo Method Link
                if (SelectedEloMethodId.HasValue)
                {
                    _context.BoardGameEloMethods.Add(new BoardGameEloMethod
                    {
                        Gid = Guid.NewGuid(),
                        FkBgdBoardGame = BoardGame.Id,
                        FkBgdEloMethod = SelectedEloMethodId.Value,
                        CreatedBy = user,
                        ModifiedBy = user,
                        TimeCreated = DateTime.Now,
                        TimeModified = DateTime.Now
                    });

                    await _context.SaveChangesAsync();
                }

                // 5. Handle Bulk Marker Addition
                if (BoardGame.HasMarkers && MarkerTypeIds != null && MarkerTypeIds.Any())
                {
                    var catalogClubId = BoardGame.FkBgdClub;
                    var allowedMarkerTypeIds = await _context.BoardGameMarkerTypes
                        .Where(t => !t.Inactive && (t.FkBgdClub == null || t.FkBgdClub == catalogClubId))
                        .Select(t => t.Id)
                        .ToListAsync();

                    var markersToAdd = MarkerTypeIds
                        .Distinct()
                        .Where(typeId => allowedMarkerTypeIds.Contains(typeId))
                        .Select(typeId => new BoardGameMarker
                        {
                            Gid = Guid.NewGuid(),
                            FkBgdBoardGame = BoardGame.Id,
                            FkBgdBoardGameMarkerType = typeId,
                            CreatedBy = user,
                            ModifiedBy = user,
                            TimeCreated = DateTime.Now,
                            TimeModified = DateTime.Now
                        });

                    _context.BoardGameMarkers.AddRange(markersToAdd);
                    await _context.SaveChangesAsync();
                }

                // 6. Handle Expansion Links
                if (BoardGame.IsExpansion && ExpansionBaseGameIds != null && ExpansionBaseGameIds.Any())
                {
                    var expansionLinks = ExpansionBaseGameIds
                        .Where(id => id != BoardGame.Id)
                        .Distinct()
                        .Select(baseGameId => new BoardGameExpansion
                        {
                            Gid = Guid.NewGuid(),
                            FkBgdBoardGame = baseGameId,
                            FkBgdExpansionBoardGame = BoardGame.Id,
                            CreatedBy = user,
                            ModifiedBy = user,
                            TimeCreated = DateTime.Now,
                            TimeModified = DateTime.Now
                        });

                    _context.BoardGameExpansions.AddRange(expansionLinks);
                    await _context.SaveChangesAsync();
                }

                // 7. Handle Box Art Upload
                if (ImageUpload != null && ImageUpload.Length > 0)
                {
                    try
                    {
                        await _imageService.UploadGameCoverAsync(
                            checked((int)BoardGame.Id),
                            ImageUpload,
                            User.Identity?.Name,
                            HttpContext.RequestAborted);
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError(nameof(ImageUpload), $"Image upload failed: {ex.Message}");
                        throw;
                    }
                }

                await transaction.CommitAsync();
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                // Extract inner exception message if available for better error trapping
                if (!ModelState.ContainsKey(nameof(ImageUpload)) ||
                    ModelState[nameof(ImageUpload)]?.Errors.Count == 0)
                {
                    var errorMsg = ex.InnerException?.Message ?? ex.Message;
                    ModelState.AddModelError(string.Empty, $"Critical Error: {errorMsg}");
                }

                await LoadSelectLists();
                await LoadAvailableMarkerTypes();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostQuickAddPublisherAsync(string publisherName)
        {
            if (!await CanAddGameAsync())
            {
                return Forbid();
            }

            var name = publisherName?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(new { message = "Publisher name is required." });
            }

            if (name.Length > 80)
            {
                return BadRequest(new { message = "Publisher name must be 80 characters or fewer." });
            }

            var catalogClubId = await GetCurrentCatalogClubIdAsync();
            var existing = await _context.Publishers
                .Where(p => !p.Inactive && p.PublisherName == name && (p.FkBgdClub == null || p.FkBgdClub == catalogClubId))
                .OrderByDescending(p => p.FkBgdClub == catalogClubId)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                return new JsonResult(new { id = existing.Id, name = existing.PublisherName });
            }

            var now = DateTime.Now;
            var actor = User.Identity?.Name ?? "system";
            var publisher = new Publisher
            {
                Gid = Guid.NewGuid(),
                PublisherName = name,
                FkBgdClub = catalogClubId,
                CreatedBy = actor,
                ModifiedBy = actor,
                TimeCreated = now,
                TimeModified = now
            };

            _context.Publishers.Add(publisher);
            await _context.SaveChangesAsync();

            return new JsonResult(new { id = publisher.Id, name = publisher.PublisherName });
        }

        public async Task<IActionResult> OnPostQuickAddGameTypeAsync(string typeDesc)
        {
            if (!await CanAddGameAsync())
            {
                return Forbid();
            }

            var name = typeDesc?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(new { message = "Game type is required." });
            }

            if (name.Length > 50)
            {
                return BadRequest(new { message = "Game type must be 50 characters or fewer." });
            }

            var existing = await _context.BoardGameTypes
                .Where(t => !t.Inactive && t.TypeDesc == name)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                return new JsonResult(new { id = existing.Id, name = existing.TypeDesc });
            }

            var now = DateTime.Now;
            var actor = User.Identity?.Name ?? "system";
            var gameType = new BoardGameType
            {
                Gid = Guid.NewGuid(),
                TypeDesc = name,
                CreatedBy = actor,
                ModifiedBy = actor,
                TimeCreated = now,
                TimeModified = now
            };

            _context.BoardGameTypes.Add(gameType);
            await _context.SaveChangesAsync();

            return new JsonResult(new { id = gameType.Id, name = gameType.TypeDesc });
        }

        private async Task LoadSelectLists()
        {
            var catalogClubId = await GetCurrentCatalogClubIdAsync();
            var linkedExpansionIds = _context.BoardGameExpansions
                .Where(link => !link.Inactive)
                .Select(link => link.FkBgdExpansionBoardGame);

            BoardGameTypes = new SelectList(await _context.BoardGameTypes.Where(t => !t.Inactive).OrderBy(t => t.TypeDesc).ToListAsync(), "Id", "TypeDesc");
            VictoryConditions = new SelectList(await _context.BoardGameVictoryConditionTypes.Where(t => !t.Inactive).OrderBy(t => t.TypeDesc).ToListAsync(), "Id", "TypeDesc");
            Publishers = new SelectList(await _context.Publishers.Where(p => !p.Inactive && (p.FkBgdClub == null || p.FkBgdClub == catalogClubId)).OrderBy(p => p.PublisherName).ToListAsync(), "Id", "PublisherName");
            EloMethods = new SelectList(await _context.EloMethods.Where(e => !e.Inactive).OrderBy(e => e.MethodName).ToListAsync(), "Id", "MethodName");
            BaseGames = new SelectList(
                await _context.BoardGames
                    .Where(bg => !bg.Inactive
                        && !bg.IsExpansion
                        && bg.FkBgdClub == catalogClubId
                        && !linkedExpansionIds.Contains(bg.Id))
                    .OrderBy(bg => bg.BoardGameName)
                    .ToListAsync(),
                "Id",
                "BoardGameName");
        }

        private async Task LoadAvailableMarkerTypes()
        {
            var catalogClubId = await GetCurrentCatalogClubIdAsync();
            var types = await _context.BoardGameMarkerTypes
                .Where(t => !t.Inactive && (t.FkBgdClub == null || t.FkBgdClub == catalogClubId))
                .OrderBy(t => t.TypeDesc)
                .ToListAsync();

            var typeIds = types.Select(t => checked((int)t.Id)).ToList();
            var imageMap = await _context.StoredImages
                .AsNoTracking()
                .Where(image => image.OwnerType == ImageService.MarkerTypeImageOwnerType && typeIds.Contains(image.OwnerId))
                .GroupBy(image => image.OwnerId)
                .Select(group => group.OrderByDescending(image => image.CreatedAtUtc).First())
                .ToDictionaryAsync(image => image.OwnerId);

            foreach (var t in types)
            {
                imageMap.TryGetValue(checked((int)t.Id), out var img);
                AvailableMarkerTypes.Add(new MarkerTypeViewModel
                {
                    Id = t.Id,
                    TypeDesc = t.TypeDesc,
                    ImageUrl = img?.PublicUrl
                });
            }
        }

        private async Task<long?> GetCurrentCatalogClubIdAsync()
        {
            var club = await _currentClubService.GetCurrentClubAsync();
            return club.HasClub && !club.IsPlatformAdminMode ? club.CurrentClubId : null;
        }

        private async Task LoadDuplicateCandidatesAsync(string? gameName)
        {
            DuplicateCandidates.Clear();
            if (string.IsNullOrWhiteSpace(gameName))
            {
                return;
            }

            var catalogClubId = await GetCurrentCatalogClubIdAsync();
            var normalizedName = BoardGameDefaults.NormalizeName(gameName);
            if (normalizedName.Length < 3)
            {
                return;
            }

            var primaryWord = normalizedName
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(word => word.Length >= 3)
                .OrderByDescending(word => word.Length)
                .FirstOrDefault() ?? normalizedName;

            var query = _context.BoardGames
                .AsNoTracking()
                .Include(g => g.FkBgdPublisherNavigation)
                .Where(g => !g.Inactive &&
                    g.GameStatus != BoardGameDefaults.RejectedStatus &&
                    g.GameStatus != BoardGameDefaults.MergedStatus &&
                    (g.FkBgdClub == null || g.FkBgdClub == catalogClubId));

            query = query.Where(g =>
                g.NormalizedName == normalizedName ||
                g.NormalizedName.Contains(normalizedName) ||
                normalizedName.Contains(g.NormalizedName) ||
                g.BoardGameAliases.Any(alias => !alias.Inactive &&
                    (alias.NormalizedAliasName == normalizedName ||
                     alias.NormalizedAliasName.Contains(normalizedName) ||
                     normalizedName.Contains(alias.NormalizedAliasName))) ||
                g.NormalizedName.Contains(primaryWord) ||
                g.BoardGameAliases.Any(alias => !alias.Inactive && alias.NormalizedAliasName.Contains(primaryWord)));

            DuplicateCandidates = await query
                .OrderBy(g => g.NormalizedName == normalizedName ? 0 : 1)
                .ThenBy(g => g.FkBgdClub == catalogClubId ? 0 : 1)
                .ThenBy(g => g.BoardGameName)
                .Take(8)
                .Select(g => new DuplicateCandidate
                {
                    Id = g.Id,
                    Gid = g.Gid,
                    Name = g.BoardGameName,
                    PublisherName = g.FkBgdPublisherNavigation == null ? null : g.FkBgdPublisherNavigation.PublisherName,
                    IsClubGame = g.FkBgdClub.HasValue,
                    IsExactMatch = g.NormalizedName == normalizedName ||
                        g.BoardGameAliases.Any(alias => !alias.Inactive && alias.NormalizedAliasName == normalizedName)
                })
                .ToListAsync();
        }

        private async Task<bool> CanAddGameAsync()
        {
            var club = await _currentClubService.GetCurrentClubAsync();
            return User.IsInRole("Admin") || club.CanManageCurrentClub;
        }

        public sealed class DuplicateCandidate
        {
            public long Id { get; init; }
            public Guid Gid { get; init; }
            public string Name { get; init; } = string.Empty;
            public string? PublisherName { get; init; }
            public bool IsClubGame { get; init; }
            public bool IsExactMatch { get; init; }
        }
    }
}

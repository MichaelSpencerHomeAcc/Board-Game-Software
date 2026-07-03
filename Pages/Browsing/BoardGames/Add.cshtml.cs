using Board_Game_Software.Data;
using Board_Game_Software.Models;
using Board_Game_Software.Pages.Browsing.BoardGames;
using Board_Game_Software.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

namespace Board_Game_Software.Pages.Browsing.BoardGames
{
    [Authorize(Roles = "Admin")]
    public class AddModel : PageModel
    {
        private readonly BoardGameDbContext _context;
        private readonly IMongoCollection<BoardGameImages> _boardGameImages;
        private readonly ICurrentClubService _currentClubService;

        public AddModel(BoardGameDbContext context, IMongoClient mongoClient, IConfiguration configuration, ICurrentClubService currentClubService)
        {
            _context = context;
            _currentClubService = currentClubService;
            var databaseName = configuration["MongoDbSettings:Database"];
            var database = mongoClient.GetDatabase(databaseName);
            _boardGameImages = database.GetCollection<BoardGameImages>("BoardGameImages");
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

        public List<MarkerTypeViewModel> AvailableMarkerTypes { get; set; } = new();
        public SelectList BoardGameTypes { get; set; } = default!;
        public SelectList VictoryConditions { get; set; } = default!;
        public SelectList Publishers { get; set; } = default!;
        public SelectList EloMethods { get; set; } = default!;
        public SelectList BaseGames { get; set; } = default!;

        public async Task<IActionResult> OnGet()
        {
            await LoadSelectLists();
            await LoadAvailableMarkerTypes();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // 1. Set Audit and Identity Fields
            string user = User.Identity?.Name ?? "system";
            BoardGame.CreatedBy = user;
            BoardGame.ModifiedBy = user;
            BoardGame.TimeCreated = DateTime.Now;
            BoardGame.TimeModified = DateTime.Now;
            BoardGame.Gid = Guid.NewGuid();
            BoardGame.FkBgdClub = await GetCurrentCatalogClubIdAsync();

            // 2. Bypass Validation for background-set fields
            ModelState.Remove("BoardGame.CreatedBy");
            ModelState.Remove("BoardGame.ModifiedBy");
            ModelState.Remove("BoardGame.Gid");

            if (BoardGame.IsExpansion && (ExpansionBaseGameIds == null || !ExpansionBaseGameIds.Any()))
            {
                ModelState.AddModelError(nameof(ExpansionBaseGameIds), "Select at least one base game for this expansion.");
            }

            if (!ModelState.IsValid)
            {
                await LoadSelectLists();
                await LoadAvailableMarkerTypes();
                return Page();
            }

            // Using a transaction to ensure SQL and MongoDB stay in sync
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

                // 7. Handle Box Art Upload (MongoDB)
                if (ImageUpload != null && ImageUpload.Length > 0)
                {
                    using var ms = new MemoryStream();
                    await ImageUpload.CopyToAsync(ms);

                    var frontImageType = await _context.BoardGameImageTypes
                        .FirstOrDefaultAsync(t => t.TypeDesc == "Board Game Front");

                    if (frontImageType != null)
                    {
                        var newImage = new BoardGameImages
                        {
                            GID = BoardGame.Gid,
                            ImageTypeGID = frontImageType.Gid,
                            SQLTable = "BoardGames",
                            ImageBytes = ms.ToArray(),
                            ContentType = ImageUpload.ContentType,
                            Description = $"Front image for {BoardGame.BoardGameName}"
                        };

                        await _boardGameImages.InsertOneAsync(newImage);
                    }
                }

                await transaction.CommitAsync();
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                // Extract inner exception message if available for better error trapping
                var errorMsg = ex.InnerException?.Message ?? ex.Message;
                ModelState.AddModelError(string.Empty, $"Critical Error: {errorMsg}");

                await LoadSelectLists();
                await LoadAvailableMarkerTypes();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostQuickAddPublisherAsync(string publisherName)
        {
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

            var guids = types.Select(t => (Guid?)t.Gid).ToList();
            var images = await _boardGameImages
                .Find(x => x.SQLTable == "bgd.BoardGameMarkerType" && guids.Contains(x.GID))
                .ToListAsync();
            var imageMap = images
                .Where(x => x.GID.HasValue)
                .GroupBy(x => x.GID!.Value)
                .ToDictionary(x => x.Key, x => x.First());

            foreach (var t in types)
            {
                imageMap.TryGetValue(t.Gid, out var img);
                AvailableMarkerTypes.Add(new MarkerTypeViewModel
                {
                    Id = t.Id,
                    TypeDesc = t.TypeDesc,
                    ImageBase64 = img?.ImageBytes != null ? $"data:{img.ContentType};base64,{Convert.ToBase64String(img.ImageBytes)}" : null
                });
            }
        }

        private async Task<long?> GetCurrentCatalogClubIdAsync()
        {
            var club = await _currentClubService.GetCurrentClubAsync();
            return club.HasClub && !club.IsPlatformAdminMode ? club.CurrentClubId : null;
        }
    }
}

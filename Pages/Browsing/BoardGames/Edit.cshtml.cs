using Board_Game_Software.Data;
using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Board_Game_Software.Pages.Browsing.BoardGames
{
    public class EditModel : PageModel
    {
        private readonly BoardGameDbContext _context;
        private readonly IMongoCollection<BoardGameImages> _boardGameImages;

        public EditModel(BoardGameDbContext context, IMongoClient mongoClient, IConfiguration configuration)
        {
            _context = context;
            var databaseName = configuration["MongoDbSettings:Database"];
            var database = mongoClient.GetDatabase(databaseName);
            _boardGameImages = database.GetCollection<BoardGameImages>("BoardGameImages");
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
                    await UpsertBoardGameFrontImageAsync(gameToUpdate, ImageUpload);
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
            if (BoardGame != null) await LoadCurrentImageUrl(BoardGame.Gid);
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

        private async Task UpsertBoardGameFrontImageAsync(BoardGame game, IFormFile upload)
        {
            // Choose ONE canonical value and use it everywhere (controller + docs).
            // Recommended to match your other endpoints:
            const string sqlTableCanonical = "bgd.BoardGame";

            // Look up the "Board Game Front" type in SQL
            var frontType = await _context.BoardGameImageTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TypeDesc == "Board Game Front");

            if (frontType == null)
                throw new InvalidOperationException("Board Game Front image type not found in BoardGameImageTypes.");

            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                await upload.CopyToAsync(ms);
                bytes = ms.ToArray();
            }

            // Prefer the browser-provided content type, else fall back
            var contentType = !string.IsNullOrWhiteSpace(upload.ContentType)
                ? upload.ContentType
                : "application/octet-stream";

            // Description defaults to "<BoardGameName> Box"
            var description = $"{game.BoardGameName} Box";

            // Filter to find existing front image doc for this board game
            // We allow BOTH SQLTable values so older docs still update cleanly.
            var filter = Builders<BoardGameImages>.Filter.And(
                Builders<BoardGameImages>.Filter.Eq(x => x.GID, game.Gid),
                Builders<BoardGameImages>.Filter.Eq(x => x.ImageTypeGID, frontType.Gid),
                Builders<BoardGameImages>.Filter.In(x => x.SQLTable, new[] { sqlTableCanonical, "BoardGames" })
            );

            var update = Builders<BoardGameImages>.Update
                .Set(x => x.SQLTable, sqlTableCanonical)     // normalize going forward
                .Set(x => x.GID, game.Gid)
                .Set(x => x.ImageTypeGID, frontType.Gid)
                .Set(x => x.Description, description)
                .Set(x => x.ImageBytes, bytes)
                .Set(x => x.ContentType, contentType);

            await _boardGameImages.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
        }

        private async Task LoadSelectLists()
        {
            BoardGameTypes = new SelectList(await _context.BoardGameTypes.AsNoTracking().Where(t => !t.Inactive).OrderBy(t => t.TypeDesc).ToListAsync(), "Id", "TypeDesc");
            VictoryConditions = new SelectList(await _context.BoardGameVictoryConditionTypes.AsNoTracking().Where(t => !t.Inactive).OrderBy(t => t.TypeDesc).ToListAsync(), "Id", "TypeDesc");
            Publishers = new SelectList(await _context.Publishers.AsNoTracking().Where(p => !p.Inactive).OrderBy(p => p.PublisherName).ToListAsync(), "Id", "PublisherName");
            EloMethods = new SelectList(await _context.EloMethods.AsNoTracking().Where(e => !e.Inactive).OrderBy(e => e.MethodName).ToListAsync(), "Id", "MethodName");
        }

        private async Task LoadExpansionBaseGames(long id)
        {
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
                        && bg.Id != id
                        && !linkedExpansionIds.Contains(bg.Id))
                    .OrderBy(bg => bg.BoardGameName)
                    .ToListAsync(),
                "Id",
                "BoardGameName",
                ExistingExpansionBaseGameIds);
        }

        // PERFORMANCE FIX: Batch Mongo image lookups (no N+1)
        private async Task LoadMarkerTypes()
        {
            AvailableMarkerTypes.Clear();
            var existingIds = ExistingMarkers.Where(m => !m.Inactive).Select(m => m.FkBgdBoardGameMarkerType).ToHashSet();

            var types = await _context.BoardGameMarkerTypes
                .AsNoTracking()
                .Where(t => !t.Inactive && !existingIds.Contains(t.Id))
                .OrderBy(t => t.TypeDesc)
                .ToListAsync();

            var gids = types.Select(t => (Guid?)t.Gid).ToList();

            var imgFilter = Builders<BoardGameImages>.Filter.And(
                Builders<BoardGameImages>.Filter.Eq(x => x.SQLTable, "bgd.BoardGameMarkerType"),
                Builders<BoardGameImages>.Filter.In(x => x.GID, gids)
            );

            var imgs = await _boardGameImages.Find(imgFilter).ToListAsync();
            var imgByGid = imgs.Where(i => i.GID.HasValue).GroupBy(i => i.GID!.Value).ToDictionary(g => g.Key, g => g.First());

            foreach (var type in types)
            {
                imgByGid.TryGetValue(type.Gid, out var img);

                AvailableMarkerTypes.Add(new MarkerTypeViewModel
                {
                    Id = type.Id,
                    TypeDesc = type.TypeDesc,
                    ImageBase64 = img?.ImageBytes != null ? $"data:{img.ContentType};base64,{Convert.ToBase64String(img.ImageBytes)}" : null
                });
            }
        }

        private async Task LoadCurrentImageUrl(Guid gid)
        {
            var frontType = await _context.BoardGameImageTypes.AsNoTracking().FirstOrDefaultAsync(t => t.TypeDesc == "Board Game Front");
            if (frontType != null)
            {
                var filter = Builders<BoardGameImages>.Filter.And(
                    Builders<BoardGameImages>.Filter.Eq(i => i.GID, gid),
                    Builders<BoardGameImages>.Filter.Eq(i => i.ImageTypeGID, frontType.Gid),
                    Builders<BoardGameImages>.Filter.In(i => i.SQLTable, new[] { "bgd.BoardGame", "BoardGames" })
                );

                var img = await _boardGameImages.Find(filter).FirstOrDefaultAsync();

                if (img?.ImageBytes != null) CurrentImageUrl = $"data:{img.ContentType};base64,{Convert.ToBase64String(img.ImageBytes)}";
            }
        }
    }

    public class MarkerTypeViewModel
    {
        public long Id { get; set; }
        public string TypeDesc { get; set; } = string.Empty;
        public string? ImageBase64 { get; set; }
    }
}

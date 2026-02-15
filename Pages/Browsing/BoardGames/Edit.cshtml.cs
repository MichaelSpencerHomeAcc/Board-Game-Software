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
        public long? SelectedEloMethodId { get; set; }

        public List<BoardGameMarker> ExistingMarkers { get; set; } = new();
        public List<MarkerTypeViewModel> AvailableMarkerTypes { get; set; } = new();

        public SelectList BoardGameTypes { get; set; } = default!;
        public SelectList VictoryConditions { get; set; } = default!;
        public SelectList Publishers { get; set; } = default!;
        public SelectList EloMethods { get; set; } = default!;
        public string? CurrentImageUrl { get; set; }

        public async Task<IActionResult> OnGetAsync(long id)
        {
            BoardGame = await _context.BoardGames
                .AsNoTracking()
                .Include(bg => bg.BoardGameEloMethods)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (BoardGame == null) return NotFound();

            SelectedEloMethodId = BoardGame.BoardGameEloMethods
                .FirstOrDefault(x => !x.Inactive)?.FkBgdEloMethod;

            await ReloadPageData(id);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(long id)
        {
            var gameToUpdate = await _context.BoardGames
                .Include(bg => bg.BoardGameEloMethods)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (gameToUpdate == null) return NotFound();

            var existingMarkers = await _context.BoardGameMarkers
                .Where(m => m.FkBgdBoardGame == id)
                .ToListAsync();

            if (await TryUpdateModelAsync(gameToUpdate, "BoardGame",
                b => b.BoardGameName, b => b.FkBgdBoardGameType, b => b.PlayerCountMin,
                b => b.PlayerCountMax, b => b.ReleaseDate, b => b.PlayingTimeMinInMinutes,
                b => b.PlayingTimeMaxInMinutes, b => b.HasMarkers, b => b.ComplexityRating,
                b => b.HeightCm, b => b.WidthCm, b => b.BoardGameSummary,
                b => b.HowToPlayHyperlink, b => b.FkBgdBoardGameVictoryConditionType,
                b => b.FkBgdPublisher))
            {
                var now = DateTime.Now;
                var actor = User.Identity?.Name ?? "system";

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

                if (ImageUpload != null && ImageUpload.Length > 0)
                {
                    await UpsertBoardGameFrontImageAsync(gameToUpdate, ImageUpload);
                }

                await _context.SaveChangesAsync();
                return RedirectToPage("./BoardGameDetails", new { id = gameToUpdate.Id });
            }

            BoardGame = gameToUpdate;
            await ReloadPageData(id);
            return Page();
        }

        private async Task ReloadPageData(long id)
        {
            ExistingMarkers = await _context.BoardGameMarkers
                .AsNoTracking()
                .Include(m => m.FkBgdBoardGameMarkerTypeNavigation)
                .Where(m => m.FkBgdBoardGame == id)
                .ToListAsync();

            await LoadSelectLists();
            await LoadMarkerTypes();
            if (BoardGame != null) await LoadCurrentImageUrl(BoardGame.Gid);
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

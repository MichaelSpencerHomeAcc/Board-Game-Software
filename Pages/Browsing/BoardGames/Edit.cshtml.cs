using Board_Game_Software.Data;
using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using SixLabors.ImageSharp;



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

            // Load existing markers up-front so we can sync them
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

                // ----- ELO LOGIC (unchanged) -----
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

                // ----- MARKERS: FIX (this is what was missing) -----
                var desiredTypeIds = (MarkerTypeIds ?? new List<long>())
                    .Distinct()
                    .ToHashSet();

                if (!gameToUpdate.HasMarkers)
                {
                    // Markers switched off => inactivate all existing marker rows
                    foreach (var m in existingMarkers.Where(m => !m.Inactive))
                    {
                        m.Inactive = true;
                        m.ModifiedBy = actor;
                        m.TimeModified = now;
                    }
                }
                else
                {
                    // Inactivate removed markers
                    foreach (var m in existingMarkers.Where(m => !m.Inactive))
                    {
                        // If FK is null OR not desired => inactivate
                        if (!m.FkBgdBoardGameMarkerType.HasValue ||
                            !desiredTypeIds.Contains(m.FkBgdBoardGameMarkerType.Value))
                        {
                            m.Inactive = true;
                            m.ModifiedBy = actor;
                            m.TimeModified = now;
                        }
                    }

                    // Add new markers (or reactivate inactive ones)
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

                // Save everything in one go
                await _context.SaveChangesAsync();
                return RedirectToPage("./BoardGameDetails", new { id = gameToUpdate.Id });
            }

            // If validation failed, refresh page state
            BoardGame = gameToUpdate;
            await ReloadPageData(id);
            return Page();
        }

        private async Task ReloadPageData(long id)
        {
            ExistingMarkers = await _context.BoardGameMarkers
                .Include(m => m.FkBgdBoardGameMarkerTypeNavigation)
                .Where(m => m.FkBgdBoardGame == id)
                .ToListAsync();

            await LoadSelectLists();
            await LoadMarkerTypes();

            if (BoardGame != null)
                await LoadCurrentImageUrl(BoardGame.Gid);
        }

        private async Task LoadSelectLists()
        {
            BoardGameTypes = new SelectList(
                await _context.BoardGameTypes.Where(t => !t.Inactive).OrderBy(t => t.TypeDesc).ToListAsync(),
                "Id", "TypeDesc");

            VictoryConditions = new SelectList(
                await _context.BoardGameVictoryConditionTypes.Where(t => !t.Inactive).OrderBy(t => t.TypeDesc).ToListAsync(),
                "Id", "TypeDesc");

            Publishers = new SelectList(
                await _context.Publishers.Where(p => !p.Inactive).OrderBy(p => p.PublisherName).ToListAsync(),
                "Id", "PublisherName");

            EloMethods = new SelectList(
                await _context.EloMethods.Where(e => !e.Inactive).OrderBy(e => e.MethodName).ToListAsync(),
                "Id", "MethodName");
        }

        // PERFORMANCE FIX: Batch Mongo image lookups (no N+1)
        private async Task LoadMarkerTypes()
        {
            AvailableMarkerTypes.Clear();

            var existingIds = ExistingMarkers
                .Where(m => !m.Inactive)
                .Select(m => m.FkBgdBoardGameMarkerType)
                .ToHashSet();

            var types = await _context.BoardGameMarkerTypes
                .Where(t => !t.Inactive && !existingIds.Contains(t.Id))
                .OrderBy(t => t.TypeDesc)
                .ToListAsync();

            var gids = types.Select(t => (Guid?)t.Gid).ToList();

            var imgFilter = Builders<BoardGameImages>.Filter.And(
                Builders<BoardGameImages>.Filter.Eq(x => x.SQLTable, "bgd.BoardGameMarkerType"),
                Builders<BoardGameImages>.Filter.In(x => x.GID, gids)
            );

            var imgs = await _boardGameImages.Find(imgFilter).ToListAsync();
            var imgByGid = imgs
                .Where(i => i.GID.HasValue)
                .GroupBy(i => i.GID!.Value)
                .ToDictionary(g => g.Key, g => g.First());

            foreach (var type in types)
            {
                imgByGid.TryGetValue(type.Gid, out var img);

                AvailableMarkerTypes.Add(new MarkerTypeViewModel
                {
                    Id = type.Id,
                    TypeDesc = type.TypeDesc,
                    ImageBase64 = img?.ImageBytes != null ? ToDataUrl(img) : null
                });
            }
        }

        private async Task LoadCurrentImageUrl(Guid gid)
        {
            var frontType = await _context.BoardGameImageTypes
                .FirstOrDefaultAsync(t => t.TypeDesc == "Board Game Front");

            if (frontType != null)
            {
                var img = await _boardGameImages
                    .Find(i => i.GID == gid && i.ImageTypeGID == frontType.Gid)
                    .FirstOrDefaultAsync();

                if (img?.ImageBytes != null)
                    CurrentImageUrl = $"data:{img.ContentType};base64,{Convert.ToBase64String(img.ImageBytes)}";
            }
        }
        private static string ToDataUrl(BoardGameImages img)
        {
            if (img.ImageBytes == null || img.ImageBytes.Length == 0)
                return "";

            var ct = (img.ContentType ?? "").Trim().ToLowerInvariant();

            // If it's WebP, convert to PNG for maximum compatibility
            if (ct == "image/webp" || ct == "image/x-webp" || ct.EndsWith("/webp") || ct.Contains("webp"))
            {
                try
                {
                    using var image = SixLabors.ImageSharp.Image.Load(img.ImageBytes);
                    using var ms = new MemoryStream();
                    image.SaveAsPng(ms);
                    return $"data:image/png;base64,{Convert.ToBase64String(ms.ToArray())}";
                }
                catch
                {
                    // If conversion fails, still attempt to return original bytes
                    return $"data:{(string.IsNullOrWhiteSpace(img.ContentType) ? "image/webp" : img.ContentType)};base64,{Convert.ToBase64String(img.ImageBytes)}";
                }
            }

            // Non-webp: serve as-is
            var safeCt = string.IsNullOrWhiteSpace(img.ContentType) ? "image/png" : img.ContentType;
            return $"data:{safeCt};base64,{Convert.ToBase64String(img.ImageBytes)}";
        }
    }

    public class MarkerTypeViewModel
    {
        public long Id { get; set; }
        public string TypeDesc { get; set; } = string.Empty;
        public string? ImageBase64 { get; set; }
    }
}

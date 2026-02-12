using Board_Game_Software.Data;
using Board_Game_Software.Models;
using Microsoft.AspNetCore.Authorization;
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

namespace Board_Game_Software.Pages.Admin.BoardGames
{
    [Authorize(Roles = "Admin")]
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

        public List<BoardGameMarker> ExistingMarkers { get; set; } = new();
        public List<MarkerTypeViewModel> AvailableMarkerTypes { get; set; } = new();

        public SelectList BoardGameTypes { get; set; } = default!;
        public SelectList VictoryConditions { get; set; } = default!;
        public SelectList Publishers { get; set; } = default!;
        public string? CurrentImageUrl { get; set; }

        public async Task<IActionResult> OnGetAsync(long id)
        {
            BoardGame = await _context.BoardGames
                .Include(bg => bg.FkBgdBoardGameTypeNavigation)
                .Include(bg => bg.FkBgdBoardGameVictoryConditionTypeNavigation)
                .Include(bg => bg.FkBgdPublisherNavigation)
                .FirstOrDefaultAsync(bg => bg.Id == id);

            if (BoardGame == null) return NotFound();

            await ReloadPageData(id);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(long id)
        {
            var gameToUpdate = await _context.BoardGames.FindAsync(id);
            if (gameToUpdate == null) return NotFound();

            // 1. Update the main game properties via TryUpdateModel to include new fields
            if (await TryUpdateModelAsync<BoardGame>(gameToUpdate, "BoardGame",
                b => b.BoardGameName,
                b => b.FkBgdBoardGameType,
                b => b.PlayerCountMin,
                b => b.PlayerCountMax,
                b => b.ReleaseDate,
                b => b.PlayingTimeMinInMinutes,
                b => b.PlayingTimeMaxInMinutes,
                b => b.HasMarkers,
                b => b.ComplexityRating,
                b => b.HeightCm,
                b => b.WidthCm,
                b => b.BoardGameSummary,           // Added
                b => b.HowToPlayHyperlink,         // Added
                b => b.FkBgdBoardGameVictoryConditionType,
                b => b.FkBgdPublisher))
            {
                gameToUpdate.ModifiedBy = User.Identity?.Name ?? "system";
                gameToUpdate.TimeModified = DateTime.Now;

                // 2. Handle Image logic (MongoDB)
                if (ImageUpload != null && ImageUpload.Length > 0)
                {
                    using var ms = new MemoryStream();
                    await ImageUpload.CopyToAsync(ms);
                    var imageBytes = ms.ToArray();

                    var frontImageType = await _context.BoardGameImageTypes
                        .FirstOrDefaultAsync(t => t.TypeDesc == "Board Game Front");

                    if (frontImageType != null)
                    {
                        await _boardGameImages.DeleteManyAsync(img =>
                            img.GID == gameToUpdate.Gid && img.ImageTypeGID == frontImageType.Gid);

                        var newImage = new BoardGameImages
                        {
                            GID = gameToUpdate.Gid,
                            ImageTypeGID = frontImageType.Gid,
                            SQLTable = "BoardGames",
                            ImageBytes = imageBytes,
                            ContentType = ImageUpload.ContentType,
                            Description = $"Updated front image for {gameToUpdate.BoardGameName}"
                        };

                        await _boardGameImages.InsertOneAsync(newImage);
                    }
                }

                // 3. SMART SYNC for Markers
                var currentMarkersInDb = await _context.BoardGameMarkers
                    .Where(m => m.FkBgdBoardGame == id)
                    .ToListAsync();

                if (!gameToUpdate.HasMarkers)
                {
                    _context.BoardGameMarkers.RemoveRange(currentMarkersInDb);
                }
                else
                {
                    var selectedIds = (MarkerTypeIds ?? new List<long>()).Distinct().ToHashSet();

                    // Remove markers no longer selected
                    var toRemove = currentMarkersInDb.Where(m => !selectedIds.Contains(m.FkBgdBoardGameMarkerType ?? 0));
                    _context.BoardGameMarkers.RemoveRange(toRemove);

                    // Add new markers
                    var currentIds = currentMarkersInDb.Select(m => m.FkBgdBoardGameMarkerType ?? 0).ToHashSet();
                    var toAdd = selectedIds.Where(sid => !currentIds.Contains(sid))
                        .Select(newId => new BoardGameMarker
                        {
                            Gid = Guid.NewGuid(),
                            FkBgdBoardGame = id,
                            FkBgdBoardGameMarkerType = newId,
                            CreatedBy = gameToUpdate.CreatedBy,
                            TimeCreated = gameToUpdate.TimeCreated,
                            ModifiedBy = User.Identity?.Name ?? "system",
                            TimeModified = DateTime.Now
                        });

                    _context.BoardGameMarkers.AddRange(toAdd);
                }

                await _context.SaveChangesAsync();
                return RedirectToPage("./BoardGameDetails", new { id = gameToUpdate.Id });
            }

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
            await LoadCurrentImageUrl(BoardGame.Gid);
        }

        private async Task LoadSelectLists()
        {
            BoardGameTypes = new SelectList(await _context.BoardGameTypes.Where(t => !t.Inactive).OrderBy(t => t.TypeDesc).ToListAsync(), "Id", "TypeDesc");
            VictoryConditions = new SelectList(await _context.BoardGameVictoryConditionTypes.Where(t => !t.Inactive).OrderBy(t => t.TypeDesc).ToListAsync(), "Id", "TypeDesc");
            Publishers = new SelectList(await _context.Publishers.Where(p => !p.Inactive).OrderBy(p => p.PublisherName).ToListAsync(), "Id", "PublisherName");
        }

        private async Task LoadMarkerTypes()
        {
            AvailableMarkerTypes.Clear();
            var existingIds = ExistingMarkers.Select(m => m.FkBgdBoardGameMarkerType).ToHashSet();
            var types = await _context.BoardGameMarkerTypes
                .Where(t => !t.Inactive && !existingIds.Contains(t.Id))
                .OrderBy(t => t.TypeDesc)
                .ToListAsync();

            foreach (var type in types)
            {
                var img = await _boardGameImages.Find(x => x.SQLTable == "bgd.BoardGameMarkerType" && x.GID == type.Gid).FirstOrDefaultAsync();
                AvailableMarkerTypes.Add(new MarkerTypeViewModel
                {
                    Id = type.Id,
                    TypeDesc = type.TypeDesc,
                    ImageBase64 = img != null ? $"data:{img.ContentType};base64,{Convert.ToBase64String(img.ImageBytes)}" : null
                });
            }
        }

        private async Task LoadCurrentImageUrl(Guid gid)
        {
            var frontType = await _context.BoardGameImageTypes.FirstOrDefaultAsync(t => t.TypeDesc == "Board Game Front");
            if (frontType != null)
            {
                var img = await _boardGameImages.Find(i => i.GID == gid && i.ImageTypeGID == frontType.Gid).FirstOrDefaultAsync();
                if (img != null) CurrentImageUrl = $"data:{img.ContentType};base64,{Convert.ToBase64String(img.ImageBytes)}";
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
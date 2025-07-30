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
        public BoardGame BoardGame { get; set; }

        [BindProperty]
        public IFormFile? ImageUpload { get; set; }

        // Marker type ids selected from the form
        [BindProperty]
        public List<long> MarkerTypeIds { get; set; } = new();

        public List<BoardGameMarker> ExistingMarkers { get; set; } = new();

        public List<SelectListItem> MarkerTypes { get; set; } = new();

        public bool HasMarkers
        {
            get => BoardGame?.HasMarkers ?? false;
            set
            {
                if (BoardGame != null) BoardGame.HasMarkers = value;
            }
        }

        public SelectList BoardGameTypes { get; set; }
        public SelectList VictoryConditions { get; set; }
        public SelectList Publishers { get; set; }

        public string? CurrentImageUrl { get; set; }

        public async Task<IActionResult> OnGetAsync(long id)
        {
            BoardGame = await _context.BoardGames
                .Include(bg => bg.FkBgdBoardGameTypeNavigation)
                .Include(bg => bg.FkBgdBoardGameVictoryConditionTypeNavigation)
                .Include(bg => bg.FkBgdPublisherNavigation)
                .FirstOrDefaultAsync(bg => bg.Id == id);

            if (BoardGame == null)
            {
                return NotFound();
            }

            // Load markers for this board game
            ExistingMarkers = await _context.BoardGameMarkers
                .Include(m => m.FkBgdBoardGameMarkerTypeNavigation)
                .Where(m => m.FkBgdBoardGame == BoardGame.Id)
                .ToListAsync();

            ExistingMarkers = ExistingMarkers.OrderBy(m => m.FkBgdBoardGameMarkerTypeNavigation?.TypeDesc).ToList();

            await LoadSelectLists();

            // Load marker types filtering out already assigned marker types
            await LoadMarkerTypes();

            await LoadCurrentImageUrl(BoardGame.Gid);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(long id)
        {
            if (!ModelState.IsValid)
            {
                ExistingMarkers = await _context.BoardGameMarkers
                    .Include(m => m.FkBgdBoardGameMarkerTypeNavigation)
                    .Where(m => m.FkBgdBoardGame == id)
                    .ToListAsync();

                await LoadSelectLists();
                await LoadMarkerTypes(); // marker types filtered using ExistingMarkers
                await LoadCurrentImageUrl(BoardGame.Gid);
                return Page();
            }

            // Parse ComplexityRating explicitly
            if (!decimal.TryParse(Request.Form["BoardGame.ComplexityRating"], out var complexity))
            {
                ModelState.AddModelError("BoardGame.ComplexityRating", "Invalid complexity rating format.");

                ExistingMarkers = await _context.BoardGameMarkers
                    .Include(m => m.FkBgdBoardGameMarkerTypeNavigation)
                    .Where(m => m.FkBgdBoardGame == id)
                    .ToListAsync();

                await LoadSelectLists();
                await LoadMarkerTypes();
                await LoadCurrentImageUrl(BoardGame.Gid);
                return Page();
            }

            var boardGameToUpdate = await _context.BoardGames.FindAsync(id);
            if (boardGameToUpdate == null)
            {
                return NotFound();
            }

            // Update main properties
            boardGameToUpdate.BoardGameName = BoardGame.BoardGameName;
            boardGameToUpdate.FkBgdBoardGameType = BoardGame.FkBgdBoardGameType;
            boardGameToUpdate.PlayerCountMin = BoardGame.PlayerCountMin;
            boardGameToUpdate.PlayerCountMax = BoardGame.PlayerCountMax;
            boardGameToUpdate.PlayingTimeMinInMinutes = BoardGame.PlayingTimeMinInMinutes;
            boardGameToUpdate.PlayingTimeMaxInMinutes = BoardGame.PlayingTimeMaxInMinutes;
            boardGameToUpdate.ComplexityRating = complexity;
            boardGameToUpdate.FkBgdBoardGameVictoryConditionType = BoardGame.FkBgdBoardGameVictoryConditionType;
            boardGameToUpdate.FkBgdPublisher = BoardGame.FkBgdPublisher;
            boardGameToUpdate.ReleaseDate = BoardGame.ReleaseDate;
            boardGameToUpdate.BoardGameSummary = BoardGame.BoardGameSummary;
            boardGameToUpdate.HowToPlayHyperlink = BoardGame.HowToPlayHyperlink;
            boardGameToUpdate.HeightCm = BoardGame.HeightCm;
            boardGameToUpdate.WidthCm = BoardGame.WidthCm;
            boardGameToUpdate.HasMarkers = BoardGame.HasMarkers;

            _context.Entry(boardGameToUpdate).State = EntityState.Modified;

            // Handle image upload
            if (ImageUpload != null && ImageUpload.Length > 0)
            {
                using var ms = new MemoryStream();
                await ImageUpload.CopyToAsync(ms);
                var imageBytes = ms.ToArray();

                var frontImageType = await _context.BoardGameImageTypes.FirstOrDefaultAsync(t => t.TypeDesc == "Board Game Front");
                if (frontImageType != null)
                {
                    var existingImages = await _boardGameImages.Find(img =>
                        img.GID == boardGameToUpdate.Gid && img.ImageTypeGID == frontImageType.Gid).ToListAsync();

                    if (existingImages.Any())
                    {
                        await _boardGameImages.DeleteManyAsync(img =>
                            img.GID == boardGameToUpdate.Gid && img.ImageTypeGID == frontImageType.Gid);
                    }

                    var newImage = new BoardGameImages
                    {
                        GID = boardGameToUpdate.Gid,
                        ImageTypeGID = frontImageType.Gid,
                        ImageBytes = imageBytes,
                        ContentType = ImageUpload.ContentType,
                        Description = "Board Game Front Image"
                    };
                    await _boardGameImages.InsertOneAsync(newImage);
                }
            }

            // Update markers only if HasMarkers is true, else remove all markers for this game
            var existingMarkers = await _context.BoardGameMarkers
                .Where(m => m.FkBgdBoardGame == boardGameToUpdate.Id)
                .ToListAsync();

            if (boardGameToUpdate.HasMarkers)
            {
                // Remove markers that were removed by user
                var toRemove = existingMarkers
                    .Where(em => !MarkerTypeIds.Contains(em.FkBgdBoardGameMarkerType ?? 0))
                    .ToList();

                _context.BoardGameMarkers.RemoveRange(toRemove);

                // Add new markers that user added (only once per type)
                var existingMarkerTypeIds = existingMarkers.Select(em => em.FkBgdBoardGameMarkerType ?? 0).ToHashSet();
                var toAddTypeIds = MarkerTypeIds
                    .Where(mt => mt != 0 && !existingMarkerTypeIds.Contains(mt))
                    .Distinct()
                    .ToList();

                foreach (var markerTypeId in toAddTypeIds)
                {
                    var newMarker = new BoardGameMarker
                    {
                        Gid = Guid.NewGuid(),
                        CreatedBy = User.Identity?.Name ?? "system",
                        TimeCreated = DateTime.UtcNow,
                        ModifiedBy = User.Identity?.Name ?? "system",
                        TimeModified = DateTime.UtcNow,
                        FkBgdBoardGame = boardGameToUpdate.Id,
                        FkBgdBoardGameMarkerType = markerTypeId
                    };
                    _context.BoardGameMarkers.Add(newMarker);
                }
            }
            else
            {
                // Remove all markers if HasMarkers is false
                if (existingMarkers.Any())
                {
                    _context.BoardGameMarkers.RemoveRange(existingMarkers);
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToPage("./BoardGameDetails", new { id = boardGameToUpdate.Id });
        }

        private async Task LoadSelectLists()
        {
            BoardGameTypes = new SelectList(await _context.BoardGameTypes.Where(t => !t.Inactive).ToListAsync(), "Id", "TypeDesc");
            VictoryConditions = new SelectList(await _context.BoardGameVictoryConditionTypes.Where(t => !t.Inactive).ToListAsync(), "Id", "TypeDesc");
            Publishers = new SelectList(await _context.Publishers.Where(p => !p.Inactive).ToListAsync(), "Id", "PublisherName");
        }

        private async Task LoadMarkerTypes()
        {
            // Filter out marker types already assigned
            var existingMarkerTypeIds = ExistingMarkers
                .Where(m => m.FkBgdBoardGameMarkerType.HasValue)
                .Select(m => m.FkBgdBoardGameMarkerType.Value)
                .ToHashSet();

            var types = await _context.BoardGameMarkerTypes
                .Where(t => !t.Inactive && !existingMarkerTypeIds.Contains(t.Id))
                .OrderBy(t => t.TypeDesc)
                .ToListAsync();

            MarkerTypes = types
                .Select(t => new SelectListItem(t.TypeDesc, t.Id.ToString()))
                .ToList();
        }

        private async Task LoadCurrentImageUrl(Guid gid)
        {
            var frontImageType = await _context.BoardGameImageTypes.FirstOrDefaultAsync(t => t.TypeDesc == "Board Game Front");
            if (frontImageType != null && gid != Guid.Empty)
            {
                var image = await _boardGameImages.Find(img =>
                    img.GID == gid && img.ImageTypeGID == frontImageType.Gid)
                    .FirstOrDefaultAsync();

                if (image?.ImageBytes != null)
                {
                    var base64 = Convert.ToBase64String(image.ImageBytes);
                    CurrentImageUrl = $"data:{image.ContentType};base64,{base64}";
                }
                else
                {
                    CurrentImageUrl = null;
                }
            }
            else
            {
                CurrentImageUrl = null;
            }
        }
    }
}

using Board_Game_Software.Data;
using Board_Game_Software.Models;
using Board_Game_Software.Pages.Admin.BoardGames;
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

        public AddModel(BoardGameDbContext context, IMongoClient mongoClient, IConfiguration configuration)
        {
            _context = context;
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

        public List<MarkerTypeViewModel> AvailableMarkerTypes { get; set; } = new();
        public SelectList BoardGameTypes { get; set; }
        public SelectList VictoryConditions { get; set; }
        public SelectList Publishers { get; set; }

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

            // 2. Bypass Validation for background-set fields
            ModelState.Remove("BoardGame.CreatedBy");
            ModelState.Remove("BoardGame.ModifiedBy");
            ModelState.Remove("BoardGame.Gid");

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

                // 4. Handle Bulk Marker Addition
                if (BoardGame.HasMarkers && MarkerTypeIds != null && MarkerTypeIds.Any())
                {
                    var markersToAdd = MarkerTypeIds.Distinct().Select(typeId => new BoardGameMarker
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

                // 5. Handle Box Art Upload (MongoDB)
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

        private async Task LoadSelectLists()
        {
            BoardGameTypes = new SelectList(await _context.BoardGameTypes.Where(t => !t.Inactive).OrderBy(t => t.TypeDesc).ToListAsync(), "Id", "TypeDesc");
            VictoryConditions = new SelectList(await _context.BoardGameVictoryConditionTypes.Where(t => !t.Inactive).OrderBy(t => t.TypeDesc).ToListAsync(), "Id", "TypeDesc");
            Publishers = new SelectList(await _context.Publishers.Where(p => !p.Inactive).OrderBy(p => p.PublisherName).ToListAsync(), "Id", "PublisherName");
        }

        private async Task LoadAvailableMarkerTypes()
        {
            var types = await _context.BoardGameMarkerTypes
                .Where(t => !t.Inactive)
                .OrderBy(t => t.TypeDesc)
                .ToListAsync();

            foreach (var t in types)
            {
                var img = await _boardGameImages
                    .Find(x => x.SQLTable == "bgd.BoardGameMarkerType" && x.GID == t.Gid)
                    .FirstOrDefaultAsync();

                AvailableMarkerTypes.Add(new MarkerTypeViewModel
                {
                    Id = t.Id,
                    TypeDesc = t.TypeDesc,
                    ImageBase64 = img != null ? $"data:{img.ContentType};base64,{Convert.ToBase64String(img.ImageBytes)}" : null
                });
            }
        }
    }
}
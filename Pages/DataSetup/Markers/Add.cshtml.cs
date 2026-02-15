using Board_Game_Software.Data;
using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Board_Game_Software.Pages.DataSetup.BoardGameMarkerTypes
{
    public class AddModel : PageModel
    {
        private readonly BoardGameDbContext _context;
        private readonly IMongoCollection<BoardGameImages> _images;

        [BindProperty]
        public BoardGameMarkerType MarkerType { get; set; } = new();

        // NEW: uploaded image for this marker type
        [BindProperty]
        public IFormFile? ImageUpload { get; set; }

        public SelectList AlignmentTypes { get; set; } = null!;

        public AddModel(BoardGameDbContext context, IMongoClient mongoClient, IConfiguration configuration)
        {
            _context = context;

            var dbName = configuration["MongoDbSettings:Database"];
            _images = mongoClient.GetDatabase(dbName).GetCollection<BoardGameImages>("BoardGameImages");
        }

        public async Task OnGetAsync()
        {
            await PopulateAlignmentTypesSelectList(null);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await PopulateAlignmentTypesSelectList(MarkerType.FkBgdMarkerAlignmentType);
                return Page();
            }

            var actor = User.Identity?.Name ?? "system";
            var now = DateTime.UtcNow;

            MarkerType.CreatedBy = actor;
            MarkerType.TimeCreated = now;
            MarkerType.ModifiedBy = actor;
            MarkerType.TimeModified = now;
            MarkerType.Gid = Guid.NewGuid();
            MarkerType.Inactive = false;

            _context.BoardGameMarkerTypes.Add(MarkerType);
            await _context.SaveChangesAsync(); // ensure MarkerType has its final Gid/Id

            // NEW: save uploaded image to Mongo (if provided)
            if (ImageUpload != null && ImageUpload.Length > 0)
            {
                byte[] bytes;
                using (var ms = new MemoryStream())
                {
                    await ImageUpload.CopyToAsync(ms);
                    bytes = ms.ToArray();
                }

                // Replace existing image doc if one already exists for this marker type
                var filter = Builders<BoardGameImages>.Filter.And(
                    Builders<BoardGameImages>.Filter.Eq(x => x.SQLTable, "bgd.BoardGameMarkerType"),
                    Builders<BoardGameImages>.Filter.Eq(x => x.GID, MarkerType.Gid)
                );

                var doc = new BoardGameImages
                {
                    SQLTable = "bgd.BoardGameMarkerType",
                    GID = MarkerType.Gid,
                    ImageBytes = bytes,
                    ContentType = string.IsNullOrWhiteSpace(ImageUpload.ContentType) ? "application/octet-stream" : ImageUpload.ContentType,
                    AvatarFocusX = 50,
                    AvatarFocusY = 50,
                    AvatarZoom = 100,
                    PodiumFocusX = 50,
                    PodiumFocusY = 50,
                    PodiumZoom = 100
                };

                await _images.ReplaceOneAsync(filter, doc, new ReplaceOptions { IsUpsert = true });
            }

            return RedirectToPage("Index");
        }

        private async Task PopulateAlignmentTypesSelectList(long? selectedId)
        {
            var alignmentTypes = await _context.MarkerAlignmentTypes
                .Where(a => !a.Inactive)
                .OrderBy(a => a.TypeDesc)
                .ToListAsync();

            AlignmentTypes = new SelectList(alignmentTypes, "Id", "TypeDesc", selectedId);
        }
    }
}

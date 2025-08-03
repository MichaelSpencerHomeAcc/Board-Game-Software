using Board_Game_Software.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;

namespace Board_Game_Software.Pages.DataSetup.BoardGameMarkerTypes
{
    public class EditModel : PageModel
    {
        private readonly BoardGameDbContext _context;
        private readonly IMongoCollection<BoardGameImages> _imagesCollection;

        public EditModel(BoardGameDbContext context, IMongoClient mongoClient, IConfiguration config)
        {
            _context = context;
            var dbName = config["MongoDbSettings:Database"];
            var database = mongoClient.GetDatabase(dbName);
            _imagesCollection = database.GetCollection<BoardGameImages>("BoardGameImages");
        }

        [BindProperty]
        public BoardGameMarkerType MarkerType { get; set; } = default!;

        [BindProperty]
        public IFormFile? Upload { get; set; }

        public SelectList? AlignmentTypes { get; set; }
        public SelectList? AdditionalTypes { get; set; }
        public string? MarkerImageBase64 { get; set; }

        [BindProperty]
        public string? NewAdditionalTypeDesc { get; set; }

        public async Task<IActionResult> OnGetAsync(long id)
        {
            MarkerType = await _context.BoardGameMarkerTypes
                .Include(m => m.FkBgdMarkerAlignmentTypeNavigation)
                .Include(m => m.FkBgdMarkerAdditionalTypeNavigation)  // Add this!
                .FirstOrDefaultAsync(m => m.Id == id);


            if (MarkerType == null)
            {
                return NotFound();
            }

            await LoadMarkerImage(MarkerType.Gid);
            await PopulateAlignmentTypesSelectList(MarkerType.FkBgdMarkerAlignmentType);
            await PopulateAdditionalTypesSelectList(MarkerType.FkBgdMarkerAdditionalType);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadMarkerImage(MarkerType.Gid);
                await PopulateAlignmentTypesSelectList(MarkerType.FkBgdMarkerAlignmentType);
                await PopulateAdditionalTypesSelectList(MarkerType.FkBgdMarkerAdditionalType);
                return Page();
            }

            var markerInDb = await _context.BoardGameMarkerTypes.FindAsync(MarkerType.Id);

            if (markerInDb == null)
            {
                return NotFound();
            }

            // Update fields
            markerInDb.TypeDesc = MarkerType.TypeDesc;
            markerInDb.FkBgdMarkerAlignmentType = MarkerType.FkBgdMarkerAlignmentType;
            markerInDb.FkBgdMarkerAdditionalType = MarkerType.FkBgdMarkerAdditionalType;
            markerInDb.CustomSort = MarkerType.CustomSort;
            markerInDb.Inactive = MarkerType.Inactive;
            markerInDb.ModifiedBy = User.Identity?.Name ?? "system";
            markerInDb.TimeModified = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Handle image upload
            if (Upload != null && Upload.Length > 0)
            {
                try
                {
                    using var ms = new MemoryStream();
                    await Upload.CopyToAsync(ms);
                    var imageBytes = ms.ToArray();

                    var filter = Builders<BoardGameImages>.Filter.And(
                        Builders<BoardGameImages>.Filter.Eq(x => x.SQLTable, "bgd.BoardGameMarkerType"),
                        Builders<BoardGameImages>.Filter.Eq(x => x.GID, markerInDb.Gid)
                    );

                    var update = Builders<BoardGameImages>.Update
                        .Set(x => x.ImageBytes, imageBytes)
                        .Set(x => x.Description, "Marker Type Image")
                        .Set(x => x.ContentType, Upload.ContentType);

                    var options = new UpdateOptions { IsUpsert = true };

                    await _imagesCollection.UpdateOneAsync(filter, update, options);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, $"Image upload failed: {ex.Message}");
                    await LoadMarkerImage(markerInDb.Gid);
                    await PopulateAlignmentTypesSelectList(markerInDb.FkBgdMarkerAlignmentType);
                    await PopulateAdditionalTypesSelectList(markerInDb.FkBgdMarkerAdditionalType);
                    return Page();
                }
            }

            return RedirectToPage("Index");
        }

        private async Task LoadMarkerImage(Guid gid)
        {
            var filter = Builders<BoardGameImages>.Filter.And(
                Builders<BoardGameImages>.Filter.Eq(x => x.SQLTable, "bgd.BoardGameMarkerType"),
                Builders<BoardGameImages>.Filter.Eq(x => x.GID, gid)
            );

            var imageDoc = await _imagesCollection.Find(filter).FirstOrDefaultAsync();

            if (imageDoc != null && imageDoc.ImageBytes != null)
            {
                MarkerImageBase64 = $"data:{imageDoc.ContentType};base64,{Convert.ToBase64String(imageDoc.ImageBytes)}";
            }
            else
            {
                MarkerImageBase64 = null;
            }
        }

        private async Task PopulateAlignmentTypesSelectList(long? selectedId)
        {
            var alignmentTypes = await _context.MarkerAlignmentTypes
                .Where(a => !a.Inactive)
                .OrderBy(a => a.TypeDesc)
                .ToListAsync();

            AlignmentTypes = new SelectList(alignmentTypes, "Id", "TypeDesc", selectedId);
        }

        private async Task PopulateAdditionalTypesSelectList(long? selectedId)
        {
            var additionalTypes = await _context.MarkerAdditionalTypes
                .Where(a => !a.Inactive)
                .OrderBy(a => a.TypeDesc)
                .ToListAsync();

            AdditionalTypes = new SelectList(additionalTypes, "Id", "TypeDesc", selectedId);
        }

    }
}

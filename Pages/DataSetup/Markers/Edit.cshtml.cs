using BoardGameClubSoftware.Storage;
using Board_Game_Software.Models;
using Board_Game_Software.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Board_Game_Software.Pages.DataSetup.BoardGameMarkerTypes
{
    public class EditModel : PageModel
    {
        private readonly BoardGameDbContext _context;
        private readonly ICurrentClubService _currentClubService;
        private readonly IImageUploadValidator _imageUploadValidator;
        private readonly ImageService _imageService;

        public EditModel(BoardGameDbContext context, ICurrentClubService currentClubService, IImageUploadValidator imageUploadValidator, ImageService imageService)
        {
            _context = context;
            _currentClubService = currentClubService;
            _imageUploadValidator = imageUploadValidator;
            _imageService = imageService;
        }

        [BindProperty]
        public BoardGameMarkerType MarkerType { get; set; } = default!;

        [BindProperty]
        public IFormFile? Upload { get; set; }

        public SelectList? AlignmentTypes { get; set; }
        public SelectList? AdditionalTypes { get; set; }
        public string? MarkerImageUrl { get; set; }

        [BindProperty]
        public string? NewAdditionalTypeDesc { get; set; }

        public async Task<IActionResult> OnGetAsync(long id)
        {
            var club = await _currentClubService.GetCurrentClubAsync();
            var markerType = await _context.BoardGameMarkerTypes
                .Include(m => m.FkBgdMarkerAlignmentTypeNavigation)
                .Include(m => m.FkBgdMarkerAdditionalTypeNavigation)  // Add this!
                .FirstOrDefaultAsync(m => m.Id == id
                    && ((club.IsPlatformAdminMode && m.FkBgdClub == null)
                        || (!club.IsPlatformAdminMode && m.FkBgdClub == club.CurrentClubId)));


            if (markerType == null)
            {
                return NotFound();
            }

            MarkerType = markerType;
            await LoadMarkerImage(MarkerType.Gid);
            await PopulateAlignmentTypesSelectList(MarkerType.FkBgdMarkerAlignmentType);
            await PopulateAdditionalTypesSelectList(MarkerType.FkBgdMarkerAdditionalType);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ImageUploadValidationResult? uploadValidation = null;
            if (Upload != null && Upload.Length > 0)
            {
                uploadValidation = _imageUploadValidator.Validate(Upload);
                if (!uploadValidation.IsValid)
                {
                    ModelState.AddModelError(nameof(Upload), uploadValidation.ErrorMessage!);
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadMarkerImage(MarkerType.Gid);
                await PopulateAlignmentTypesSelectList(MarkerType.FkBgdMarkerAlignmentType);
                await PopulateAdditionalTypesSelectList(MarkerType.FkBgdMarkerAdditionalType);
                return Page();
            }

            var club = await _currentClubService.GetCurrentClubAsync();
            var markerInDb = await _context.BoardGameMarkerTypes
                .FirstOrDefaultAsync(m => m.Id == MarkerType.Id
                    && ((club.IsPlatformAdminMode && m.FkBgdClub == null)
                        || (!club.IsPlatformAdminMode && m.FkBgdClub == club.CurrentClubId)));

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
                    await _imageService.UploadMarkerTypeImageAsync(
                        checked((int)markerInDb.Id),
                        Upload,
                        User.Identity?.Name,
                        HttpContext.RequestAborted);
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
            MarkerImageUrl = await _context.StoredImages
                .AsNoTracking()
                .Where(image => image.OwnerType == ImageService.MarkerTypeImageOwnerType
                    && _context.BoardGameMarkerTypes.Any(markerType => markerType.Gid == gid && markerType.Id == image.OwnerId))
                .OrderByDescending(image => image.CreatedAtUtc)
                .Select(image => image.PublicUrl)
                .FirstOrDefaultAsync();
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

using BoardGameClubSoftware.Storage;
using Board_Game_Software.Data;
using Board_Game_Software.Models;
using Board_Game_Software.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Board_Game_Software.Pages.DataSetup.BoardGameMarkerTypes
{
    public class AddModel : PageModel
    {
        private readonly BoardGameDbContext _context;
        private readonly ICurrentClubService _currentClubService;
        private readonly IImageUploadValidator _imageUploadValidator;
        private readonly ImageService _imageService;

        [BindProperty]
        public BoardGameMarkerType MarkerType { get; set; } = new();

        // NEW: uploaded image for this marker type
        [BindProperty]
        public IFormFile? ImageUpload { get; set; }

        public SelectList AlignmentTypes { get; set; } = null!;

        public AddModel(
            BoardGameDbContext context,
            ICurrentClubService currentClubService,
            IImageUploadValidator imageUploadValidator,
            ImageService imageService)
        {
            _context = context;
            _currentClubService = currentClubService;
            _imageUploadValidator = imageUploadValidator;
            _imageService = imageService;
        }

        public async Task OnGetAsync()
        {
            await PopulateAlignmentTypesSelectList(null);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            MarkerType.TypeDesc = MarkerType.TypeDesc?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(MarkerType.TypeDesc))
            {
                ModelState.AddModelError("MarkerType.TypeDesc", "Marker name is required.");
            }

            ImageUploadValidationResult? imageValidation = null;
            if (ImageUpload != null && ImageUpload.Length > 0)
            {
                imageValidation = _imageUploadValidator.Validate(ImageUpload);
                if (!imageValidation.IsValid)
                {
                    ModelState.AddModelError(nameof(ImageUpload), imageValidation.ErrorMessage!);
                }
            }

            if (!ModelState.IsValid)
            {
                await PopulateAlignmentTypesSelectList(MarkerType.FkBgdMarkerAlignmentType);
                return Page();
            }

            var actor = User.Identity?.Name ?? "system";
            var now = DateTime.UtcNow;
            var clubId = await GetCurrentDataClubIdAsync();

            var existingMarkerType = await _context.BoardGameMarkerTypes
                .Where(t => t.TypeDesc == MarkerType.TypeDesc && (t.FkBgdClub == null || t.FkBgdClub == clubId))
                .OrderByDescending(t => t.FkBgdClub == clubId)
                .FirstOrDefaultAsync();

            if (existingMarkerType == null)
            {
                MarkerType.CreatedBy = actor;
                MarkerType.TimeCreated = now;
                MarkerType.ModifiedBy = actor;
                MarkerType.TimeModified = now;
                MarkerType.Gid = Guid.NewGuid();
                MarkerType.Inactive = false;
                MarkerType.FkBgdClub = clubId;

                _context.BoardGameMarkerTypes.Add(MarkerType);
                await _context.SaveChangesAsync(); // ensure MarkerType has its final Gid/Id
            }
            else
            {
                existingMarkerType.FkBgdMarkerAlignmentType = MarkerType.FkBgdMarkerAlignmentType;
                existingMarkerType.CustomSort = MarkerType.CustomSort;
                existingMarkerType.Inactive = false;
                existingMarkerType.ModifiedBy = actor;
                existingMarkerType.TimeModified = now;
                MarkerType = existingMarkerType;

                await _context.SaveChangesAsync();
            }

            // Save uploaded image to Azure Blob (if provided)
            if (ImageUpload != null && ImageUpload.Length > 0)
            {
                await _imageService.UploadMarkerTypeImageAsync(
                    checked((int)MarkerType.Id),
                    ImageUpload,
                    User.Identity?.Name,
                    HttpContext.RequestAborted);
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

        private async Task<long?> GetCurrentDataClubIdAsync()
        {
            var club = await _currentClubService.GetCurrentClubAsync();
            return club.HasClub && !club.IsPlatformAdminMode ? club.CurrentClubId : null;
        }
    }
}

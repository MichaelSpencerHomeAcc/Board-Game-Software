using BoardGameClubSoftware.Storage;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Board_Game_Software.Data;
using Board_Game_Software.Models;
using Board_Game_Software.Services;

namespace Board_Game_Software.Pages.DataSetup.Publishers
{
    public class EditModel : PageModel
    {
        private readonly BoardGameDbContext _context;
        private readonly ICurrentClubService _currentClubService;
        private readonly IImageUploadValidator _imageUploadValidator;
        private readonly ImageService _imageService;

        public EditModel(
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

        [BindProperty]
        public Publisher Publisher { get; set; } = default!;

        [BindProperty]
        public IFormFile? ImageUpload { get; set; }

        public string? ExistingImageUrl { get; set; }
        public string? ExistingImageContentType { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        public async Task<IActionResult> OnGetAsync(long id, string? returnUrl = null)
        {
            // Capture returnUrl from query string if present
            ReturnUrl = returnUrl;
            var club = await _currentClubService.GetCurrentClubAsync();

            var publisher = await _context.Publishers.FirstOrDefaultAsync(m => m.Id == id
                && ((club.IsPlatformAdminMode && m.FkBgdClub == null)
                    || (!club.IsPlatformAdminMode && m.FkBgdClub == club.CurrentClubId)));

            if (publisher == null)
            {
                return NotFound();
            }

            Publisher = publisher;

            ExistingImageUrl = await _context.StoredImages
                .AsNoTracking()
                .Where(image => image.OwnerType == ImageService.PublisherLogoOwnerType && image.OwnerId == checked((int)Publisher.Id))
                .OrderByDescending(image => image.CreatedAtUtc)
                .Select(image => image.PublicUrl)
                .FirstOrDefaultAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
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
                return Page();
            }

            var club = await _currentClubService.GetCurrentClubAsync();
            var publisherToUpdate = await _context.Publishers.FirstOrDefaultAsync(p => p.Id == Publisher.Id
                && ((club.IsPlatformAdminMode && p.FkBgdClub == null)
                    || (!club.IsPlatformAdminMode && p.FkBgdClub == club.CurrentClubId)));

            if (publisherToUpdate == null)
            {
                return NotFound();
            }

            publisherToUpdate.PublisherName = Publisher.PublisherName;
            publisherToUpdate.Description = Publisher.Description;
            publisherToUpdate.ModifiedBy = User.Identity?.Name ?? "system";
            publisherToUpdate.TimeModified = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();

                if (ImageUpload != null && ImageUpload.Length > 0)
                {
                    await _imageService.UploadPublisherLogoAsync(
                        checked((int)publisherToUpdate.Id),
                        ImageUpload,
                        User.Identity?.Name,
                        HttpContext.RequestAborted);
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Publishers.Any(e => e.Id == Publisher.Id)) return NotFound();
                else throw;
            }

            // FIX: More robust redirect logic
            if (!string.IsNullOrEmpty(ReturnUrl))
            {
                return Redirect(ReturnUrl);
            }

            return RedirectToPage("Index");
        }
    }
}

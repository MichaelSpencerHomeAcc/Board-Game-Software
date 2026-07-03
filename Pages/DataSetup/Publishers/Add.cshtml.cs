using BoardGameClubSoftware.Storage;
using Board_Game_Software.Data;
using Board_Game_Software.Models;
using Board_Game_Software.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Board_Game_Software.Pages.DataSetup.Publishers
{
    public class AddModel : PageModel
    {
        private readonly BoardGameDbContext _context;
        private readonly ICurrentClubService _currentClubService;
        private readonly IImageUploadValidator _imageUploadValidator;
        private readonly ImageService _imageService;

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

        [BindProperty]
        public Publisher Publisher { get; set; } = new();

        [BindProperty]
        public IFormFile? ImageUpload { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Set metadata following the MarkerType pattern
            Publisher.CreatedBy = User.Identity?.Name ?? "system";
            Publisher.TimeCreated = DateTime.UtcNow;
            Publisher.ModifiedBy = Publisher.CreatedBy;
            Publisher.TimeModified = Publisher.TimeCreated;
            Publisher.Gid = Guid.NewGuid();
            Publisher.Inactive = false;
            Publisher.FkBgdClub = await GetCurrentDataClubIdAsync();
            Publisher.PublisherName = Publisher.PublisherName?.Trim() ?? string.Empty;

            var existingPublisher = await _context.Publishers
                .Where(p => !p.Inactive
                    && p.PublisherName == Publisher.PublisherName
                    && (p.FkBgdClub == null || p.FkBgdClub == Publisher.FkBgdClub))
                .OrderByDescending(p => p.FkBgdClub == Publisher.FkBgdClub)
                .FirstOrDefaultAsync();

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

            if (existingPublisher != null)
            {
                return RedirectToPage("./Edit", new { id = existingPublisher.Id });
            }

            // 1. Save Publisher to SQL Server
            _context.Publishers.Add(Publisher);
            await _context.SaveChangesAsync();

            // 2. Handle Image Upload
            if (ImageUpload != null && ImageUpload.Length > 0)
            {
                await _imageService.UploadPublisherLogoAsync(
                    checked((int)Publisher.Id),
                    ImageUpload,
                    User.Identity?.Name,
                    HttpContext.RequestAborted);
            }

            return RedirectToPage("./Index");
        }

        private async Task<long?> GetCurrentDataClubIdAsync()
        {
            var club = await _currentClubService.GetCurrentClubAsync();
            return club.HasClub && !club.IsPlatformAdminMode ? club.CurrentClubId : null;
        }
    }
}

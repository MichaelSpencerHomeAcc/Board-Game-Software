using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Board_Game_Software.Data;
using Board_Game_Software.Models;
using Board_Game_Software.Services;

namespace Board_Game_Software.Pages.DataSetup.Publishers
{
    public class EditModel : PageModel
    {
        private readonly BoardGameDbContext _context;
        private readonly IMongoCollection<BoardGameImages> _boardGameImages;
        private readonly ICurrentClubService _currentClubService;

        public EditModel(BoardGameDbContext context, IMongoClient mongoClient, IConfiguration configuration, ICurrentClubService currentClubService)
        {
            _context = context;
            _currentClubService = currentClubService;
            var databaseName = configuration["MongoDbSettings:Database"];
            var database = mongoClient.GetDatabase(databaseName);
            _boardGameImages = database.GetCollection<BoardGameImages>("BoardGameImages");
        }

        [BindProperty]
        public Publisher Publisher { get; set; } = default!;

        [BindProperty]
        public IFormFile? ImageUpload { get; set; }

        public string? ExistingImageBase64 { get; set; }
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

            // Fetch current image from MongoDB
            var imageType = await _context.BoardGameImageTypes
                .FirstOrDefaultAsync(t => t.TypeDesc == "Image");

            if (imageType != null)
            {
                var imageDoc = await _boardGameImages
                    .Find(img => img.GID == Publisher.Gid && img.ImageTypeGID == imageType.Gid)
                    .FirstOrDefaultAsync();

                if (imageDoc?.ImageBytes != null)
                {
                    ExistingImageBase64 = Convert.ToBase64String(imageDoc.ImageBytes);
                    ExistingImageContentType = imageDoc.ContentType;
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
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
                    using var ms = new MemoryStream();
                    await ImageUpload.CopyToAsync(ms);
                    var imageBytes = ms.ToArray();

                    var imageType = await _context.BoardGameImageTypes
                        .FirstOrDefaultAsync(t => t.TypeDesc == "Image");

                    if (imageType != null)
                    {
                        await _boardGameImages.DeleteManyAsync(img =>
                            img.GID == publisherToUpdate.Gid && img.ImageTypeGID == imageType.Gid);

                        var newImage = new BoardGameImages
                        {
                            GID = publisherToUpdate.Gid,
                            SQLTable = "bgd.Publisher",
                            ImageTypeGID = imageType.Gid,
                            ImageBytes = imageBytes,
                            ContentType = ImageUpload.ContentType,
                            Description = $"{publisherToUpdate.PublisherName} Logo"
                        };

                        await _boardGameImages.InsertOneAsync(newImage);
                    }
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

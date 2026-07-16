using Board_Game_Software.Models;
using Board_Game_Software.Services;
using BoardGameClubSoftware.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Board_Game_Software.Controllers
{
    [Route("media")]
    public class MediaController : Controller
    {
        private readonly BoardGameDbContext _db;
        private readonly AzureBlobOptions _azureBlobOptions;

        public MediaController(BoardGameDbContext db, IOptions<AzureBlobOptions> azureBlobOptions)
        {
            _db = db;
            _azureBlobOptions = azureBlobOptions.Value;
        }

        [HttpGet("marker-type/{gid:guid}")]
        public async Task<IActionResult> MarkerType(Guid gid)
        {
            var markerTypeId = await _db.BoardGameMarkerTypes
                .AsNoTracking()
                .Where(markerType => markerType.Gid == gid)
                .Select(markerType => (int?)markerType.Id)
                .FirstOrDefaultAsync();

            return markerTypeId.HasValue
                ? await RedirectToStoredImageAsync(ImageService.MarkerTypeImageOwnerType, markerTypeId.Value)
                : NotFound();
        }

        [HttpGet("player/{gid:guid}")]
        public async Task<IActionResult> Player(Guid gid)
        {
            var playerId = await _db.Players
                .AsNoTracking()
                .Where(player => player.Gid == gid)
                .Select(player => (int?)player.Id)
                .FirstOrDefaultAsync();

            return playerId.HasValue
                ? await RedirectToStoredImageAsync(ImageService.UserAvatarOwnerType, playerId.Value)
                : NotFound();
        }

        [HttpGet("publisher/{gid:guid}")]
        public async Task<IActionResult> Publisher(Guid gid)
        {
            var publisherId = await _db.Publishers
                .AsNoTracking()
                .Where(publisher => publisher.Gid == gid)
                .Select(publisher => (int?)publisher.Id)
                .FirstOrDefaultAsync();

            return publisherId.HasValue
                ? await RedirectToStoredImageAsync(ImageService.PublisherLogoOwnerType, publisherId.Value)
                : NotFound();
        }

        [HttpGet("boardgame/front/{gid:guid}")]
        public async Task<IActionResult> BoardGameFront(Guid gid)
        {
            var game = await _db.BoardGames
                .AsNoTracking()
                .Where(boardGame => boardGame.Gid == gid)
                .Select(boardGame => new
                {
                    Id = (int)boardGame.Id,
                    boardGame.BoardGameName,
                    TemplateId = boardGame.FkBgdTemplateBoardGame.HasValue
                        ? (int?)boardGame.FkBgdTemplateBoardGame.Value
                        : null
                })
                .FirstOrDefaultAsync();

            if (game == null)
            {
                return NotFound();
            }

            var image = await FindStoredImageAsync(ImageService.GameCoverOwnerType, game.Id);
            if (image == null && game.TemplateId.HasValue)
            {
                image = await FindStoredImageAsync(ImageService.GameCoverOwnerType, game.TemplateId.Value);
            }

            image ??= await FindBoardGameFrontImageByLegacyBlobKeyAsync(gid);
            image ??= await FindBoardGameFrontImageByMatchingNameAsync(game.BoardGameName, game.Id);

            return image != null ? RedirectToStoredImage(image) : NotFound();
        }

        private async Task<IActionResult> RedirectToStoredImageAsync(string ownerType, int ownerId)
        {
            var image = await FindStoredImageAsync(ownerType, ownerId);
            return image != null ? RedirectToStoredImage(image) : NotFound();
        }

        private async Task<StoredImage?> FindStoredImageAsync(string ownerType, int ownerId)
        {
            return await _db.StoredImages
                .AsNoTracking()
                .Where(image => image.OwnerType == ownerType && image.OwnerId == ownerId)
                .OrderByDescending(image => image.CreatedAtUtc)
                .FirstOrDefaultAsync();
        }

        private async Task<StoredImage?> FindBoardGameFrontImageByLegacyBlobKeyAsync(Guid gid)
        {
            var legacyPrefix = $"boardgame/front/{gid:D}";

            return await _db.StoredImages
                .AsNoTracking()
                .Where(image => image.OwnerType == ImageService.GameCoverOwnerType
                    && image.BlobKey.StartsWith(legacyPrefix))
                .OrderByDescending(image => image.CreatedAtUtc)
                .FirstOrDefaultAsync();
        }

        private async Task<StoredImage?> FindBoardGameFrontImageByMatchingNameAsync(string boardGameName, int currentGameId)
        {
            var matchingGameIds = await _db.BoardGames
                .AsNoTracking()
                .Where(boardGame => !boardGame.Inactive
                    && boardGame.Id != currentGameId
                    && boardGame.Id <= int.MaxValue
                    && boardGame.BoardGameName == boardGameName)
                .Select(boardGame => (int)boardGame.Id)
                .ToListAsync();

            if (matchingGameIds.Count == 0)
            {
                return null;
            }

            return await _db.StoredImages
                .AsNoTracking()
                .Where(image => image.OwnerType == ImageService.GameCoverOwnerType
                    && matchingGameIds.Contains(image.OwnerId))
                .OrderByDescending(image => image.CreatedAtUtc)
                .FirstOrDefaultAsync();
        }

        private IActionResult RedirectToStoredImage(StoredImage image)
        {
            var publicUrl = ResolvePublicUrl(image);
            if (string.IsNullOrWhiteSpace(publicUrl))
            {
                return NotFound();
            }

            Response.Headers["Cache-Control"] = "public,max-age=604800";
            Response.Headers["X-Media-Source"] = "azure-blob";
            return Redirect(publicUrl);
        }

        private string? ResolvePublicUrl(StoredImage image)
        {
            if (Uri.TryCreate(image.PublicUrl, UriKind.Absolute, out _))
            {
                return image.PublicUrl;
            }

            if (string.IsNullOrWhiteSpace(image.BlobKey) ||
                string.IsNullOrWhiteSpace(_azureBlobOptions.ConnectionString) ||
                string.IsNullOrWhiteSpace(_azureBlobOptions.ContainerName))
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(_azureBlobOptions.PublicBaseUrl))
            {
                return $"{_azureBlobOptions.PublicBaseUrl.TrimEnd('/')}/{image.BlobKey.TrimStart('/')}";
            }

            try
            {
                var container = new Azure.Storage.Blobs.BlobContainerClient(
                    _azureBlobOptions.ConnectionString,
                    _azureBlobOptions.ContainerName);

                return container.GetBlobClient(image.BlobKey).Uri.ToString();
            }
            catch
            {
                return null;
            }
        }
    }
}

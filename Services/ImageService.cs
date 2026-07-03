using BoardGameClubSoftware.Storage;
using Board_Game_Software.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Board_Game_Software.Services;

public sealed class ImageService
{
    private const string BlobProvider = "AzureBlob";
    public const string ClubLogoOwnerType = "ClubLogo";
    public const string GameCoverOwnerType = "GameCover";
    public const string UserAvatarOwnerType = "UserAvatar";
    public const string GameNightPhotoOwnerType = "GameNightPhoto";
    public const string MarkerTypeImageOwnerType = "MarkerTypeImage";
    public const string PublisherLogoOwnerType = "PublisherLogo";

    private readonly BoardGameDbContext _db;
    private readonly IBlobStorageService _blobStorage;
    private readonly IImageUploadValidator _imageUploadValidator;

    public ImageService(
        BoardGameDbContext db,
        IBlobStorageService blobStorage,
        IImageUploadValidator imageUploadValidator)
    {
        _db = db;
        _blobStorage = blobStorage;
        _imageUploadValidator = imageUploadValidator;
    }

    public async Task<StoredImage> UploadClubLogoAsync(
        int clubId,
        IFormFile file,
        string? uploadedByUserId,
        CancellationToken cancellationToken = default)
    {
        var validation = Validate(file);
        var blobKey = ImageBlobKeyBuilder.ClubLogo(clubId, Guid.NewGuid(), validation.Extension);

        return await UploadReplacingSingleImageAsync(
            ClubLogoOwnerType,
            clubId,
            blobKey,
            file,
            validation,
            uploadedByUserId,
            cancellationToken);
    }

    public async Task<StoredImage> UploadGameCoverAsync(
        int gameId,
        IFormFile file,
        string? uploadedByUserId,
        CancellationToken cancellationToken = default)
    {
        var validation = Validate(file);
        var blobKey = ImageBlobKeyBuilder.GameCover(gameId, Guid.NewGuid(), validation.Extension);

        return await UploadReplacingSingleImageAsync(
            GameCoverOwnerType,
            gameId,
            blobKey,
            file,
            validation,
            uploadedByUserId,
            cancellationToken);
    }

    public async Task<StoredImage> UploadUserAvatarAsync(
        int userId,
        IFormFile file,
        string? uploadedByUserId,
        CancellationToken cancellationToken = default)
    {
        var validation = Validate(file);
        var blobKey = ImageBlobKeyBuilder.UserAvatar(userId.ToString(), Guid.NewGuid(), validation.Extension);

        return await UploadReplacingSingleImageAsync(
            UserAvatarOwnerType,
            userId,
            blobKey,
            file,
            validation,
            uploadedByUserId,
            cancellationToken);
    }

    public async Task<StoredImage> UploadGameNightPhotoAsync(
        int gameNightId,
        IFormFile file,
        string? uploadedByUserId,
        CancellationToken cancellationToken = default)
    {
        var validation = Validate(file);
        var blobKey = ImageBlobKeyBuilder.GameNightPhoto(gameNightId, Guid.NewGuid(), validation.Extension);

        return await UploadNewImageAsync(
            GameNightPhotoOwnerType,
            gameNightId,
            blobKey,
            file,
            validation,
            uploadedByUserId,
            cancellationToken);
    }

    public async Task<StoredImage> UploadMarkerTypeImageAsync(
        int markerTypeId,
        IFormFile file,
        string? uploadedByUserId,
        CancellationToken cancellationToken = default)
    {
        var validation = Validate(file);
        var blobKey = $"marker-types/{markerTypeId}/image/{Guid.NewGuid():D}{validation.Extension}";

        return await UploadReplacingSingleImageAsync(
            MarkerTypeImageOwnerType,
            markerTypeId,
            blobKey,
            file,
            validation,
            uploadedByUserId,
            cancellationToken);
    }

    public async Task<StoredImage> UploadPublisherLogoAsync(
        int publisherId,
        IFormFile file,
        string? uploadedByUserId,
        CancellationToken cancellationToken = default)
    {
        var validation = Validate(file);
        var blobKey = $"publishers/{publisherId}/logo/{Guid.NewGuid():D}{validation.Extension}";

        return await UploadReplacingSingleImageAsync(
            PublisherLogoOwnerType,
            publisherId,
            blobKey,
            file,
            validation,
            uploadedByUserId,
            cancellationToken);
    }

    private ImageUploadValidationResult Validate(IFormFile file)
    {
        var validation = _imageUploadValidator.Validate(file);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(validation.ErrorMessage ?? "The image upload is invalid.");
        }

        return validation;
    }

    private async Task<StoredImage> UploadReplacingSingleImageAsync(
        string ownerType,
        int ownerId,
        string blobKey,
        IFormFile file,
        ImageUploadValidationResult validation,
        string? uploadedByUserId,
        CancellationToken cancellationToken)
    {
        var existingImages = await _db.StoredImages
            .Where(image => image.OwnerType == ownerType && image.OwnerId == ownerId)
            .ToListAsync(cancellationToken);

        var storedImage = await UploadNewImageAsync(
            ownerType,
            ownerId,
            blobKey,
            file,
            validation,
            uploadedByUserId,
            cancellationToken);

        foreach (var existingImage in existingImages)
        {
            if (!string.IsNullOrWhiteSpace(existingImage.BlobKey))
            {
                await _blobStorage.DeleteAsync(existingImage.BlobKey, cancellationToken);
            }

            _db.StoredImages.Remove(existingImage);
        }

        if (existingImages.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        return storedImage;
    }

    private async Task<StoredImage> UploadNewImageAsync(
        string ownerType,
        int ownerId,
        string blobKey,
        IFormFile file,
        ImageUploadValidationResult validation,
        string? uploadedByUserId,
        CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var upload = await _blobStorage.UploadAsync(
            stream,
            blobKey,
            validation.ContentType,
            cancellationToken);

        var storedImage = new StoredImage
        {
            OwnerType = ownerType,
            OwnerId = ownerId,
            BlobProvider = BlobProvider,
            BlobKey = upload.BlobKey,
            PublicUrl = upload.PublicUrl,
            OriginalFileName = Path.GetFileName(file.FileName),
            ContentType = upload.ContentType,
            SizeBytes = upload.SizeBytes ?? validation.SizeBytes,
            UploadedByUserId = uploadedByUserId,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.StoredImages.Add(storedImage);
        await _db.SaveChangesAsync(cancellationToken);

        return storedImage;
    }
}

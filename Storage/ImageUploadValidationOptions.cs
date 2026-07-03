namespace BoardGameClubSoftware.Storage;

public sealed class ImageUploadValidationOptions
{
    public long MaxSizeBytes { get; set; } = 5 * 1024 * 1024;
}

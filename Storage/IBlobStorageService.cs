namespace BoardGameClubSoftware.Storage;

public interface IBlobStorageService
{
    Task<BlobUploadResult> UploadAsync(
        Stream stream,
        string blobKey,
        string contentType,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string blobKey,
        CancellationToken cancellationToken = default);

    string GetPublicUrl(string blobKey);
}

public sealed record BlobUploadResult(
    string BlobKey,
    string PublicUrl,
    string ContentType,
    long? SizeBytes);

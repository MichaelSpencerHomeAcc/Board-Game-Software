using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace BoardGameClubSoftware.Storage;

public sealed class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _container;
    private readonly AzureBlobOptions _options;

    public AzureBlobStorageService(IOptions<AzureBlobOptions> options)
    {
        _options = options.Value;

        _container = new BlobContainerClient(
            _options.ConnectionString,
            _options.ContainerName);
    }

    public async Task<BlobUploadResult> UploadAsync(
        Stream stream,
        string blobKey,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        await _container.CreateIfNotExistsAsync(
            PublicAccessType.Blob,
            cancellationToken: cancellationToken);

        var blob = _container.GetBlobClient(blobKey);

        var headers = new BlobHttpHeaders
        {
            ContentType = contentType
        };

        await blob.UploadAsync(
            stream,
            new BlobUploadOptions
            {
                HttpHeaders = headers
            },
            cancellationToken);

        return new BlobUploadResult(
            blobKey,
            GetPublicUrl(blobKey),
            contentType,
            stream.CanSeek ? stream.Length : null);
    }

    public async Task DeleteAsync(
        string blobKey,
        CancellationToken cancellationToken = default)
    {
        var blob = _container.GetBlobClient(blobKey);

        await blob.DeleteIfExistsAsync(
            cancellationToken: cancellationToken);
    }

    public string GetPublicUrl(string blobKey)
    {
        var baseUrl = _options.PublicBaseUrl.TrimEnd('/');
        return $"{baseUrl}/{blobKey.TrimStart('/')}";
    }
}

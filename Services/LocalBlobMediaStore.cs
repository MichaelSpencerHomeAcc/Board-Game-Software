namespace Board_Game_Software.Services;

public interface IBlobMediaStore
{
    Task<BlobMediaFile?> FindAsync(params string[] blobKeys);
}

public sealed class LocalBlobMediaStore : IBlobMediaStore
{
    private static readonly Dictionary<string, string> ContentTypesByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp",
        [".gif"] = "image/gif",
        [".svg"] = "image/svg+xml"
    };

    private readonly string? _rootPath;

    public LocalBlobMediaStore(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var configuredRoot = configuration["Media:BlobRoot"];
        if (string.IsNullOrWhiteSpace(configuredRoot))
        {
            return;
        }

        _rootPath = Path.IsPathRooted(configuredRoot)
            ? configuredRoot
            : Path.GetFullPath(Path.Combine(environment.ContentRootPath, configuredRoot));
    }

    public async Task<BlobMediaFile?> FindAsync(params string[] blobKeys)
    {
        if (string.IsNullOrWhiteSpace(_rootPath) || !Directory.Exists(_rootPath))
        {
            return null;
        }

        foreach (var blobKey in blobKeys.Where(k => !string.IsNullOrWhiteSpace(k)))
        {
            var path = ResolveBlobPath(blobKey);
            if (path == null || !File.Exists(path))
            {
                continue;
            }

            var extension = Path.GetExtension(path);
            var contentType = ContentTypesByExtension.TryGetValue(extension, out var knownContentType)
                ? knownContentType
                : "application/octet-stream";

            return new BlobMediaFile(await File.ReadAllBytesAsync(path), contentType);
        }

        return null;
    }

    private string? ResolveBlobPath(string blobKey)
    {
        if (_rootPath == null)
        {
            return null;
        }

        var normalisedKey = blobKey.Replace('/', Path.DirectorySeparatorChar);
        var candidate = Path.GetFullPath(Path.Combine(_rootPath, normalisedKey));
        var root = Path.GetFullPath(_rootPath);

        return candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase)
            ? candidate
            : null;
    }
}

public sealed record BlobMediaFile(byte[] Bytes, string ContentType);

using System.Security.Cryptography;
using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Data.SqlClient;

var options = ImportOptions.Parse(args);
if (options.ShowHelp)
{
    ImportOptions.PrintHelp();
    return 0;
}

if (string.IsNullOrWhiteSpace(options.SqlConnectionString))
{
    Console.Error.WriteLine("Missing --sql-connection-string or AZURE_SQL_CONNECTION_STRING.");
    ImportOptions.PrintHelp();
    return 2;
}

if (string.IsNullOrWhiteSpace(options.AzureBlobConnectionString))
{
    Console.Error.WriteLine("Missing --azure-blob-connection-string or AZURE_BLOB_CONNECTION_STRING.");
    ImportOptions.PrintHelp();
    return 2;
}

if (string.IsNullOrWhiteSpace(options.PublicBaseUrl))
{
    Console.Error.WriteLine("Missing --public-base-url or AZURE_BLOB_PUBLIC_BASE_URL.");
    ImportOptions.PrintHelp();
    return 2;
}

if (!File.Exists(options.ManifestPath))
{
    Console.Error.WriteLine($"Manifest not found: {options.ManifestPath}");
    return 2;
}

var manifestRoot = Path.GetDirectoryName(Path.GetFullPath(options.ManifestPath)) ?? Directory.GetCurrentDirectory();
var manifestJson = await File.ReadAllTextAsync(options.ManifestPath);
var manifest = JsonSerializer.Deserialize<List<BlobManifestEntry>>(manifestJson, new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
}) ?? [];

var container = new BlobContainerClient(options.AzureBlobConnectionString, options.ContainerName);
if (!options.DryRun)
{
    await container.CreateIfNotExistsAsync(PublicAccessType.Blob);
}

await using var sqlConnection = new SqlConnection(options.SqlConnectionString);
await sqlConnection.OpenAsync();

var imported = 0;
var skipped = 0;
var missingOwner = 0;
var missingFile = 0;
var unsupported = 0;

foreach (var entry in manifest.OrderBy(x => x.BlobKey, StringComparer.OrdinalIgnoreCase))
{
    var owner = ResolveOwner(entry);
    if (owner is null)
    {
        unsupported++;
        Console.WriteLine($"SKIP unsupported source {entry.SqlTable ?? "(null)"} {entry.BlobKey}");
        continue;
    }

    var ownerId = await FindOwnerIdAsync(sqlConnection, owner, entry.Gid);
    if (ownerId is null)
    {
        missingOwner++;
        Console.WriteLine($"SKIP missing owner {owner.OwnerType} gid={entry.Gid ?? "(null)"} blob={entry.BlobKey}");
        continue;
    }

    var localPath = ResolveLocalPath(entry.LocalPath, manifestRoot);
    if (!File.Exists(localPath))
    {
        missingFile++;
        Console.WriteLine($"SKIP missing file {localPath}");
        continue;
    }

    if (await StoredImageExistsAsync(sqlConnection, entry.BlobKey) && !options.ReplaceMetadata)
    {
        skipped++;
        Console.WriteLine($"SKIP existing metadata {entry.BlobKey}");
        continue;
    }

    var sizeBytes = new FileInfo(localPath).Length;
    var contentType = string.IsNullOrWhiteSpace(entry.ContentType)
        ? "application/octet-stream"
        : entry.ContentType;

    if (!options.DryRun)
    {
        if (options.ReplaceMetadata)
        {
            await DeleteStoredImageAsync(sqlConnection, entry.BlobKey);
        }

        await using (var stream = File.OpenRead(localPath))
        {
            var blob = container.GetBlobClient(entry.BlobKey);
            await blob.UploadAsync(
                stream,
                new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
                },
                cancellationToken: default);
        }

        await InsertStoredImageAsync(
            sqlConnection,
            owner.OwnerType,
            ownerId.Value,
            entry.BlobKey,
            BuildPublicUrl(options.PublicBaseUrl, entry.BlobKey),
            Path.GetFileName(localPath),
            contentType,
            sizeBytes,
            entry.Description);
    }

    imported++;
    Console.WriteLine($"{(options.DryRun ? "DRYRUN" : "IMPORT")} {owner.OwnerType} owner={ownerId.Value} blob={entry.BlobKey}");
}

Console.WriteLine();
Console.WriteLine($"Imported: {imported}");
Console.WriteLine($"Skipped existing metadata: {skipped}");
Console.WriteLine($"Missing owners: {missingOwner}");
Console.WriteLine($"Missing files: {missingFile}");
Console.WriteLine($"Unsupported manifest entries: {unsupported}");
Console.WriteLine(options.DryRun ? "Dry run only. Re-run without --dry-run to upload blobs and insert metadata." : "Import completed.");

return missingOwner > 0 || missingFile > 0 ? 1 : 0;

static OwnerMapping? ResolveOwner(BlobManifestEntry entry)
{
    var table = entry.SqlTable?.Trim();
    if (string.IsNullOrWhiteSpace(table))
    {
        return null;
    }

    if (table.Equals("bgd.Player", StringComparison.OrdinalIgnoreCase))
    {
        return new OwnerMapping("UserAvatar", "bgd", "Player");
    }

    if (table.Equals("bgd.BoardGame", StringComparison.OrdinalIgnoreCase) ||
        table.Equals("BoardGames", StringComparison.OrdinalIgnoreCase))
    {
        return new OwnerMapping("GameCover", "bgd", "BoardGame");
    }

    if (table.Equals("bgd.BoardGameMarkerType", StringComparison.OrdinalIgnoreCase))
    {
        return new OwnerMapping("MarkerTypeImage", "bgd", "BoardGameMarkerType");
    }

    if (table.Equals("bgd.Publisher", StringComparison.OrdinalIgnoreCase) ||
        table.Equals("Publishers", StringComparison.OrdinalIgnoreCase))
    {
        return new OwnerMapping("PublisherLogo", "bgd", "Publisher");
    }

    return null;
}

static async Task<int?> FindOwnerIdAsync(SqlConnection connection, OwnerMapping owner, string? gid)
{
    if (!Guid.TryParse(gid, out var parsedGid))
    {
        return null;
    }

    await using var command = connection.CreateCommand();
    command.CommandText = $"SELECT [ID] FROM [{owner.Schema}].[{owner.Table}] WHERE [GID] = @Gid;";
    command.Parameters.AddWithValue("@Gid", parsedGid);

    var result = await command.ExecuteScalarAsync();
    if (result is null || result == DBNull.Value)
    {
        return null;
    }

    var id = Convert.ToInt64(result);
    if (id > int.MaxValue)
    {
        throw new InvalidOperationException($"{owner.Schema}.{owner.Table} ID {id} is too large for StoredImage.OwnerId.");
    }

    return (int)id;
}

static async Task<bool> StoredImageExistsAsync(SqlConnection connection, string blobKey)
{
    await using var command = connection.CreateCommand();
    command.CommandText = "SELECT COUNT_BIG(*) FROM [bgd].[StoredImage] WHERE [BlobKey] = @BlobKey;";
    command.Parameters.AddWithValue("@BlobKey", blobKey);

    return Convert.ToInt64(await command.ExecuteScalarAsync()) > 0;
}

static async Task DeleteStoredImageAsync(SqlConnection connection, string blobKey)
{
    await using var command = connection.CreateCommand();
    command.CommandText = "DELETE FROM [bgd].[StoredImage] WHERE [BlobKey] = @BlobKey;";
    command.Parameters.AddWithValue("@BlobKey", blobKey);
    await command.ExecuteNonQueryAsync();
}

static async Task InsertStoredImageAsync(
    SqlConnection connection,
    string ownerType,
    int ownerId,
    string blobKey,
    string publicUrl,
    string originalFileName,
    string contentType,
    long sizeBytes,
    string? caption)
{
    await using var command = connection.CreateCommand();
    command.CommandText = """
        INSERT INTO [bgd].[StoredImage]
            ([OwnerType], [OwnerId], [BlobProvider], [BlobKey], [PublicUrl],
             [OriginalFileName], [ContentType], [SizeBytes], [AltText], [Caption],
             [UploadedByUserId], [CreatedAtUtc])
        VALUES
            (@OwnerType, @OwnerId, N'AzureBlob', @BlobKey, @PublicUrl,
             @OriginalFileName, @ContentType, @SizeBytes, NULL, @Caption,
             NULL, SYSUTCDATETIME());
        """;

    command.Parameters.AddWithValue("@OwnerType", ownerType);
    command.Parameters.AddWithValue("@OwnerId", ownerId);
    command.Parameters.AddWithValue("@BlobKey", blobKey);
    command.Parameters.AddWithValue("@PublicUrl", publicUrl);
    command.Parameters.AddWithValue("@OriginalFileName", originalFileName);
    command.Parameters.AddWithValue("@ContentType", contentType);
    command.Parameters.AddWithValue("@SizeBytes", sizeBytes);
    command.Parameters.AddWithValue("@Caption", string.IsNullOrWhiteSpace(caption) ? DBNull.Value : caption);

    await command.ExecuteNonQueryAsync();
}

static string ResolveLocalPath(string localPath, string manifestRoot)
{
    if (Path.IsPathRooted(localPath))
    {
        return localPath;
    }

    return Path.GetFullPath(Path.Combine(manifestRoot, localPath));
}

static string BuildPublicUrl(string publicBaseUrl, string blobKey)
{
    return $"{publicBaseUrl.TrimEnd('/')}/{blobKey.TrimStart('/')}";
}

public sealed record OwnerMapping(string OwnerType, string Schema, string Table);

public sealed class BlobManifestEntry
{
    public string SourceMongoId { get; init; } = string.Empty;
    public string? SqlTable { get; init; }
    public string? Gid { get; init; }
    public string? ImageTypeGid { get; init; }
    public string? Description { get; init; }
    public string BlobKey { get; init; } = string.Empty;
    public string LocalPath { get; init; } = string.Empty;
    public string ContentType { get; init; } = "application/octet-stream";
    public long SizeBytes { get; init; }
    public string Sha256 { get; init; } = string.Empty;
}

public sealed class ImportOptions
{
    public string ManifestPath { get; private init; } = Path.Combine("artifacts", "image-blobs", "manifest.json");
    public string? SqlConnectionString { get; private init; }
    public string? AzureBlobConnectionString { get; private init; }
    public string ContainerName { get; private init; } = "images";
    public string? PublicBaseUrl { get; private init; }
    public bool DryRun { get; private init; }
    public bool ReplaceMetadata { get; private init; }
    public bool ShowHelp { get; private init; }

    public static ImportOptions Parse(string[] args)
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            var key = arg[2..];
            if (key is "dry-run" or "replace-metadata" or "help")
            {
                flags.Add(key);
                continue;
            }

            if (i + 1 >= args.Length)
            {
                throw new ArgumentException($"Missing value for --{key}.");
            }

            values[key] = args[++i];
        }

        return new ImportOptions
        {
            ManifestPath = Get(values, "manifest") ?? Path.Combine("artifacts", "image-blobs", "manifest.json"),
            SqlConnectionString = Get(values, "sql-connection-string") ?? Environment.GetEnvironmentVariable("AZURE_SQL_CONNECTION_STRING"),
            AzureBlobConnectionString = Get(values, "azure-blob-connection-string") ?? Environment.GetEnvironmentVariable("AZURE_BLOB_CONNECTION_STRING"),
            ContainerName = Get(values, "container") ?? "images",
            PublicBaseUrl = Get(values, "public-base-url") ?? Environment.GetEnvironmentVariable("AZURE_BLOB_PUBLIC_BASE_URL"),
            DryRun = flags.Contains("dry-run"),
            ReplaceMetadata = flags.Contains("replace-metadata"),
            ShowHelp = flags.Contains("help")
        };
    }

    public static void PrintHelp()
    {
        Console.WriteLine("""
        MongoImageBlobImporter

        Uploads files from MongoImageBlobExporter manifest.json to Azure Blob Storage
        and inserts bgd.StoredImage metadata rows into Azure SQL.

        Usage:
          dotnet run --project Tools/MongoImageBlobImporter -- --manifest ".\artifacts\image-blobs\manifest.json" --sql-connection-string "<azure-sql>" --azure-blob-connection-string "<storage>" --public-base-url "https://account.blob.core.windows.net/images"

        Options:
          --manifest                      Exporter manifest path. Default: .\artifacts\image-blobs\manifest.json.
          --sql-connection-string          Azure SQL connection string. Or set AZURE_SQL_CONNECTION_STRING.
          --azure-blob-connection-string   Azure Storage connection string. Or set AZURE_BLOB_CONNECTION_STRING.
          --container                      Blob container name. Default: images.
          --public-base-url                Public base URL for the container. Or set AZURE_BLOB_PUBLIC_BASE_URL.
          --dry-run                        Resolve owners and files without uploading or inserting rows.
          --replace-metadata               Delete existing StoredImage rows for the same BlobKey before inserting.
          --help                           Show help.
        """);
    }

    private static string? Get(Dictionary<string, string?> values, string key)
    {
        return values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;
    }
}

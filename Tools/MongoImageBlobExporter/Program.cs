using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;

var options = ExportOptions.Parse(args);
if (options.ShowHelp)
{
    ExportOptions.PrintHelp();
    return 0;
}

if (string.IsNullOrWhiteSpace(options.ConnectionString))
{
    Console.Error.WriteLine("Missing --connection-string or MONGODB_CONNECTION_STRING.");
    ExportOptions.PrintHelp();
    return 2;
}

if (string.IsNullOrWhiteSpace(options.DatabaseName))
{
    Console.Error.WriteLine("Missing --database or MONGODB_DATABASE.");
    ExportOptions.PrintHelp();
    return 2;
}

Directory.CreateDirectory(options.OutputRoot);

var client = new MongoClient(options.ConnectionString);
var database = client.GetDatabase(options.DatabaseName);
var collection = database.GetCollection<BsonDocument>(options.CollectionName);

var filter = Builders<BsonDocument>.Filter.Exists("ImageBytes", true)
    & Builders<BsonDocument>.Filter.Ne("ImageBytes", BsonNull.Value);

var findOptions = new FindOptions<BsonDocument>
{
    BatchSize = options.BatchSize
};

var manifest = new List<BlobManifestEntry>();
var exported = 0;
var skipped = 0;
var overwritten = 0;
var empty = 0;

using var cursor = await collection.FindAsync(filter, findOptions);
while (await cursor.MoveNextAsync())
{
    foreach (var doc in cursor.Current)
    {
        if (options.Limit.HasValue && exported >= options.Limit.Value)
        {
            break;
        }

        var bytes = GetImageBytes(doc);
        if (bytes is null || bytes.Length == 0)
        {
            empty++;
            continue;
        }

        var sourceId = doc.GetValue("_id", BsonNull.Value).ToString() ?? "unknown-source-id";
        var sqlTable = GetString(doc, "SQLTable") ?? "unknown";
        var gid = GetGuidString(doc, "GID") ?? "no-gid";
        var imageTypeGid = GetGuidString(doc, "ImageTypeGID");
        var contentType = DetectContentType(bytes, GetString(doc, "ContentType"));
        var extension = ExtensionFromContentType(contentType);
        var blobKey = BuildBlobKey(sqlTable, gid, imageTypeGid, extension, sourceId);
        var outputPath = Path.Combine(options.OutputRoot, blobKey.Replace('/', Path.DirectorySeparatorChar));

        if (File.Exists(outputPath) && !options.Overwrite)
        {
            skipped++;
            manifest.Add(BuildManifestEntry(doc, sourceId, blobKey, outputPath, bytes, contentType, skipped: true));
            continue;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        await File.WriteAllBytesAsync(outputPath, bytes);

        if (File.Exists(outputPath))
        {
            overwritten++;
        }

        exported++;
        manifest.Add(BuildManifestEntry(doc, sourceId, blobKey, outputPath, bytes, contentType, skipped: false));
    }

    if (options.Limit.HasValue && exported >= options.Limit.Value)
    {
        break;
    }
}

var manifestPath = Path.Combine(options.OutputRoot, "manifest.json");
var summaryPath = Path.Combine(options.OutputRoot, "summary.json");

var jsonOptions = new JsonSerializerOptions
{
    WriteIndented = true
};

await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest.OrderBy(x => x.BlobKey), jsonOptions));

var summary = new
{
    ExportedAtUtc = DateTimeOffset.UtcNow,
    options.DatabaseName,
    options.CollectionName,
    OutputRoot = Path.GetFullPath(options.OutputRoot),
    ExportedFiles = exported,
    SkippedExistingFiles = skipped,
    EmptyImageDocuments = empty,
    OverwriteEnabled = options.Overwrite,
    ManifestPath = Path.GetFullPath(manifestPath)
};

await File.WriteAllTextAsync(summaryPath, JsonSerializer.Serialize(summary, jsonOptions));

Console.WriteLine($"Exported files: {exported}");
Console.WriteLine($"Skipped existing files: {skipped}");
Console.WriteLine($"Empty image documents: {empty}");
Console.WriteLine($"Manifest: {manifestPath}");
Console.WriteLine($"Summary: {summaryPath}");

return 0;

static byte[]? GetImageBytes(BsonDocument doc)
{
    if (!doc.TryGetValue("ImageBytes", out var value) || value.IsBsonNull)
    {
        return null;
    }

    if (value.IsBsonBinaryData)
    {
        return value.AsBsonBinaryData.Bytes;
    }

    if (value.IsString)
    {
        try
        {
            return Convert.FromBase64String(value.AsString);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    return null;
}

static string? GetString(BsonDocument doc, string name)
{
    if (!doc.TryGetValue(name, out var value) || value.IsBsonNull)
    {
        return null;
    }

    return value switch
    {
        { IsString: true } => value.AsString,
        { IsGuid: true } => value.AsGuid.ToString("D"),
        _ => value.ToString()
    };
}

static string? GetGuidString(BsonDocument doc, string name)
{
    var raw = GetString(doc, name);
    if (string.IsNullOrWhiteSpace(raw))
    {
        return null;
    }

    return Guid.TryParse(raw, out var guid)
        ? guid.ToString("D")
        : SanitiseKeySegment(raw);
}

static string BuildBlobKey(string sqlTable, string gid, string? imageTypeGid, string extension, string sourceId)
{
    var table = sqlTable.Trim();

    if (table.Equals("bgd.Player", StringComparison.OrdinalIgnoreCase))
    {
        return $"player/{gid}{extension}";
    }

    if (table.Equals("bgd.BoardGameMarkerType", StringComparison.OrdinalIgnoreCase))
    {
        return $"marker-type/{gid}{extension}";
    }

    if (table.Equals("bgd.Publisher", StringComparison.OrdinalIgnoreCase) ||
        table.Equals("Publishers", StringComparison.OrdinalIgnoreCase))
    {
        return $"publisher/{gid}{extension}";
    }

    if (table.Equals("bgd.BoardGame", StringComparison.OrdinalIgnoreCase) ||
        table.Equals("BoardGames", StringComparison.OrdinalIgnoreCase))
    {
        return imageTypeGid is null
            ? $"boardgame/{gid}/{SanitiseKeySegment(sourceId)}{extension}"
            : $"boardgame/front/{gid}{extension}";
    }

    var safeTable = SanitiseKeySegment(table.Replace("bgd.", string.Empty, StringComparison.OrdinalIgnoreCase));
    var safeType = imageTypeGid ?? SanitiseKeySegment(sourceId);
    return $"misc/{safeTable}/{gid}/{safeType}{extension}";
}

static BlobManifestEntry BuildManifestEntry(
    BsonDocument doc,
    string sourceId,
    string blobKey,
    string outputPath,
    byte[] bytes,
    string contentType,
    bool skipped)
{
    return new BlobManifestEntry
    {
        SourceMongoId = sourceId,
        SqlTable = GetString(doc, "SQLTable"),
        Gid = GetGuidString(doc, "GID"),
        ImageTypeGid = GetGuidString(doc, "ImageTypeGID"),
        Description = GetString(doc, "Description"),
        BlobKey = blobKey,
        LocalPath = Path.GetFullPath(outputPath),
        ContentType = contentType,
        SizeBytes = bytes.LongLength,
        Sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(),
        SkippedBecauseFileExists = skipped,
        AvatarFocusX = GetInt(doc, "AvatarFocusX"),
        AvatarFocusY = GetInt(doc, "AvatarFocusY"),
        AvatarZoom = GetInt(doc, "AvatarZoom"),
        PodiumFocusX = GetInt(doc, "PodiumFocusX"),
        PodiumFocusY = GetInt(doc, "PodiumFocusY"),
        PodiumZoom = GetInt(doc, "PodiumZoom"),
        CardFocusX = GetInt(doc, "CardFocusX"),
        CardFocusY = GetInt(doc, "CardFocusY")
    };
}

static int? GetInt(BsonDocument doc, string name)
{
    if (!doc.TryGetValue(name, out var value) || value.IsBsonNull)
    {
        return null;
    }

    if (value.IsInt32) return value.AsInt32;
    if (value.IsInt64) return checked((int)value.AsInt64);
    if (value.IsDouble) return (int)value.AsDouble;
    return int.TryParse(value.ToString(), out var parsed) ? parsed : null;
}

static string DetectContentType(byte[] bytes, string? fallback)
{
    if (bytes.Length >= 12 &&
        bytes[0] == (byte)'R' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F' && bytes[3] == (byte)'F' &&
        bytes[8] == (byte)'W' && bytes[9] == (byte)'E' && bytes[10] == (byte)'B' && bytes[11] == (byte)'P')
    {
        return "image/webp";
    }

    if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
    {
        return "image/jpeg";
    }

    if (bytes.Length >= 8 &&
        bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 &&
        bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A)
    {
        return "image/png";
    }

    if (bytes.Length >= 6 &&
        bytes[0] == (byte)'G' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F')
    {
        return "image/gif";
    }

    return string.IsNullOrWhiteSpace(fallback) ? "application/octet-stream" : fallback;
}

static string ExtensionFromContentType(string contentType)
{
    return contentType.ToLowerInvariant() switch
    {
        "image/jpeg" or "image/jpg" => ".jpg",
        "image/png" => ".png",
        "image/webp" => ".webp",
        "image/gif" => ".gif",
        "image/svg+xml" => ".svg",
        _ => ".bin"
    };
}

static string SanitiseKeySegment(string value)
{
    var safe = Regex.Replace(value.Trim(), @"[^A-Za-z0-9._-]+", "-");
    return string.IsNullOrWhiteSpace(safe) ? "unknown" : safe.Trim('-');
}

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
    public bool SkippedBecauseFileExists { get; init; }
    public int? AvatarFocusX { get; init; }
    public int? AvatarFocusY { get; init; }
    public int? AvatarZoom { get; init; }
    public int? PodiumFocusX { get; init; }
    public int? PodiumFocusY { get; init; }
    public int? PodiumZoom { get; init; }
    public int? CardFocusX { get; init; }
    public int? CardFocusY { get; init; }
}

public sealed class ExportOptions
{
    public string? ConnectionString { get; private init; }
    public string? DatabaseName { get; private init; }
    public string CollectionName { get; private init; } = "BoardGameImages";
    public string OutputRoot { get; private init; } = Path.Combine("artifacts", "image-blobs");
    public int BatchSize { get; private init; } = 100;
    public int? Limit { get; private init; }
    public bool Overwrite { get; private init; }
    public bool ShowHelp { get; private init; }

    public static ExportOptions Parse(string[] args)
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
            if (key is "overwrite" or "help")
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

        return new ExportOptions
        {
            ConnectionString = Get(values, "connection-string") ?? Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING"),
            DatabaseName = Get(values, "database") ?? Environment.GetEnvironmentVariable("MONGODB_DATABASE"),
            CollectionName = Get(values, "collection") ?? "BoardGameImages",
            OutputRoot = Get(values, "output") ?? Path.Combine("artifacts", "image-blobs"),
            BatchSize = int.TryParse(Get(values, "batch-size"), out var batchSize) ? batchSize : 100,
            Limit = int.TryParse(Get(values, "limit"), out var limit) ? limit : null,
            Overwrite = flags.Contains("overwrite"),
            ShowHelp = flags.Contains("help")
        };
    }

    public static void PrintHelp()
    {
        Console.WriteLine("""
        MongoImageBlobExporter

        Exports BoardGameImages documents from MongoDB to blob-style local files plus manifest.json.

        Usage:
          dotnet run --project Tools/MongoImageBlobExporter -- --connection-string "<mongodb>" --database "<db>" --output ".\artifacts\image-blobs"

        Options:
          --connection-string  MongoDB connection string. Or set MONGODB_CONNECTION_STRING.
          --database           MongoDB database name. Or set MONGODB_DATABASE.
          --collection         MongoDB collection name. Default: BoardGameImages.
          --output             Output folder. Default: .\artifacts\image-blobs.
          --batch-size         Mongo cursor batch size. Default: 100.
          --limit              Export only this many files, useful for testing.
          --overwrite          Replace existing blob files.
          --help               Show help.
        """);
    }

    private static string? Get(Dictionary<string, string?> values, string key)
    {
        return values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace BoardGameClubSoftware.Storage;

public sealed class ImageUploadValidator : IImageUploadValidator
{
    private static readonly Dictionary<string, string> ContentTypesByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp"
    };

    private static readonly HashSet<string> RejectedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".svg",
        ".exe",
        ".zip"
    };

    private readonly ImageUploadValidationOptions _options;

    public ImageUploadValidator(IOptions<ImageUploadValidationOptions> options)
    {
        _options = options.Value;
    }

    public ImageUploadValidationResult Validate(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return ImageUploadValidationResult.Failure("Choose an image file to upload.");
        }

        if (file.Length > _options.MaxSizeBytes)
        {
            return ImageUploadValidationResult.Failure("Image uploads are limited to 5 MB.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (RejectedExtensions.Contains(extension))
        {
            return ImageUploadValidationResult.Failure("That file type is not allowed.");
        }

        if (!ContentTypesByExtension.TryGetValue(extension, out var expectedContentType))
        {
            return ImageUploadValidationResult.Failure("Allowed image extensions are .jpg, .jpeg, .png, and .webp.");
        }

        var contentType = NormalizeContentType(file.ContentType);
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return ImageUploadValidationResult.Failure("Unknown image content types are not allowed.");
        }

        if (!ContentTypesByExtension.ContainsValue(contentType))
        {
            return ImageUploadValidationResult.Failure("Allowed image content types are image/jpeg, image/png, and image/webp.");
        }

        if (!string.Equals(contentType, expectedContentType, StringComparison.OrdinalIgnoreCase))
        {
            return ImageUploadValidationResult.Failure("The image extension does not match its content type.");
        }

        if (!MatchesFileSignature(file, contentType))
        {
            return ImageUploadValidationResult.Failure("The uploaded file does not match an allowed image format.");
        }

        return ImageUploadValidationResult.Success(extension.ToLowerInvariant(), contentType, file.Length);
    }

    private static string NormalizeContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return "";
        }

        return contentType.Split(';', 2)[0].Trim().ToLowerInvariant();
    }

    private static bool MatchesFileSignature(IFormFile file, string contentType)
    {
        Span<byte> header = stackalloc byte[12];
        using var stream = file.OpenReadStream();
        var bytesRead = stream.Read(header);
        var bytes = header[..bytesRead];

        return contentType switch
        {
            "image/jpeg" => IsJpeg(bytes),
            "image/png" => IsPng(bytes),
            "image/webp" => IsWebp(bytes),
            _ => false
        };
    }

    private static bool IsJpeg(ReadOnlySpan<byte> bytes)
        => bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF;

    private static bool IsPng(ReadOnlySpan<byte> bytes)
        => bytes.Length >= 8
            && bytes[0] == 0x89
            && bytes[1] == 0x50
            && bytes[2] == 0x4E
            && bytes[3] == 0x47
            && bytes[4] == 0x0D
            && bytes[5] == 0x0A
            && bytes[6] == 0x1A
            && bytes[7] == 0x0A;

    private static bool IsWebp(ReadOnlySpan<byte> bytes)
        => bytes.Length >= 12
            && bytes[0] == (byte)'R'
            && bytes[1] == (byte)'I'
            && bytes[2] == (byte)'F'
            && bytes[3] == (byte)'F'
            && bytes[8] == (byte)'W'
            && bytes[9] == (byte)'E'
            && bytes[10] == (byte)'B'
            && bytes[11] == (byte)'P';
}

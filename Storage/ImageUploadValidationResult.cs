namespace BoardGameClubSoftware.Storage;

public sealed record ImageUploadValidationResult(
    bool IsValid,
    string? ErrorMessage,
    string Extension,
    string ContentType,
    long SizeBytes)
{
    public static ImageUploadValidationResult Success(
        string extension,
        string contentType,
        long sizeBytes)
        => new(true, null, extension, contentType, sizeBytes);

    public static ImageUploadValidationResult Failure(string errorMessage)
        => new(false, errorMessage, "", "", 0);
}

namespace Board_Game_Software.Models;

public class StoredImage
{
    public int Id { get; set; }

    public string OwnerType { get; set; } = "";
    public int OwnerId { get; set; }

    public string BlobProvider { get; set; } = "AzureBlob";
    public string BlobKey { get; set; } = "";
    public string PublicUrl { get; set; } = "";

    public string OriginalFileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long SizeBytes { get; set; }

    public string? AltText { get; set; }
    public string? Caption { get; set; }

    public string? UploadedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

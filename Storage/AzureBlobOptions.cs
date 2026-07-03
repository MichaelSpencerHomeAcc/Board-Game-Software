namespace BoardGameClubSoftware.Storage;

public sealed class AzureBlobOptions
{
    public string ConnectionString { get; set; } = "";
    public string ContainerName { get; set; } = "images";
    public string PublicBaseUrl { get; set; } = "";
}

namespace BoardGameClubSoftware.Storage;

public static class ImageBlobKeyBuilder
{
    public static string ClubLogo(long clubId, Guid imageId, string extension)
        => $"clubs/{clubId}/logo/{imageId:D}{NormalizeExtension(extension)}";

    public static string GameCover(long gameId, Guid imageId, string extension)
        => $"games/{gameId}/cover/{imageId:D}{NormalizeExtension(extension)}";

    public static string UserAvatar(string userId, Guid imageId, string extension)
        => $"users/{Uri.EscapeDataString(userId)}/avatar/{imageId:D}{NormalizeExtension(extension)}";

    public static string GameNightPhoto(long gameNightId, Guid imageId, string extension)
        => $"game-nights/{gameNightId}/photos/{imageId:D}{NormalizeExtension(extension)}";

    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            throw new ArgumentException("Blob key extension is required.", nameof(extension));
        }

        return extension.StartsWith('.')
            ? extension.ToLowerInvariant()
            : $".{extension.ToLowerInvariant()}";
    }
}

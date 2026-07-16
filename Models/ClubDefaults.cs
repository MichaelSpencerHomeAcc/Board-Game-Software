namespace Board_Game_Software.Models;

public static class ClubDefaults
{
    public const string PublicClubType = "public_club";
    public const string PrivateGroupType = "private_group";
    public const string VenueType = "venue";
    public const string NetworkType = "network";

    public const string PublicVisibility = "public";
    public const string UnlistedVisibility = "unlisted";
    public const string PrivateVisibility = "private";

    public static readonly string[] ClubTypes =
    [
        PublicClubType,
        PrivateGroupType,
        VenueType,
        NetworkType
    ];

    public static readonly string[] VisibilityLevels =
    [
        PublicVisibility,
        UnlistedVisibility,
        PrivateVisibility
    ];

    public static bool IsValidClubType(string? value) =>
        ClubTypes.Contains(value);

    public static bool IsValidVisibility(string? value) =>
        VisibilityLevels.Contains(value);

    public static string GetDisplayName(string value) =>
        value switch
        {
            PublicClubType => "Public club",
            PrivateGroupType => "Private group",
            VenueType => "Venue",
            NetworkType => "Network",
            PublicVisibility => "Public",
            UnlistedVisibility => "Unlisted",
            PrivateVisibility => "Private",
            _ => string.Join(" ", value.Split('_', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => char.ToUpperInvariant(part[0]) + part[1..]))
        };
}

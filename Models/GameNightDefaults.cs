namespace Board_Game_Software.Models;

public static class GameNightDefaults
{
    public const string PublicVisibility = "public";
    public const string MembersOnlyVisibility = "members_only";
    public const string PrivateVisibility = "private";

    public static readonly string[] VisibilityLevels =
    [
        PublicVisibility,
        MembersOnlyVisibility,
        PrivateVisibility
    ];

    public static bool IsValidVisibility(string? value) =>
        !string.IsNullOrWhiteSpace(value) && VisibilityLevels.Contains(value);

    public static string GetDisplayName(string value) =>
        value switch
        {
            PublicVisibility => "Public",
            MembersOnlyVisibility => "Members only",
            PrivateVisibility => "Private",
            _ => value
        };
}

namespace Board_Game_Software.Models;

public static class AccountTierDefaults
{
    public const string FreePlayer = "free_player";
    public const string PlayerPlus = "player_plus";
    public const string PrivateGroupPlus = "private_group_plus";
    public const string ClubBasic = "club_basic";
    public const string ClubPro = "club_pro";
    public const string VenueNetwork = "venue_network";

    public static readonly string[] Tiers =
    {
        FreePlayer,
        PlayerPlus,
        PrivateGroupPlus,
        ClubBasic,
        ClubPro,
        VenueNetwork
    };

    public static bool IsValidTier(string? tier)
    {
        return !string.IsNullOrWhiteSpace(tier) && Tiers.Contains(tier);
    }

    public static string GetDisplayName(string? tier)
    {
        return tier switch
        {
            FreePlayer => "Free player",
            PlayerPlus => "Player Plus",
            PrivateGroupPlus => "Private Group Plus",
            ClubBasic => "Club Basic",
            ClubPro => "Club Pro",
            VenueNetwork => "Venue / Network",
            _ => "Free player"
        };
    }
}

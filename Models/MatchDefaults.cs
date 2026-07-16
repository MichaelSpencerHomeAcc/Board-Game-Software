namespace Board_Game_Software.Models;

public static class MatchDefaults
{
    public const string PersonalContext = "personal";
    public const string ClubGameNightContext = "club_game_night";
    public const string ClubOneOffContext = "club_one_off";
    public const string PrivateGroupContext = "private_group";

    public const string CasualPlayType = "casual_play";
    public const string ScoredMatchType = "scored_match";
    public const string TournamentMatchType = "tournament_match";
    public const string TeachingGameType = "teaching_game";

    public const string PublicVisibility = "public";
    public const string MembersOnlyVisibility = "members_only";
    public const string PrivateVisibility = "private";

    public static readonly string[] MatchTypes =
    {
        CasualPlayType,
        ScoredMatchType,
        TournamentMatchType,
        TeachingGameType
    };

    public static readonly string[] CompetitiveMatchTypes =
    {
        ScoredMatchType,
        TournamentMatchType
    };

    public static bool IsValidMatchType(string? matchType)
    {
        return !string.IsNullOrWhiteSpace(matchType) &&
            MatchTypes.Contains(matchType, StringComparer.OrdinalIgnoreCase);
    }

    public static bool IsCompetitiveMatchType(string? matchType)
    {
        return !string.IsNullOrWhiteSpace(matchType) &&
            CompetitiveMatchTypes.Contains(matchType, StringComparer.OrdinalIgnoreCase);
    }

    public static string GetMatchTypeLabel(string? matchType)
    {
        return matchType switch
        {
            CasualPlayType => "Casual play",
            ScoredMatchType => "Scored match",
            TournamentMatchType => "Tournament match",
            TeachingGameType => "Teaching game",
            _ => "Scored match"
        };
    }
}

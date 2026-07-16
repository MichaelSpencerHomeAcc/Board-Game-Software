using System.Text.RegularExpressions;

namespace Board_Game_Software.Models;

public static partial class BoardGameDefaults
{
    public const string ApprovedStatus = "approved";
    public const string PendingStatus = "pending";
    public const string RejectedStatus = "rejected";
    public const string MergedStatus = "merged";

    public const string ManualSource = "manual";
    public const string ClubSubmittedSource = "club_submitted";
    public const string LicensedImportSource = "licensed_import";
    public const string AdminCreatedSource = "admin_created";

    public const string SharedCopyLocalStatus = "linked";
    public const string LocalOnlyStatus = "local_only";
    public const string PendingReviewLocalStatus = "pending_review";
    public const string MergedLocalStatus = "merged";
    public const string RejectedLocalStatus = "rejected";

    public static readonly string[] GameStatuses =
    [
        ApprovedStatus,
        PendingStatus,
        RejectedStatus,
        MergedStatus
    ];

    public static readonly string[] GameSources =
    [
        ManualSource,
        ClubSubmittedSource,
        LicensedImportSource,
        AdminCreatedSource
    ];

    public static readonly string[] LocalGameStatuses =
    [
        SharedCopyLocalStatus,
        LocalOnlyStatus,
        PendingReviewLocalStatus,
        MergedLocalStatus,
        RejectedLocalStatus
    ];

    public static string NormalizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        return WhitespaceRegex()
            .Replace(name.Trim().ToLowerInvariant(), " ");
    }

    public static string GetDisplayName(string value) =>
        value switch
        {
            ApprovedStatus => "Approved",
            PendingStatus => "Pending review",
            RejectedStatus => "Rejected",
            MergedStatus => "Merged",
            ManualSource => "Manual",
            ClubSubmittedSource => "Club submitted",
            LicensedImportSource => "Licensed import",
            AdminCreatedSource => "Admin created",
            SharedCopyLocalStatus => "Shared library",
            LocalOnlyStatus => "Club-added game",
            PendingReviewLocalStatus => "Pending shared review",
            _ => value
        };

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}

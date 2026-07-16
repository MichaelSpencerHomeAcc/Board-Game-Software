namespace Board_Game_Software.Models;

public static class ClubMembershipDefaults
{
    public const string OwnerRole = "Owner";
    public const string AdminRole = "Admin";
    public const string MemberRole = "Member";
    public const string GuestRole = "Guest";

    public const string ActiveStatus = "active";
    public const string InvitedStatus = "invited";
    public const string PendingStatus = "pending";
    public const string RemovedStatus = "removed";

    public static readonly string[] Roles =
    [
        OwnerRole,
        AdminRole,
        MemberRole,
        GuestRole
    ];

    public static readonly string[] Statuses =
    [
        ActiveStatus,
        InvitedStatus,
        PendingStatus,
        RemovedStatus
    ];

    public static bool IsValidRole(string? value) =>
        Roles.Contains(value);

    public static bool IsValidStatus(string? value) =>
        Statuses.Contains(value);

    public static string GetDisplayName(string value) =>
        value switch
        {
            OwnerRole => "Owner",
            AdminRole => "Admin",
            MemberRole => "Member",
            GuestRole => "Guest",
            ActiveStatus => "Active",
            InvitedStatus => "Invited",
            PendingStatus => "Pending",
            RemovedStatus => "Removed",
            _ => value
        };
}

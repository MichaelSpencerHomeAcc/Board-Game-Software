using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;

namespace Board_Game_Software.Models;

public partial class ClubMembership
{
    public long Id { get; set; }

    public Guid Gid { get; set; }

    public bool Inactive { get; set; }

    public byte[]? VersionStamp { get; set; }

    [BindNever]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime TimeCreated { get; set; }

    [BindNever]
    public string ModifiedBy { get; set; } = string.Empty;

    public DateTime TimeModified { get; set; }

    public long FkBgdClub { get; set; }

    public string? UserId { get; set; }

    public string? GuestName { get; set; }

    public string Role { get; set; } = ClubMembershipDefaults.MemberRole;

    public string Status { get; set; } = ClubMembershipDefaults.ActiveStatus;

    public string? InvitedEmail { get; set; }

    public string? InvitedByUserId { get; set; }

    public DateTime JoinedAt { get; set; }

    public virtual Club FkBgdClubNavigation { get; set; } = null!;
}

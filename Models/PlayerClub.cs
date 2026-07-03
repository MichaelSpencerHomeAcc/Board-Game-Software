using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;

namespace Board_Game_Software.Models;

public partial class PlayerClub
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

    public long FkBgdPlayer { get; set; }

    public long FkBgdClub { get; set; }

    public DateTime JoinedAt { get; set; }

    public virtual Player FkBgdPlayerNavigation { get; set; } = null!;

    public virtual Club FkBgdClubNavigation { get; set; } = null!;
}

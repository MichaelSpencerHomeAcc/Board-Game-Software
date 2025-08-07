using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;

namespace Board_Game_Software.Models;

public partial class BoardGameMatchPlayer
{
    public long Id { get; set; }

    public Guid Gid { get; set; }

    public bool Inactive { get; set; }

    public byte[]? VersionStamp { get; set; }

    [BindNever]
    public string CreatedBy { get; set; } = String.Empty;

    public DateTime TimeCreated { get; set; }

    [BindNever]
    public string ModifiedBy { get; set; } = string.Empty;

    public DateTime TimeModified { get; set; }

    public long FkBgdBoardGameMatch { get; set; }

    public long FkBgdPlayer { get; set; }

    public long? FkBgdBoardGameMarker { get; set; }

    public virtual ICollection<BoardGameMatchPlayerResult> BoardGameMatchPlayerResults { get; set; } = new List<BoardGameMatchPlayerResult>();

    public virtual BoardGameMarker? FkBgdBoardGameMarkerNavigation { get; set; }

    public virtual BoardGameMatch FkBgdBoardGameMatchNavigation { get; set; } = null!;

    public virtual Player FkBgdPlayerNavigation { get; set; } = null!;
}

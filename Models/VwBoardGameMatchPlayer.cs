using System;
using System.Collections.Generic;

namespace Board_Game_Software.Models;

public partial class VwBoardGameMatchPlayer
{
    public long Id { get; set; }

    public Guid Gid { get; set; }

    public bool Inactive { get; set; }

    public byte[]? VersionStamp { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime TimeCreated { get; set; }

    public string ModifiedBy { get; set; } = null!;

    public DateTime TimeModified { get; set; }

    public long FkBgdBoardGameMatch { get; set; }

    public long FkBgdPlayer { get; set; }

    public long? FkBgdBoardGameMarker { get; set; }

    public string? BoardGameName { get; set; }

    public DateOnly? MatchDate { get; set; }

    public string? FullName { get; set; }

    public string? BoardGameMarkerTypeTypeDesc { get; set; }
}

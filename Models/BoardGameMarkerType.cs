using System;
using System.Collections.Generic;

namespace Board_Game_Software.Models;

public partial class BoardGameMarkerType
{
    public long Id { get; set; }

    public Guid Gid { get; set; }

    public bool Inactive { get; set; }

    public byte[]? VersionStamp { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime TimeCreated { get; set; }

    public string ModifiedBy { get; set; } = null!;

    public DateTime TimeModified { get; set; }

    public string TypeDesc { get; set; } = null!;

    public int? CustomSort { get; set; }

    public long? FkBgdMarkerAlignmentType { get; set; }

    public virtual ICollection<BoardGameMarker> BoardGameMarkers { get; set; } = new List<BoardGameMarker>();

    public virtual MarkerAlignmentType? FkBgdMarkerAlignmentTypeNavigation { get; set; }
}

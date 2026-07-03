using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;

namespace Board_Game_Software.Models;

public partial class BoardGameMarkerType
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

    public string TypeDesc { get; set; } = null!;

    public long? FkBgdClub { get; set; }

    public int? CustomSort { get; set; }

    public long? FkBgdMarkerAlignmentType { get; set; }

    public string? ImageId { get; set; }

    public long? FkBgdMarkerAdditionalType { get; set; }

    public virtual MarkerAdditionalType? FkBgdMarkerAdditionalTypeNavigation { get; set; }

    public virtual ICollection<BoardGameMarker> BoardGameMarkers { get; set; } = new List<BoardGameMarker>();

    public virtual Club? FkBgdClubNavigation { get; set; }

    public virtual MarkerAlignmentType? FkBgdMarkerAlignmentTypeNavigation { get; set; }

}

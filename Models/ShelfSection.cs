using System;
using System.Collections.Generic;

namespace Board_Game_Software.Models;

public partial class ShelfSection
{
    public long Id { get; set; }

    public Guid Gid { get; set; }

    public bool Inactive { get; set; }

    public byte[]? VersionStamp { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime TimeCreated { get; set; }

    public string ModifiedBy { get; set; } = null!;

    public DateTime TimeModified { get; set; }

    public long FkBgdShelf { get; set; }

    public byte RowNumber { get; set; }

    public byte SectionNumber { get; set; }

    public decimal HeightCm { get; set; }

    public decimal WidthCm { get; set; }

    public bool? Blocked { get; set; }

    public string? SectionName { get; set; }

    public virtual ICollection<BoardGameShelfSection> BoardGameShelfSections { get; set; } = new List<BoardGameShelfSection>();

    public virtual Shelf FkBgdShelfNavigation { get; set; } = null!;
}

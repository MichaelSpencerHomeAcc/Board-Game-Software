using System;
using System.Collections.Generic;

namespace Board_Game_Software.Models;

public partial class VwShelfLocationView
{
    public long Id { get; set; }

    public Guid Gid { get; set; }

    public string? Location { get; set; }

    public string ShelfName { get; set; } = null!;

    public byte RowNumber { get; set; }

    public byte SectionNumber { get; set; }

    public decimal WidthCm { get; set; }

    public bool? Blocked { get; set; }

    public string? SectionName { get; set; }
}

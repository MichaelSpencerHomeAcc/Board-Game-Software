using System;
using System.Collections.Generic;

namespace Board_Game_Software.Models;

public partial class VwPlayerBoardGameRating
{
    public long Id { get; set; }

    public Guid Gid { get; set; }

    public bool Inactive { get; set; }

    public byte[]? VersionStamp { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime TimeCreated { get; set; }

    public string ModifiedBy { get; set; } = null!;

    public DateTime TimeModified { get; set; }

    public long FkBgdPlayer { get; set; }

    public long FkBgdBoardGame { get; set; }

    public decimal? Rating { get; set; }

    public int? Constant { get; set; }

    public string? BoardGameName { get; set; }

    public string? FullName { get; set; }
}

using System;
using System.Collections.Generic;

namespace Board_Game_Software.Models;

public partial class BoardGameEloMethod
{
    public long Id { get; set; }

    public Guid Gid { get; set; }

    public bool Inactive { get; set; }

    public byte[]? VersionStamp { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime TimeCreated { get; set; }

    public string ModifiedBy { get; set; } = null!;

    public DateTime TimeModified { get; set; }

    public long FkBgdBoardGame { get; set; }

    public long FkBgdEloMethod { get; set; }

    public decimal? ExpectedWinRatioTeamA { get; set; }

    public string? Notes { get; set; }

    public virtual BoardGame FkBgdBoardGameNavigation { get; set; } = null!;

    public virtual EloMethod? FkBgdEloMethodNavigation { get; set; }
}

using System;
using System.Collections.Generic;

namespace Board_Game_Software.Models;

public partial class BoardGameMatchPlayerResult
{
    public long Id { get; set; }

    public Guid Gid { get; set; }

    public bool Inactive { get; set; }

    public byte[]? VersionStamp { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime TimeCreated { get; set; }

    public string ModifiedBy { get; set; } = null!;

    public DateTime TimeModified { get; set; }

    public long FkBgdBoardGameMatchPlayer { get; set; }

    public long? FkBgdResultType { get; set; }

    public decimal? FinalScore { get; set; }

    public bool Win { get; set; }

    public virtual BoardGameMatchPlayer FkBgdBoardGameMatchPlayerNavigation { get; set; } = null!;

    public virtual ResultType? FkBgdResultTypeNavigation { get; set; }
}

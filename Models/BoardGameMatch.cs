using System;
using System.Collections.Generic;

namespace Board_Game_Software.Models;

public partial class BoardGameMatch
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

    public DateOnly? MatchDate { get; set; }

    public long? FkBgdResultType { get; set; }

    public DateOnly? FinishedDate { get; set; }

    public virtual ICollection<BoardGameMatchPlayer> BoardGameMatchPlayers { get; set; } = new List<BoardGameMatchPlayer>();

    public virtual ICollection<BoardGameNightBoardGameMatch> BoardGameNightBoardGameMatches { get; set; } = new List<BoardGameNightBoardGameMatch>();

    public virtual BoardGame FkBgdBoardGameNavigation { get; set; } = null!;

    public virtual ResultType? FkBgdResultTypeNavigation { get; set; }
}

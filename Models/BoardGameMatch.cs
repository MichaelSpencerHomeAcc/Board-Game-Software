using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;

namespace Board_Game_Software.Models;

public partial class BoardGameMatch
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

    public long FkBgdBoardGame { get; set; }

    public DateTime? MatchDate { get; set; }

    public long? FkBgdResultType { get; set; }

    public DateTime? FinishedDate { get; set; }

    public bool? MatchComplete { get; set; } = false;

    public virtual ICollection<BoardGameMatchPlayer> BoardGameMatchPlayers { get; set; } = new List<BoardGameMatchPlayer>();

    public virtual ICollection<BoardGameNightBoardGameMatch> BoardGameNightBoardGameMatches { get; set; } = new List<BoardGameNightBoardGameMatch>();

    public virtual BoardGame FkBgdBoardGameNavigation { get; set; } = null!;

    public virtual ResultType? FkBgdResultTypeNavigation { get; set; }
}

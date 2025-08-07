using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;

namespace Board_Game_Software.Models;

public partial class BoardGameNight
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

    public DateOnly GameNightDate { get; set; }

    public bool Finished { get; set; }

    public virtual ICollection<BoardGameNightBoardGameMatch> BoardGameNightBoardGameMatches { get; set; } = new List<BoardGameNightBoardGameMatch>();

    public virtual ICollection<BoardGameNightPlayer> BoardGameNightPlayers { get; set; } = new List<BoardGameNightPlayer>();
}

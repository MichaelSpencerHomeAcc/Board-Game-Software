using System;
using System.Collections.Generic;

namespace Board_Game_Software.Models;

public partial class BoardGameVictoryConditionType
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

    public bool? Points { get; set; }

    public bool? WinLose { get; set; }

    public virtual ICollection<BoardGame> BoardGames { get; set; } = new List<BoardGame>();
}

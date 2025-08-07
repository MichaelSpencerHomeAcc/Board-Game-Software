using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;

namespace Board_Game_Software.Models;

public partial class ResultType
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

    public bool IsVictory { get; set; }

    public bool IsDefeat { get; set; }

    public int? CustomSort { get; set; }

    public virtual ICollection<BoardGameMatchPlayerResult> BoardGameMatchPlayerResults { get; set; } = new List<BoardGameMatchPlayerResult>();

    public virtual ICollection<BoardGameMatch> BoardGameMatches { get; set; } = new List<BoardGameMatch>();

    public virtual ICollection<BoardGameResult> BoardGameResults { get; set; } = new List<BoardGameResult>();
}

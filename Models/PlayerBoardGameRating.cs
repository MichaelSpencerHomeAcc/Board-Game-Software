using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Board_Game_Software.Models;

public partial class PlayerBoardGameRating
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

    public long FkBgdPlayer { get; set; }

    public long FkBgdBoardGame { get; set; }

    public decimal RatingMu { get; set; }

    public decimal RatingSigma { get; set; }

    public int MatchesPlayed { get; set; }

    public virtual BoardGame FkBgdBoardGameNavigation { get; set; } = null!;

    public virtual Player FkBgdPlayerNavigation { get; set; } = null!;
}
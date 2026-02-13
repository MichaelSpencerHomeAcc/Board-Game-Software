using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Board_Game_Software.Models;

public partial class BoardGameMatchPlayerResult
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

    public long FkBgdBoardGameMatchPlayer { get; set; }

    public long? FkBgdResultType { get; set; }

    public decimal? FinalScore { get; set; }

    public bool Win { get; set; }

    public FinalTeam? FinalTeam { get; set; }

    public decimal? PreMatchRatingMu { get; set; }

    public decimal? PreMatchRatingSigma { get; set; }

    public decimal? RatingChangeMu { get; set; }

    public decimal? RatingChangeSigma { get; set; }

    public virtual BoardGameMatchPlayer FkBgdBoardGameMatchPlayerNavigation { get; set; } = null!;

    public virtual ResultType? FkBgdResultTypeNavigation { get; set; }
}
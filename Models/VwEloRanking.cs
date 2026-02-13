using System;
using System.Collections.Generic;

namespace Board_Game_Software.Models;

public partial class VwEloRanking
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
    public string BoardGameName { get; set; } = null!;

    public long FkBgdPlayer { get; set; }
    public string? PlayerFirstName { get; set; }
    public string? PlayerLastName { get; set; }
    public string? PlayerFullName { get; set; }

    public decimal RatingMu { get; set; }
    public decimal RatingSigma { get; set; }
    public int MatchesPlayed { get; set; }

    public decimal DisplayRating { get; set; }

    public int TotalWins { get; set; }
    public long CalculatedRank { get; set; }

    // --- Dynamic Stats from SQL ---
    public string? AlignmentWins { get; set; }
    public string? MostPlayedToken { get; set; }
    public string? MainTokenAlignment { get; set; } // Add this line
}
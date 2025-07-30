using System;
using System.Collections.Generic;

namespace Board_Game_Software.Models;

public partial class VwGameNightPlayerRanking
{
    public long GameNightId { get; set; }

    public long PlayerId { get; set; }

    public string? PlayerName { get; set; }

    public long? TotalPoints { get; set; }

    public long? Ranking { get; set; }

    public string? OverallRank { get; set; }
}

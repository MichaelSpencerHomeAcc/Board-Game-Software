using System;
using System.Collections.Generic;

namespace Board_Game_Software.Models;

public partial class VwGameNightPlayerPoint
{
    public long GameNightId { get; set; }

    public long MatchId { get; set; }

    public long BoardGameId { get; set; }

    public long PlayerId { get; set; }

    public decimal? PlayerScore { get; set; }

    public bool? IsWinner { get; set; }

    public int? TotalPlayers { get; set; }

    public long? PointsAwarded { get; set; }
}

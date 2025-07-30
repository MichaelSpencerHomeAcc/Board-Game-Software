using System;
using System.Collections.Generic;

namespace Board_Game_Software.Models;

public partial class VwGameHistory
{
    public string? BoardGameName { get; set; }

    public int? PlayedCount { get; set; }

    public string? TopPlayers { get; set; }
}

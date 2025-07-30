using System;
using System.Collections.Generic;

namespace Board_Game_Software.Models;

public partial class VwBoardGame
{
    public long Id { get; set; }

    public Guid Gid { get; set; }

    public bool Inactive { get; set; }

    public byte[]? VersionStamp { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime TimeCreated { get; set; }

    public string ModifiedBy { get; set; } = null!;

    public DateTime TimeModified { get; set; }

    public string BoardGameName { get; set; } = null!;

    public long? FkBgdBoardGameType { get; set; }

    public long? FkBgdBoardGameVictoryConditionType { get; set; }

    public long? FkBgdPublisher { get; set; }

    public byte? PlayerCountMin { get; set; }

    public byte? PlayerCountMax { get; set; }

    public byte? PlayingTimeMinInMinutes { get; set; }

    public byte? PlayingTimeMaxInMinutes { get; set; }

    public decimal? ComplexityRating { get; set; }

    public DateOnly? ReleaseDate { get; set; }

    public bool HasMarkers { get; set; }

    public decimal? HeightCm { get; set; }

    public decimal? WidthCm { get; set; }

    public string? BoardGameSummary { get; set; }

    public string? HowToPlayHyperlink { get; set; }

    public string? LocationCode { get; set; }

    public int? PlayedCount { get; set; }

    public string? BoardGameType { get; set; }
}

using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;

namespace Board_Game_Software.Models;

public partial class BoardGame
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

    public virtual ICollection<BoardGameEloMethod> BoardGameEloMethods { get; set; } = new List<BoardGameEloMethod>();

    public virtual ICollection<BoardGameMarker> BoardGameMarkers { get; set; } = new List<BoardGameMarker>();

    public virtual ICollection<BoardGameMatch> BoardGameMatches { get; set; } = new List<BoardGameMatch>();

    public virtual ICollection<BoardGameResult> BoardGameResults { get; set; } = new List<BoardGameResult>();

    public virtual ICollection<BoardGameShelfSection> BoardGameShelfSections { get; set; } = new List<BoardGameShelfSection>();

    public virtual BoardGameType? FkBgdBoardGameTypeNavigation { get; set; }

    public virtual BoardGameVictoryConditionType? FkBgdBoardGameVictoryConditionTypeNavigation { get; set; }

    public virtual Publisher? FkBgdPublisherNavigation { get; set; }

    public virtual ICollection<PlayerBoardGameRating> PlayerBoardGameRatings { get; set; } = new List<PlayerBoardGameRating>();
}

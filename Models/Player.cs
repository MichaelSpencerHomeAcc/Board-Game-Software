using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;

namespace Board_Game_Software.Models;

public partial class Player
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

    public string? FirstName { get; set; }

    public string? MiddleName { get; set; }

    public string? LastName { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? FkdboAspNetUsers { get; set; }

    public long? FkBgdClub { get; set; }

    public virtual Club? FkBgdClubNavigation { get; set; }

    public virtual ICollection<BoardGameMatchPlayer> BoardGameMatchPlayers { get; set; } = new List<BoardGameMatchPlayer>();

    public virtual ICollection<BoardGameNightPlayer> BoardGameNightPlayers { get; set; } = new List<BoardGameNightPlayer>();

    public virtual ICollection<PlayerClub> PlayerClubs { get; set; } = new List<PlayerClub>();

    public virtual ICollection<PlayerBoardGameRating> PlayerBoardGameRatings { get; set; } = new List<PlayerBoardGameRating>();

    public virtual ICollection<PlayerBoardGame> PlayerBoardGames { get; set; } = new List<PlayerBoardGame>();

    public virtual ICollection<PlayerBoardGameStarRating> PlayerBoardGameStarRatings { get; set; } = new List<PlayerBoardGameStarRating>();

    public virtual ICollection<BoardGameVote> BoardGameVotes { get; set; } = new List<BoardGameVote>();

    public virtual ICollection<PlayerAchievement> PlayerAchievements { get; set; } = new List<PlayerAchievement>();
}

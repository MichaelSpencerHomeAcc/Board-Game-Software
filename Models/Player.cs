using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace Board_Game_Software.Models;

public partial class Player
{
    public long Id { get; set; }

    public Guid Gid { get; set; }

    public bool Inactive { get; set; }

    public byte[]? VersionStamp { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime TimeCreated { get; set; }

    public string ModifiedBy { get; set; } = null!;

    public DateTime TimeModified { get; set; }

    public string? FirstName { get; set; }

    public string? MiddleName { get; set; }

    public string? LastName { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? FkdboAspNetUsers { get; set; }

    public virtual ICollection<BoardGameMatchPlayer> BoardGameMatchPlayers { get; set; } = new List<BoardGameMatchPlayer>();

    public virtual ICollection<BoardGameNightPlayer> BoardGameNightPlayers { get; set; } = new List<BoardGameNightPlayer>();

    public virtual ICollection<PlayerBoardGameRating> PlayerBoardGameRatings { get; set; } = new List<PlayerBoardGameRating>();

    public virtual IdentityUser? User { get; set; }
}

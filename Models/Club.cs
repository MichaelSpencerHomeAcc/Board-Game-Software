using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;

namespace Board_Game_Software.Models;

public partial class Club
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

    public string ClubName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Slug { get; set; }

    public string? ContactEmail { get; set; }

    public string? VenueName { get; set; }

    public string? VenueAddress { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public virtual ICollection<ClubMembership> ClubMemberships { get; set; } = new List<ClubMembership>();

    public virtual ICollection<PlayerClub> PlayerClubs { get; set; } = new List<PlayerClub>();

    public virtual ICollection<BoardGame> BoardGames { get; set; } = new List<BoardGame>();

    public virtual ICollection<BoardGameNight> BoardGameNights { get; set; } = new List<BoardGameNight>();

    public virtual ICollection<Shelf> Shelves { get; set; } = new List<Shelf>();

    public virtual ICollection<BoardGameMarkerType> BoardGameMarkerTypes { get; set; } = new List<BoardGameMarkerType>();

    public virtual ICollection<Publisher> Publishers { get; set; } = new List<Publisher>();
}

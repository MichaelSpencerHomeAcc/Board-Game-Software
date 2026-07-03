using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;

namespace Board_Game_Software.Models;

public partial class Publisher
{
    public long Id { get; set; }

    public Guid Gid { get; set; }

    public bool Inactive { get; set; }

    public byte[]? VersionStamp { get; set; }

    [BindNever]
    public string CreatedBy { get; set; } = String.Empty;

    public DateTime TimeCreated { get; set; }

    [BindNever]
    public string ModifiedBy { get; set; } = String.Empty;

    public DateTime TimeModified { get; set; }

    public string PublisherName { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<BoardGame> BoardGames { get; set; } = new List<BoardGame>();
}

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Board_Game_Software.Models;

public partial class EloMethod
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

    public string MethodName { get; set; } = null!;

    public string? MethodDescription { get; set; }

    public decimal InitialMu { get; set; }

    public decimal InitialSigma { get; set; }

    public int? KFactor { get; set; }

    public virtual ICollection<BoardGameEloMethod> BoardGameEloMethods { get; set; } = new List<BoardGameEloMethod>();
}
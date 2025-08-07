using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;

namespace Board_Game_Software.Models;

public partial class BoardGameNightBoardGameMatch
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

    public long FkBgdBoardGameNight { get; set; }

    public long FkBgdBoardGameMatch { get; set; }

    public virtual BoardGameMatch FkBgdBoardGameMatchNavigation { get; set; } = null!;

    public virtual BoardGameNight FkBgdBoardGameNightNavigation { get; set; } = null!;
}

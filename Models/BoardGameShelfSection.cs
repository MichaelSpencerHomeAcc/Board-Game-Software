using System;
using System.Collections.Generic;

namespace Board_Game_Software.Models;

public partial class BoardGameShelfSection
{
    public long Id { get; set; }

    public Guid Gid { get; set; }

    public bool Inactive { get; set; }

    public byte[]? VersionStamp { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime TimeCreated { get; set; }

    public string ModifiedBy { get; set; } = null!;

    public DateTime TimeModified { get; set; }

    public long FkBgdBoardGame { get; set; }

    public long FkBgdShelfSection { get; set; }

    public virtual BoardGame FkBgdBoardGameNavigation { get; set; } = null!;

    public virtual ShelfSection FkBgdShelfSectionNavigation { get; set; } = null!;
}

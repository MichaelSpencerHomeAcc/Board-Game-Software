using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Board_Game_Software.Models;

public partial class BoardGameExpansion
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

    public long FkBgdBoardGame { get; set; }

    public long FkBgdExpansionBoardGame { get; set; }

    [BindNever]
    public virtual BoardGame FkBgdBoardGameNavigation { get; set; } = null!;

    [BindNever]
    public virtual BoardGame FkBgdExpansionBoardGameNavigation { get; set; } = null!;
}

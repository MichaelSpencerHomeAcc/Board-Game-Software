using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Board_Game_Software.Models;

public partial class PlayerAchievement
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

    public long FkBgdPlayer { get; set; }

    public string BadgeCode { get; set; } = string.Empty;

    public string BadgeTitle { get; set; } = string.Empty;

    public string BadgeDetail { get; set; } = string.Empty;

    public DateTime UnlockedAt { get; set; }

    public long? FkBgdBoardGame { get; set; }

    public long? FkBgdBoardGameMatch { get; set; }

    public long? FkBgdBoardGameNight { get; set; }

    public virtual Player FkBgdPlayerNavigation { get; set; } = null!;

    public virtual BoardGame? FkBgdBoardGameNavigation { get; set; }

    public virtual BoardGameMatch? FkBgdBoardGameMatchNavigation { get; set; }

    public virtual BoardGameNight? FkBgdBoardGameNightNavigation { get; set; }
}

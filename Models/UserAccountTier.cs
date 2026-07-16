using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Board_Game_Software.Models;

public partial class UserAccountTier
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

    public string UserId { get; set; } = string.Empty;

    public string SubscriptionTier { get; set; } = AccountTierDefaults.FreePlayer;

    public bool IsComped { get; set; }

    public string? Notes { get; set; }
}

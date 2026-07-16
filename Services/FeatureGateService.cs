using Board_Game_Software.Models;
using Microsoft.EntityFrameworkCore;

namespace Board_Game_Software.Services;

public interface IFeatureGateService
{
    Task<AccountTierContext> GetTierAsync(string? userId);
    bool CanLogPersonalPlays(AccountTierContext tier);
    bool CanCreatePrivateGroup(AccountTierContext tier);
    bool CanCreatePublicClub(AccountTierContext tier);
    bool CanHideAds(AccountTierContext tier);
    bool CanUseAdvancedStats(AccountTierContext tier);
}

public sealed class FeatureGateService : IFeatureGateService
{
    private readonly BoardGameDbContext _db;

    public FeatureGateService(BoardGameDbContext db)
    {
        _db = db;
    }

    public async Task<AccountTierContext> GetTierAsync(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new AccountTierContext(AccountTierDefaults.FreePlayer, false);
        }

        var tier = await _db.UserAccountTiers
            .AsNoTracking()
            .Where(t => !t.Inactive && t.UserId == userId)
            .OrderByDescending(t => t.TimeCreated)
            .Select(t => new AccountTierContext(t.SubscriptionTier, t.IsComped))
            .FirstOrDefaultAsync();

        return tier ?? new AccountTierContext(AccountTierDefaults.FreePlayer, false);
    }

    public bool CanLogPersonalPlays(AccountTierContext tier) => true;

    public bool CanCreatePrivateGroup(AccountTierContext tier)
    {
        return tier.IsComped || tier.SubscriptionTier is
            AccountTierDefaults.PrivateGroupPlus or
            AccountTierDefaults.ClubPro or
            AccountTierDefaults.VenueNetwork;
    }

    public bool CanCreatePublicClub(AccountTierContext tier)
    {
        return tier.IsComped || tier.SubscriptionTier is
            AccountTierDefaults.ClubBasic or
            AccountTierDefaults.ClubPro or
            AccountTierDefaults.VenueNetwork;
    }

    public bool CanHideAds(AccountTierContext tier)
    {
        return tier.IsComped || tier.SubscriptionTier != AccountTierDefaults.FreePlayer;
    }

    public bool CanUseAdvancedStats(AccountTierContext tier)
    {
        return tier.IsComped || tier.SubscriptionTier is
            AccountTierDefaults.PlayerPlus or
            AccountTierDefaults.PrivateGroupPlus or
            AccountTierDefaults.ClubPro or
            AccountTierDefaults.VenueNetwork;
    }
}

public sealed record AccountTierContext(string SubscriptionTier, bool IsComped)
{
    public string DisplayName => AccountTierDefaults.GetDisplayName(SubscriptionTier);
}

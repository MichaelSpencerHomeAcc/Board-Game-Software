using Board_Game_Software.Models;
using Microsoft.EntityFrameworkCore;

namespace Board_Game_Software.Services;

public interface ICurrentClubService
{
    Task<CurrentClubContext> GetCurrentClubAsync();
    Task<bool> SetCurrentClubAsync(long clubId);
    void SetPlatformAdminMode();
    void ClearCurrentClub();
}

public sealed class CurrentClubService : ICurrentClubService
{
    public const string CookieName = "bgm.currentClubId";
    public const string PlatformAdminCookieValue = "platform-admin";

    private readonly BoardGameDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentClubService(BoardGameDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<CurrentClubContext> GetCurrentClubAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var user = httpContext?.User;

        if (user?.Identity?.IsAuthenticated != true)
        {
            return CurrentClubContext.Empty;
        }

        var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = user.IsInRole("Admin");
        var selectedId = ReadSelectedClubId(httpContext);

        if (isAdmin && selectedId == null)
        {
            return new CurrentClubContext
            {
                IsPlatformAdminMode = true,
                CurrentRole = "Admin",
                AvailableClubs = await GetAvailableClubsAsync(userId, isAdmin)
            };
        }

        var clubs = await GetAvailableClubsAsync(userId, isAdmin);

        if (clubs.Count == 0)
        {
            return isAdmin
                ? new CurrentClubContext { IsPlatformAdminMode = true, CurrentRole = "Admin" }
                : CurrentClubContext.Empty;
        }

        var selectedClub = clubs.FirstOrDefault(c => c.ClubId == selectedId) ?? clubs.First();

        return new CurrentClubContext
        {
            HasClub = true,
            CurrentClubId = selectedClub.ClubId,
            CurrentClubName = selectedClub.ClubName,
            CurrentRole = selectedClub.Role,
            AvailableClubs = clubs
        };
    }

    private async Task<List<CurrentClubOption>> GetAvailableClubsAsync(string? userId, bool isAdmin)
    {
        var clubsQuery = _db.Clubs
            .AsNoTracking()
            .Where(c => !c.Inactive);

        if (!isAdmin)
        {
            clubsQuery = clubsQuery.Where(c => c.ClubMemberships.Any(m =>
                !m.Inactive &&
                m.Status == ClubMembershipDefaults.ActiveStatus &&
                m.UserId == userId));
        }

        var clubs = await clubsQuery
            .OrderBy(c => c.ClubName)
            .Select(c => new CurrentClubOption
            {
                ClubId = c.Id,
                ClubName = c.ClubName,
                Role = isAdmin
                    ? "Admin"
                    : c.ClubMemberships
                        .Where(m => !m.Inactive &&
                            m.Status == ClubMembershipDefaults.ActiveStatus &&
                            m.UserId == userId)
                        .Select(m => m.Role)
                        .FirstOrDefault()
            })
            .ToListAsync();

        return clubs;
    }

    public async Task<bool> SetCurrentClubAsync(long clubId)
    {
        var context = await GetCurrentClubAsync();
        if (!context.AvailableClubs.Any(c => c.ClubId == clubId))
        {
            return false;
        }

        var httpContext = _httpContextAccessor.HttpContext;
        httpContext?.Response.Cookies.Append(CookieName, clubId.ToString(), new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = httpContext.Request.IsHttps,
            Expires = DateTimeOffset.UtcNow.AddYears(1)
        });

        return true;
    }

    public void ClearCurrentClub()
    {
        _httpContextAccessor.HttpContext?.Response.Cookies.Delete(CookieName);
    }

    public void SetPlatformAdminMode()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        httpContext?.Response.Cookies.Append(CookieName, PlatformAdminCookieValue, new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = httpContext.Request.IsHttps,
            Expires = DateTimeOffset.UtcNow.AddYears(1)
        });
    }

    private static long? ReadSelectedClubId(HttpContext? httpContext)
    {
        if (httpContext?.Request.Cookies.TryGetValue(CookieName, out var rawValue) != true)
        {
            return null;
        }

        if (rawValue == PlatformAdminCookieValue)
        {
            return null;
        }

        if (long.TryParse(rawValue, out var clubId))
        {
            return clubId;
        }

        return null;
    }
}

public sealed class CurrentClubContext
{
    public static CurrentClubContext Empty { get; } = new();

    public bool IsPlatformAdminMode { get; init; }
    public bool HasClub { get; init; }
    public long? CurrentClubId { get; init; }
    public string? CurrentClubName { get; init; }
    public string? CurrentRole { get; init; }
    public IReadOnlyList<CurrentClubOption> AvailableClubs { get; init; } = Array.Empty<CurrentClubOption>();

    public bool CanManageCurrentClub => CurrentRole is "Admin" or "Owner";
}

public sealed class CurrentClubOption
{
    public long ClubId { get; init; }
    public string ClubName { get; init; } = string.Empty;
    public string? Role { get; init; }
}

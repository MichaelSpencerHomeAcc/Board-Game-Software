using Board_Game_Software.Models;
using Board_Game_Software.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Board_Game_Software.Pages.GameNight
{
    public class ArchiveModel : PageModel
    {
        private readonly BoardGameDbContext _db;
        private readonly ICurrentClubService _currentClubService;

        public ArchiveModel(BoardGameDbContext db, ICurrentClubService currentClubService)
        {
            _db = db;
            _currentClubService = currentClubService;
        }

        public List<VwBoardGameNight> ArchivedNights { get; set; } = new();

        public async Task OnGetAsync()
        {
            var cutoffDate = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-3);
            var currentClub = await _currentClubService.GetCurrentClubAsync();

            // Fetch only finished games older than 3 months
            var query = _db.BoardGameNights
                .AsNoTracking()
                .Where(n => !n.Inactive && n.Finished && n.GameNightDate < cutoffDate);

            if (!(User.IsInRole("Admin") && currentClub.IsPlatformAdminMode))
            {
                if (!currentClub.CurrentClubId.HasValue)
                {
                    ArchivedNights = new List<VwBoardGameNight>();
                    return;
                }

                var currentClubId = currentClub.CurrentClubId.Value;
                query = query.Where(n => n.FkBgdClub == currentClubId);
            }

            ArchivedNights = await query
                .OrderByDescending(n => n.GameNightDate)
                .Select(n => new VwBoardGameNight
                {
                    Id = n.Id,
                    Gid = n.Gid,
                    Inactive = n.Inactive,
                    VersionStamp = n.VersionStamp,
                    CreatedBy = n.CreatedBy,
                    TimeCreated = n.TimeCreated,
                    ModifiedBy = n.ModifiedBy,
                    TimeModified = n.TimeModified,
                    GameNightDate = n.GameNightDate,
                    Finished = n.Finished,
                    FkBgdClub = n.FkBgdClub,
                    ClubName = n.FkBgdClubNavigation != null ? n.FkBgdClubNavigation.ClubName : null,
                    PlayerCount = n.BoardGameNightPlayers.Count(p => !p.Inactive),
                    MatchCount = n.BoardGameNightBoardGameMatches.Count(m => !m.Inactive)
                })
                .ToListAsync();
        }
    }
}

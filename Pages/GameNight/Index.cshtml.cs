using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Board_Game_Software.Models;
using Board_Game_Software.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Board_Game_Software.Pages.GameNight
{
    public class IndexModel : PageModel
    {
        private readonly BoardGameDbContext _db;
        private readonly ICurrentClubService _currentClubService;

        public IndexModel(BoardGameDbContext db, ICurrentClubService currentClubService)
        {
            _db = db;
            _currentClubService = currentClubService;
        }

        public List<VwBoardGameNight> Nights { get; set; } = new();
        public bool IsAdmin { get; set; } // Tracks administrative permissions
        public CurrentClubContext CurrentClub { get; private set; } = CurrentClubContext.Empty;

        public async Task OnGetAsync()
        {
            // Determine if the current user has administrative rights
            IsAdmin = User.IsInRole("Admin");
            CurrentClub = await _currentClubService.GetCurrentClubAsync();

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            // Calculate the date 3 months ago
            var cutoffDate = today.AddMonths(-3);

            var query = _db.BoardGameNights.AsNoTracking().AsQueryable();

            if (IsAdmin && CurrentClub.IsPlatformAdminMode)
            {
                // Platform Admin sees all clubs.
            }
            else
            {
                if (!CurrentClub.CurrentClubId.HasValue)
                {
                    Nights = new List<VwBoardGameNight>();
                    return;
                }

                var currentClubId = CurrentClub.CurrentClubId.Value;
                query = query.Where(n => n.FkBgdClub == currentClubId);
            }

            // Filter out Finished games that are older than 3 months
            // Logic: Keep it if it's NOT finished OR if it's newer than the cutoff
            var items = await query
                .Where(n => !n.Finished || n.GameNightDate >= cutoffDate)
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

            // Sorting logic to keep Upcoming at the top
            Nights = items
                .OrderBy(n => n.Inactive)
                .ThenByDescending(n => !n.Finished && n.GameNightDate >= today)
                .ThenByDescending(n => !n.Finished && n.GameNightDate < today)
                .ThenBy(n => n.Finished)
                .ThenByDescending(n => n.GameNightDate)
                .ToList();
        }
    }
}

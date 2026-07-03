using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Board_Game_Software.Models;
using Board_Game_Software.Services;

namespace Board_Game_Software.Pages
{
    public class IndexModel : PageModel
    {
        private readonly BoardGameDbContext _context;
        private readonly ICurrentClubService _currentClubService;

        public IndexModel(BoardGameDbContext context, ICurrentClubService currentClubService)
        {
            _context = context;
            _currentClubService = currentClubService;
        }

        public int TotalGames { get; set; }
        public int TotalPlayers { get; set; }
        public int TotalGameNights { get; set; }
        public int TotalMatches { get; set; }
        public int OpenGameNights { get; set; }
        public string ScopeName { get; set; } = "Platform";
        public string ScopeDescription { get; set; } = "Global template catalogue and platform activity.";
        public List<RecentNightRow> RecentNights { get; set; } = new();

        public async Task OnGetAsync()
        {
            var currentClub = await _currentClubService.GetCurrentClubAsync();
            var isPlatformAdmin = User.IsInRole("Admin") && currentClub.IsPlatformAdminMode;
            var currentClubId = currentClub.CurrentClubId;

            ScopeName = isPlatformAdmin
                ? "Platform Admin"
                : currentClub.CurrentClubName ?? "No club selected";

            ScopeDescription = isPlatformAdmin
                ? "Managing global templates and platform setup."
                : currentClubId.HasValue
                    ? "Live view of this club's library, players, and game nights."
                    : "Choose a club to see its dashboard.";

            var gameQuery = _context.BoardGames.AsNoTracking().Where(g => !g.Inactive);
            var playerQuery = _context.Players.AsNoTracking().Where(p => !p.Inactive);
            var nightQuery = _context.BoardGameNights.AsNoTracking().Where(n => !n.Inactive);

            if (isPlatformAdmin)
            {
                gameQuery = gameQuery.Where(g => g.FkBgdClub == null);
            }
            else if (currentClubId.HasValue)
            {
                var clubId = currentClubId.Value;
                gameQuery = gameQuery.Where(g => g.FkBgdClub == clubId);
                playerQuery = playerQuery.Where(p => p.PlayerClubs.Any(pc => !pc.Inactive && pc.FkBgdClub == clubId));
                nightQuery = nightQuery.Where(n => n.FkBgdClub == clubId);
            }
            else
            {
                gameQuery = gameQuery.Where(g => false);
                playerQuery = playerQuery.Where(p => false);
                nightQuery = nightQuery.Where(n => false);
            }

            TotalGames = await gameQuery.CountAsync();
            TotalPlayers = await playerQuery.CountAsync();
            TotalGameNights = await nightQuery.CountAsync();
            OpenGameNights = await nightQuery.CountAsync(n => !n.Finished);
            TotalMatches = await nightQuery
                .SelectMany(n => n.BoardGameNightBoardGameMatches)
                .CountAsync(link => !link.Inactive);

            RecentNights = await nightQuery
                .OrderByDescending(n => n.GameNightDate)
                .ThenByDescending(n => n.Id)
                .Take(4)
                .Select(n => new RecentNightRow
                {
                    Id = n.Id,
                    Date = n.GameNightDate,
                    Finished = n.Finished,
                    PlayerCount = n.BoardGameNightPlayers.Count(p => !p.Inactive),
                    MatchCount = n.BoardGameNightBoardGameMatches.Count(m => !m.Inactive)
                })
                .ToListAsync();
        }

        public sealed class RecentNightRow
        {
            public long Id { get; init; }
            public DateOnly Date { get; init; }
            public bool Finished { get; init; }
            public int PlayerCount { get; init; }
            public int MatchCount { get; init; }
        }
    }
}

using Board_Game_Software.Models;
using Board_Game_Software.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Board_Game_Software.Pages.GameNight
{
    public class HostModel : PageModel
    {
        private readonly BoardGameDbContext _db;
        private readonly BoardGamePlayabilityService _playabilityService;
        private readonly ICurrentClubService _currentClubService;

        public HostModel(
            BoardGameDbContext db,
            BoardGamePlayabilityService playabilityService,
            ICurrentClubService currentClubService)
        {
            _db = db;
            _playabilityService = playabilityService;
            _currentClubService = currentClubService;
        }

        public BoardGameNight Night { get; private set; } = null!;
        public CurrentMatchRow? CurrentMatch { get; private set; }
        public SuggestedGameRow? NextGame { get; private set; }
        public List<PlayerRow> Players { get; private set; } = new();
        public List<QueueRow> Queue { get; private set; } = new();
        public bool CanEdit { get; private set; }

        public sealed class CurrentMatchRow
        {
            public long MatchId { get; init; }
            public string GameName { get; init; } = string.Empty;
            public Guid GameGid { get; init; }
            public DateTime? StartedAt { get; init; }
            public int PlayerCount { get; init; }
        }

        public sealed class SuggestedGameRow
        {
            public long GameId { get; init; }
            public string Name { get; init; } = string.Empty;
            public Guid GameGid { get; init; }
            public string Reason { get; init; } = string.Empty;
        }

        public sealed class PlayerRow
        {
            public long PlayerId { get; init; }
            public string Name { get; init; } = string.Empty;
            public Guid PlayerGid { get; init; }
        }

        public sealed class QueueRow
        {
            public long GameId { get; init; }
            public string Name { get; init; } = string.Empty;
            public int Votes { get; init; }
        }

        public async Task<IActionResult> OnGetAsync(long id)
        {
            CanEdit = User.IsInRole("Admin");

            var night = await _db.BoardGameNights.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id);
            if (night == null) return NotFound();
            if (!await CanAccessNightAsync(night)) return Forbid();
            Night = night;

            Players = await _db.BoardGameNightPlayers.AsNoTracking()
                .Where(np => np.FkBgdBoardGameNight == id && !np.Inactive)
                .Select(np => new PlayerRow
                {
                    PlayerId = np.FkBgdPlayer,
                    PlayerGid = np.FkBgdPlayerNavigation.Gid,
                    Name = (np.FkBgdPlayerNavigation.FirstName + " " + np.FkBgdPlayerNavigation.LastName).Trim()
                })
                .OrderBy(p => p.Name)
                .ToListAsync();

            CurrentMatch = await _db.BoardGameNightBoardGameMatches.AsNoTracking()
                .Where(nm => nm.FkBgdBoardGameNight == id
                    && !nm.Inactive
                    && nm.FkBgdBoardGameMatchNavigation.MatchComplete != true)
                .OrderByDescending(nm => nm.FkBgdBoardGameMatchNavigation.MatchDate)
                .Select(nm => new CurrentMatchRow
                {
                    MatchId = nm.FkBgdBoardGameMatch,
                    GameName = nm.FkBgdBoardGameMatchNavigation.FkBgdBoardGameNavigation.BoardGameName,
                    GameGid = nm.FkBgdBoardGameMatchNavigation.FkBgdBoardGameNavigation.Gid,
                    StartedAt = nm.FkBgdBoardGameMatchNavigation.MatchDate,
                    PlayerCount = nm.FkBgdBoardGameMatchNavigation.BoardGameMatchPlayers.Count(mp => !mp.Inactive)
                })
                .FirstOrDefaultAsync();

            Queue = await _db.BoardGameVotes.AsNoTracking()
                .Where(v => v.FkBgdBoardGameNight == id && !v.Inactive)
                .GroupBy(v => new { v.FkBgdBoardGame, v.FkBgdBoardGameNavigation.BoardGameName })
                .Select(g => new QueueRow
                {
                    GameId = g.Key.FkBgdBoardGame,
                    Name = g.Key.BoardGameName,
                    Votes = g.Count()
                })
                .OrderByDescending(q => q.Votes)
                .ThenBy(q => q.Name)
                .Take(4)
                .ToListAsync();

            NextGame = await GetNextGameAsync(id, Players.Count, night.FkBgdClub);
            return Page();
        }

        private async Task<SuggestedGameRow?> GetNextGameAsync(long nightId, int playerCount, long? clubId)
        {
            var playedTonight = await _db.BoardGameNightBoardGameMatches.AsNoTracking()
                .Where(nm => nm.FkBgdBoardGameNight == nightId && !nm.Inactive)
                .Select(nm => nm.FkBgdBoardGameMatchNavigation.FkBgdBoardGame)
                .ToListAsync();

            var voteCounts = await _db.BoardGameVotes.AsNoTracking()
                .Where(v => v.FkBgdBoardGameNight == nightId && !v.Inactive)
                .GroupBy(v => v.FkBgdBoardGame)
                .Select(g => new { GameId = g.Key, Votes = g.Count() })
                .ToDictionaryAsync(x => x.GameId, x => x.Votes);

            var playable = (await _playabilityService.GetPlayableBaseGamesAsync(clubId))
                .Where(g => !playedTonight.Contains(g.Id)
                    && (!g.MinPlayers.HasValue || g.MinPlayers.Value <= playerCount)
                    && (!g.MaxPlayers.HasValue || g.MaxPlayers.Value >= playerCount))
                .OrderByDescending(g => voteCounts.GetValueOrDefault(g.Id))
                .ThenBy(g => g.MaxMinutes ?? 255)
                .ThenBy(g => g.Name)
                .FirstOrDefault();

            if (playable == null) return null;

            return new SuggestedGameRow
            {
                GameId = playable.Id,
                GameGid = playable.Gid,
                Name = playable.Name,
                Reason = voteCounts.GetValueOrDefault(playable.Id) > 0
                    ? $"{voteCounts[playable.Id]} queue votes"
                    : playable.UsesExpansionPlayerCount ? "Fits with expansions" : "Fits this table"
            };
        }

        private async Task<bool> CanAccessNightAsync(BoardGameNight night)
        {
            var currentClub = await _currentClubService.GetCurrentClubAsync();
            if (User.IsInRole("Admin") && currentClub.IsPlatformAdminMode) return true;

            return night.FkBgdClub.HasValue && night.FkBgdClub == currentClub.CurrentClubId;
        }
    }
}

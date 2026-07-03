using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Board_Game_Software.Pages.Statistics
{
    public class GameRankingsModel : PageModel
    {
        private readonly BoardGameDbContext _context;

        public GameRankingsModel(BoardGameDbContext context)
        {
            _context = context;
        }

        public List<RankEntry> Leaderboard { get; private set; } = new();
        public BoardGame Game { get; private set; } = default!;
        public SelectList GameList { get; private set; } = default!;
        public RankingSummary Summary { get; private set; } = new();

        [BindProperty(SupportsGet = true)]
        public long? SelectedGameId { get; set; }

        public async Task OnGetAsync(long? id)
        {
            var games = await _context.BoardGames.AsNoTracking()
                .Where(bg => !bg.Inactive && bg.FkBgdBoardGameVictoryConditionTypeNavigation!.Points == true)
                .OrderBy(bg => bg.BoardGameName)
                .Select(bg => new { bg.Id, bg.BoardGameName })
                .ToListAsync();

            GameList = new SelectList(games, "Id", "BoardGameName");

            var targetId = id ?? SelectedGameId;
            if (!targetId.HasValue) return;

            SelectedGameId = targetId;

            Game = await _context.BoardGames.AsNoTracking()
                .Include(bg => bg.FkBgdPublisherNavigation)
                .FirstOrDefaultAsync(bg => bg.Id == targetId.Value) ?? new BoardGame();

            var resultRows = await _context.BoardGameMatchPlayerResults.AsNoTracking()
                .Where(r =>
                    !r.Inactive &&
                    r.FinalScore.HasValue &&
                    r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.FkBgdBoardGame == targetId.Value &&
                    r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.MatchComplete == true)
                .Select(r => new
                {
                    r.FkBgdBoardGameMatchPlayerNavigation.FkBgdPlayer,
                    PlayerGid = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdPlayerNavigation!.Gid,
                    PlayerName = ((r.FkBgdBoardGameMatchPlayerNavigation.FkBgdPlayerNavigation.FirstName ?? "") + " " +
                                  (r.FkBgdBoardGameMatchPlayerNavigation.FkBgdPlayerNavigation.LastName ?? "")).Trim(),
                    Score = r.FinalScore ?? 0,
                    MatchDate = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.FinishedDate ??
                                r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.MatchDate,
                    MatchId = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatch
                })
                .ToListAsync();

            Leaderboard = resultRows
                .GroupBy(r => r.FkBgdPlayer)
                .Select(g =>
                {
                    var best = g.OrderByDescending(x => x.Score).ThenByDescending(x => x.MatchDate).First();
                    return new RankEntry
                    {
                        PlayerId = best.FkBgdPlayer,
                        PlayerGid = best.PlayerGid,
                        PlayerName = string.IsNullOrWhiteSpace(best.PlayerName) ? "Unknown player" : best.PlayerName,
                        Score = best.Score,
                        Date = best.MatchDate,
                        MatchId = best.MatchId,
                        Plays = g.Count(),
                        AverageScore = g.Average(x => x.Score)
                    };
                })
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.PlayerName)
                .ToList();

            Summary = new RankingSummary
            {
                PlayersRanked = Leaderboard.Count,
                ScoresRecorded = resultRows.Count,
                BestScore = Leaderboard.FirstOrDefault()?.Score ?? 0,
                LeaderName = Leaderboard.FirstOrDefault()?.PlayerName ?? "No leader yet",
                AverageScore = resultRows.Any() ? resultRows.Average(r => r.Score) : 0,
                LastPlayed = resultRows.OrderByDescending(r => r.MatchDate).Select(r => r.MatchDate).FirstOrDefault()
            };
        }

        public sealed class RankingSummary
        {
            public int PlayersRanked { get; init; }
            public int ScoresRecorded { get; init; }
            public decimal BestScore { get; init; }
            public string LeaderName { get; init; } = "No leader yet";
            public decimal AverageScore { get; init; }
            public DateTime? LastPlayed { get; init; }
        }

        public sealed class RankEntry
        {
            public long PlayerId { get; init; }
            public Guid PlayerGid { get; init; }
            public string PlayerName { get; init; } = "";
            public decimal Score { get; init; }
            public DateTime? Date { get; init; }
            public long MatchId { get; init; }
            public int Plays { get; init; }
            public decimal AverageScore { get; init; }
        }
    }
}

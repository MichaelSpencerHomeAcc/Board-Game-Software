using Board_Game_Software.Models;
using Board_Game_Software.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Board_Game_Software.Pages.GameNight
{
    public class RecapModel : PageModel
    {
        private readonly BoardGameDbContext _db;
        private readonly GameNightService _nightService;

        public RecapModel(BoardGameDbContext db, GameNightService nightService)
        {
            _db = db;
            _nightService = nightService;
        }

        public BoardGameNight Night { get; private set; } = null!;
        public List<PlayerRecapRow> Podium { get; private set; } = new();
        public List<MatchRecapRow> Matches { get; private set; } = new();
        public MatchRecapRow? BestMatch { get; private set; }
        public UpsetRow? BiggestUpset { get; private set; }
        public List<RecordRow> NewRecords { get; private set; } = new();
        public List<PlayerStatRow> PlayerStats { get; private set; } = new();

        public sealed class PlayerRecapRow
        {
            public long PlayerId { get; init; }
            public string Name { get; init; } = string.Empty;
            public string AvatarUrl { get; init; } = "/images/default-avatar.png";
            public double Points { get; init; }
            public int Firsts { get; init; }
            public int Seconds { get; init; }
            public int Thirds { get; init; }
        }

        public sealed class MatchRecapRow
        {
            public long MatchId { get; init; }
            public string GameName { get; init; } = string.Empty;
            public Guid GameGid { get; init; }
            public DateTime? FinishedDate { get; init; }
            public int PlayerCount { get; init; }
            public string Winners { get; init; } = string.Empty;
            public decimal? TopScore { get; init; }
            public decimal? RatingSwing { get; init; }
        }

        public sealed class UpsetRow
        {
            public string PlayerName { get; init; } = string.Empty;
            public string GameName { get; init; } = string.Empty;
            public decimal Delta { get; init; }
        }

        public sealed class RecordRow
        {
            public string PlayerName { get; init; } = string.Empty;
            public string GameName { get; init; } = string.Empty;
            public decimal Score { get; init; }
            public decimal? PreviousBest { get; init; }
        }

        public sealed class PlayerStatRow
        {
            public string Name { get; init; } = string.Empty;
            public int Matches { get; init; }
            public int Wins { get; init; }
            public decimal? AverageScore { get; init; }
            public decimal RatingDelta { get; init; }
        }

        public async Task<IActionResult> OnGetAsync(long id)
        {
            var night = await _db.BoardGameNights.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id);
            if (night == null) return NotFound();
            Night = night;

            var scores = await _nightService.GetCurrentScores(id);
            Podium = scores
                .OrderByDescending(s => s.Points)
                .ThenByDescending(s => s.BestGamePoints)
                .ThenByDescending(s => s.Firsts)
                .Take(3)
                .Select(s => new PlayerRecapRow
                {
                    PlayerId = s.PlayerId,
                    Name = s.PlayerName,
                    AvatarUrl = s.AvatarUrl ?? "/images/default-avatar.png",
                    Points = s.Points,
                    Firsts = s.Firsts,
                    Seconds = s.Seconds,
                    Thirds = s.Thirds
                })
                .ToList();

            var matchIds = await _db.BoardGameNightBoardGameMatches.AsNoTracking()
                .Where(nm => nm.FkBgdBoardGameNight == id
                    && !nm.Inactive
                    && nm.FkBgdBoardGameMatchNavigation.MatchComplete == true)
                .Select(nm => nm.FkBgdBoardGameMatch)
                .ToListAsync();

            var rows = await _db.BoardGameMatchPlayerResults.AsNoTracking()
                .Where(r => !r.Inactive && matchIds.Contains(r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatch))
                .Select(r => new
                {
                    MatchId = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatch,
                    GameId = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.FkBgdBoardGame,
                    GameName = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.FkBgdBoardGameNavigation.BoardGameName,
                    GameGid = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.FkBgdBoardGameNavigation.Gid,
                    r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.FinishedDate,
                    PlayerId = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdPlayer,
                    PlayerName = (r.FkBgdBoardGameMatchPlayerNavigation.FkBgdPlayerNavigation.FirstName + " " + r.FkBgdBoardGameMatchPlayerNavigation.FkBgdPlayerNavigation.LastName).Trim(),
                    r.FinalScore,
                    r.Win,
                    r.RatingChangeMu
                })
                .ToListAsync();

            Matches = rows
                .GroupBy(r => r.MatchId)
                .Select(g => new MatchRecapRow
                {
                    MatchId = g.Key,
                    GameName = g.First().GameName,
                    GameGid = g.First().GameGid,
                    FinishedDate = g.First().FinishedDate,
                    PlayerCount = g.Select(x => x.PlayerId).Distinct().Count(),
                    Winners = string.Join(", ", g.Where(x => x.Win).Select(x => x.PlayerName).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct()),
                    TopScore = g.Max(x => x.FinalScore),
                    RatingSwing = g.Max(x => x.RatingChangeMu)
                })
                .OrderBy(m => m.FinishedDate)
                .ToList();

            BestMatch = Matches
                .OrderByDescending(m => m.PlayerCount)
                .ThenByDescending(m => m.TopScore ?? 0)
                .ThenByDescending(m => m.RatingSwing ?? 0)
                .FirstOrDefault();

            BiggestUpset = rows
                .Where(r => r.Win && r.RatingChangeMu.HasValue)
                .OrderByDescending(r => r.RatingChangeMu)
                .Select(r => new UpsetRow
                {
                    PlayerName = r.PlayerName,
                    GameName = r.GameName,
                    Delta = r.RatingChangeMu!.Value
                })
                .FirstOrDefault();

            var tonightMatchIds = rows.Select(r => r.MatchId).Distinct().ToList();
            var gameIds = rows.Select(r => r.GameId).Distinct().ToList();
            var previousHighs = await _db.BoardGameMatchPlayerResults.AsNoTracking()
                .Where(r => !r.Inactive
                    && r.FinalScore.HasValue
                    && gameIds.Contains(r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.FkBgdBoardGame)
                    && !tonightMatchIds.Contains(r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatch)
                    && r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.MatchComplete == true)
                .GroupBy(r => r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.FkBgdBoardGame)
                .Select(g => new { GameId = g.Key, Best = g.Max(r => r.FinalScore) })
                .ToListAsync();

            var previousByGame = previousHighs.ToDictionary(x => x.GameId, x => x.Best);
            NewRecords = rows
                .Where(r => r.FinalScore.HasValue && (!previousByGame.TryGetValue(r.GameId, out var previous) || r.FinalScore > previous))
                .GroupBy(r => r.GameId)
                .Select(g =>
                {
                    var best = g.OrderByDescending(r => r.FinalScore).First();
                    previousByGame.TryGetValue(best.GameId, out var previousBest);
                    return new RecordRow
                    {
                        PlayerName = best.PlayerName,
                        GameName = best.GameName,
                        Score = best.FinalScore!.Value,
                        PreviousBest = previousBest
                    };
                })
                .OrderByDescending(r => r.Score)
                .Take(5)
                .ToList();

            PlayerStats = rows
                .GroupBy(r => r.PlayerId)
                .Select(g => new PlayerStatRow
                {
                    Name = g.First().PlayerName,
                    Matches = g.Select(x => x.MatchId).Distinct().Count(),
                    Wins = g.Count(x => x.Win),
                    AverageScore = g.Any(x => x.FinalScore.HasValue) ? g.Where(x => x.FinalScore.HasValue).Average(x => x.FinalScore) : null,
                    RatingDelta = g.Sum(x => x.RatingChangeMu ?? 0)
                })
                .OrderByDescending(p => p.Wins)
                .ThenByDescending(p => p.RatingDelta)
                .ThenBy(p => p.Name)
                .ToList();

            return Page();
        }
    }
}

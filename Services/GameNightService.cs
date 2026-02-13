using Board_Game_Software.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Board_Game_Software.Services;

public class GameNightService
{
    private readonly BoardGameDbContext _db;

    public GameNightService(BoardGameDbContext db) => _db = db;

    public async Task<List<PlayerNightScore>> GetCurrentScores(long nightId)
    {
        var matches = await _db.BoardGameNightBoardGameMatches
            .Include(link => link.FkBgdBoardGameMatchNavigation)
                .ThenInclude(m => m.BoardGameMatchPlayers)
                    .ThenInclude(mp => mp.BoardGameMatchPlayerResults)
            .Include(link => link.FkBgdBoardGameMatchNavigation)
                .ThenInclude(m => m.BoardGameMatchPlayers)
                    .ThenInclude(mp => mp.FkBgdPlayerNavigation)
            .Where(link => link.FkBgdBoardGameNight == nightId && !link.Inactive)
            .ToListAsync();

        var scoreboard = new Dictionary<long, PlayerNightScore>();

        foreach (var matchLink in matches)
        {
            var match = matchLink.FkBgdBoardGameMatchNavigation;
            if (match == null || match.MatchComplete != true) continue;

            var players = match.BoardGameMatchPlayers.ToList();
            int N = players.Count;
            if (N == 0) continue;

            // Total points pool based on number of players (Sum of 1 to N)
            double totalPool = (N * (N + 1)) / 2.0;

            var winners = players.Where(p => p.BoardGameMatchPlayerResults.FirstOrDefault()?.Win == true).ToList();
            var losers = players.Where(p => p.BoardGameMatchPlayerResults.FirstOrDefault()?.Win == false).ToList();

            // Logic: Winners share 70% of the pool, Losers share 30%
            double winnerShare = totalPool * 0.70;
            double loserShare = totalPool * 0.30;

            if (winners.Any())
            {
                double ptsEach = winnerShare / winners.Count;
                foreach (var p in winners) AddPoints(scoreboard, p, ptsEach);
            }

            if (losers.Any())
            {
                double ptsEach = loserShare / losers.Count;
                foreach (var p in losers) AddPoints(scoreboard, p, ptsEach);
            }
        }

        return scoreboard.Values.OrderByDescending(s => s.Points).ToList();
    }

    private void AddPoints(Dictionary<long, PlayerNightScore> dict, BoardGameMatchPlayer mp, double pts)
    {
        var pId = mp.FkBgdPlayer;
        if (!dict.ContainsKey(pId))
        {
            dict[pId] = new PlayerNightScore
            {
                PlayerName = $"{mp.FkBgdPlayerNavigation.FirstName} {mp.FkBgdPlayerNavigation.LastName}".Trim()
            };
        }
        dict[pId].Points += pts;
    }
}

public class PlayerNightScore
{
    public string PlayerName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; } // NEW
    public double Points { get; set; }
}
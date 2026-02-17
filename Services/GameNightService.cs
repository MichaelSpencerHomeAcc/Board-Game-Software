using Board_Game_Software.Models;
using Microsoft.EntityFrameworkCore;
using System;
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

            var players = match.BoardGameMatchPlayers
                .Where(p => !p.Inactive) // if you have this flag
                .ToList();

            int N = players.Count;
            if (N == 0) continue;

            // Build per-player row
            var rows = players.Select(mp =>
            {
                var res = mp.BoardGameMatchPlayerResults?.FirstOrDefault(r => !r.Inactive);
                var win = res?.Win == true;

                // CHANGE THIS if your score field differs.
                // If your results table uses "Score" this compiles.
                decimal? score = res?.FinalScore;

                return new MatchRow
                {
                    MatchPlayer = mp,
                    PlayerId = mp.FkBgdPlayer,
                    PlayerName = $"{mp.FkBgdPlayerNavigation.FirstName} {mp.FkBgdPlayerNavigation.LastName}".Trim(),
                    PlayerGid = mp.FkBgdPlayerNavigation.Gid,
                    Win = win,
                    Score = score
                };
            }).ToList();

            bool hasScoreData = rows.Any(r => r.Score.HasValue);
            bool hasAnyWinner = rows.Any(r => r.Win);

            if (hasScoreData)
            {
                // SCORE MODE: higher score wins (if you later add per-game direction, flip this order)
                var ordered = rows
                    .OrderByDescending(r => r.Score ?? decimal.MinValue)
                    .ToList();

                var groups = GroupByTieKey(
                    ordered,
                    r => $"S:{(r.Score ?? decimal.MinValue):0.########}"
                );

                AssignRankPoints(N, groups, scoreboard);
            }
            else if (hasAnyWinner)
            {
                // WIN-ONLY MODE: winners share top slots, everyone else shares remaining slots
                var winners = rows.Where(r => r.Win).ToList();
                var others = rows.Where(r => !r.Win).ToList();

                var groups = new List<List<MatchRow>>();
                if (winners.Count > 0) groups.Add(winners);
                if (others.Count > 0) groups.Add(others);

                AssignRankPoints(N, groups, scoreboard);
            }
            else
            {
                // No score data and no winner flags -> can't rank this match
                continue;
            }
        }

        // Sort using tie-breakers:
        return scoreboard.Values
            .OrderByDescending(s => s.Points)
            .ThenByDescending(s => s.BestGamePoints)
            .ThenByDescending(s => s.Firsts)
            .ThenByDescending(s => s.Seconds)
            .ThenByDescending(s => s.Thirds)
            .ThenBy(s => s.PlayerName)
            .ToList();
    }

    private sealed class MatchRow
    {
        public BoardGameMatchPlayer MatchPlayer { get; init; } = null!;
        public long PlayerId { get; init; }
        public string PlayerName { get; init; } = string.Empty;
        public Guid PlayerGid { get; init; }
        public bool Win { get; init; }
        public decimal? Score { get; init; }
    }

    private static List<List<MatchRow>> GroupByTieKey(List<MatchRow> ordered, Func<MatchRow, string> tieKey)
    {
        var groups = new List<List<MatchRow>>();
        List<MatchRow>? current = null;
        string? currentKey = null;

        foreach (var r in ordered)
        {
            var key = tieKey(r);
            if (current == null || key != currentKey)
            {
                current = new List<MatchRow>();
                groups.Add(current);
                currentKey = key;
            }
            current.Add(r);
        }

        return groups;
    }

    /// <summary>
    /// Assigns points based on player count N:
    /// Slots are N, N-1, ..., 1.
    /// If a tie group covers multiple slots, they share the average of those slots.
    /// Also increments tie-break counters (first/second/third) based on the group's starting position.
    /// </summary>
    private void AssignRankPoints(int N, List<List<MatchRow>> groups, Dictionary<long, PlayerNightScore> scoreboard)
    {
        int position = 1; // 1-based

        foreach (var group in groups)
        {
            int groupSize = group.Count;
            if (groupSize == 0) continue;

            // Sum of slots covered by this tied group
            // slot value at position p is (N - p + 1)
            double totalForSlots = 0;
            for (int p = position; p < position + groupSize; p++)
                totalForSlots += (N - p + 1);

            double ptsEach = totalForSlots / groupSize;

            foreach (var g in group)
            {
                if (!scoreboard.TryGetValue(g.PlayerId, out var entry))
                {
                    entry = new PlayerNightScore
                    {
                        PlayerId = g.PlayerId,
                        PlayerName = g.PlayerName,
                        AvatarUrl = $"/media/player/{g.PlayerGid}"
                    };
                    scoreboard[g.PlayerId] = entry;
                }

                entry.Points += ptsEach;
                if (ptsEach > entry.BestGamePoints) entry.BestGamePoints = ptsEach;

                // Tie-break counters based on the starting position of the tie group
                if (position == 1) entry.Firsts++;
                else if (position == 2) entry.Seconds++;
                else if (position == 3) entry.Thirds++;
            }

            position += groupSize;
        }
    }
}

public class PlayerNightScore
{
    public long PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }

    public double Points { get; set; }

    // Tie-break helpers
    public double BestGamePoints { get; set; }
    public int Firsts { get; set; }
    public int Seconds { get; set; }
    public int Thirds { get; set; }
}

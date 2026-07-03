using Board_Game_Software.Models;
using Microsoft.EntityFrameworkCore;

namespace Board_Game_Software.Services
{
    public sealed class BoardGamePlayabilityService
    {
        private readonly BoardGameDbContext _db;

        public BoardGamePlayabilityService(BoardGameDbContext db)
        {
            _db = db;
        }

        public sealed class PlayableGame
        {
            public long Id { get; init; }
            public Guid Gid { get; init; }
            public string Name { get; init; } = string.Empty;
            public byte? MinPlayers { get; init; }
            public byte? MaxPlayers { get; init; }
            public byte? BaseMaxPlayers { get; init; }
            public byte? MaxMinutes { get; init; }
            public bool UsesExpansionPlayerCount => MaxPlayers.HasValue
                && BaseMaxPlayers.HasValue
                && MaxPlayers.Value > BaseMaxPlayers.Value;
        }

        public async Task<List<PlayableGame>> GetPlayableBaseGamesAsync()
        {
            var expansionGameIds = await _db.BoardGameExpansions.AsNoTracking()
                .Where(e => !e.Inactive)
                .Select(e => e.FkBgdExpansionBoardGame)
                .Distinct()
                .ToListAsync();

            var games = await _db.BoardGames.AsNoTracking()
                .Where(g => !g.Inactive && !g.IsExpansion && !expansionGameIds.Contains(g.Id))
                .Select(g => new
                {
                    g.Id,
                    g.Gid,
                    g.BoardGameName,
                    g.PlayerCountMin,
                    g.PlayerCountMax,
                    g.PlayingTimeMaxInMinutes
                })
                .ToListAsync();

            var expansionCounts = await _db.BoardGameExpansions.AsNoTracking()
                .Where(e => !e.Inactive && !e.FkBgdExpansionBoardGameNavigation.Inactive)
                .Select(e => new
                {
                    e.FkBgdBoardGame,
                    e.FkBgdExpansionBoardGameNavigation.PlayerCountMin,
                    e.FkBgdExpansionBoardGameNavigation.PlayerCountMax
                })
                .ToListAsync();

            var byBaseGame = expansionCounts
                .GroupBy(e => e.FkBgdBoardGame)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        Min = g.Select(e => e.PlayerCountMin).Where(v => v.HasValue).Select(v => v!.Value).DefaultIfEmpty().Min(),
                        Max = g.Select(e => e.PlayerCountMax).Where(v => v.HasValue).Select(v => v!.Value).DefaultIfEmpty().Max()
                    });

            return games.Select(g =>
            {
                byBaseGame.TryGetValue(g.Id, out var expansion);
                return new PlayableGame
                {
                    Id = g.Id,
                    Gid = g.Gid,
                    Name = g.BoardGameName,
                    MinPlayers = LowestPlayerCount(g.PlayerCountMin, expansion?.Min),
                    MaxPlayers = HighestPlayerCount(g.PlayerCountMax, expansion?.Max),
                    BaseMaxPlayers = g.PlayerCountMax,
                    MaxMinutes = g.PlayingTimeMaxInMinutes
                };
            }).ToList();
        }

        public static byte? LowestPlayerCount(byte? baseCount, byte? expansionCount)
        {
            if (!baseCount.HasValue) return expansionCount;
            if (!expansionCount.HasValue || expansionCount.Value == 0) return baseCount;
            return Math.Min(baseCount.Value, expansionCount.Value);
        }

        public static byte? HighestPlayerCount(byte? baseCount, byte? expansionCount)
        {
            if (!baseCount.HasValue) return expansionCount;
            if (!expansionCount.HasValue || expansionCount.Value == 0) return baseCount;
            return Math.Max(baseCount.Value, expansionCount.Value);
        }
    }
}

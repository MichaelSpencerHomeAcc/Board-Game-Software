using Board_Game_Software.Models;
using Microsoft.EntityFrameworkCore;

namespace Board_Game_Software.Services
{
    public sealed class AchievementService
    {
        private readonly BoardGameDbContext _db;

        public AchievementService(BoardGameDbContext db)
        {
            _db = db;
        }

        public async Task UnlockForMatchAsync(long matchId, long? nightId, string actor)
        {
            var rows = await _db.BoardGameMatchPlayerResults
                .Where(r => !r.Inactive
                    && r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatch == matchId
                    && r.FkBgdBoardGameMatchPlayerNavigation.FkBgdPlayer.HasValue)
                .Select(r => new
                {
                    MatchId = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatch,
                    GameId = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.FkBgdBoardGame,
                    GameName = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.FkBgdBoardGameNavigation.BoardGameName,
                    PlayerId = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdPlayer!.Value,
                    PlayerName = (r.FkBgdBoardGameMatchPlayerNavigation.FkBgdPlayerNavigation.FirstName + " " + r.FkBgdBoardGameMatchPlayerNavigation.FkBgdPlayerNavigation.LastName).Trim(),
                    r.Win,
                    r.RatingChangeMu
                })
                .ToListAsync();

            if (!rows.Any()) return;

            var now = DateTime.UtcNow;
            foreach (var row in rows)
            {
                await TryUnlockAsync(row.PlayerId, "first_play", "First Play", $"Played {row.GameName}.", actor, now, row.GameId, row.MatchId, nightId);

                if (row.Win)
                {
                    await TryUnlockAsync(row.PlayerId, "first_win", "First Win", $"Won {row.GameName}.", actor, now, row.GameId, row.MatchId, nightId);

                    var totalWins = await _db.BoardGameMatchPlayerResults.AsNoTracking()
                        .CountAsync(r => !r.Inactive
                            && r.Win
                            && r.FkBgdBoardGameMatchPlayerNavigation.FkBgdPlayer == row.PlayerId
                            && r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.MatchComplete == true);

                    if (totalWins >= 5)
                    {
                        await TryUnlockAsync(row.PlayerId, "five_wins", "Five Wins", "Reached five recorded wins.", actor, now);
                    }
                }

                if (row.RatingChangeMu >= 2.5m)
                {
                    await TryUnlockAsync(row.PlayerId, "upset_win", "Upset Win", $"+{row.RatingChangeMu:0.0} rating in {row.GameName}.", actor, now, row.GameId, row.MatchId, nightId);
                }

                if (row.RatingChangeMu >= 1.0m)
                {
                    await TryUnlockAsync(row.PlayerId, "rating_climber", "Rating Climber", $"Gained rating in {row.GameName}.", actor, now);
                }
            }

            if (nightId.HasValue)
            {
                await UnlockShelfSweepsAsync(nightId.Value, actor, now);
            }

            await _db.SaveChangesAsync();
        }

        private async Task UnlockShelfSweepsAsync(long nightId, string actor, DateTime now)
        {
            var wins = await _db.BoardGameMatchPlayerResults.AsNoTracking()
                .Where(r => !r.Inactive
                    && r.Win
                    && r.FkBgdBoardGameMatchPlayerNavigation.FkBgdPlayer.HasValue
                    && r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.MatchComplete == true
                    && r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.BoardGameNightBoardGameMatches
                        .Any(nm => !nm.Inactive && nm.FkBgdBoardGameNight == nightId))
                .Select(r => new
                {
                    PlayerId = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdPlayer!.Value,
                    Shelves = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.FkBgdBoardGameNavigation.BoardGameShelfSections
                        .Where(s => !s.Inactive)
                        .Select(s => new { s.FkBgdShelfSectionNavigation.FkBgdShelf, s.FkBgdShelfSectionNavigation.FkBgdShelfNavigation.ShelfName })
                        .ToList()
                })
                .ToListAsync();

            foreach (var group in wins.GroupBy(w => w.PlayerId))
            {
                foreach (var shelf in group.SelectMany(w => w.Shelves).GroupBy(s => s.FkBgdShelf))
                {
                    var shelfGameIds = await _db.BoardGameShelfSections.AsNoTracking()
                        .Where(s => !s.Inactive && s.FkBgdShelfSectionNavigation.FkBgdShelf == shelf.Key)
                        .Select(s => s.FkBgdBoardGame)
                        .Distinct()
                        .ToListAsync();

                    if (!shelfGameIds.Any()) continue;

                    var playerWonGameIds = await _db.BoardGameMatchPlayerResults.AsNoTracking()
                        .Where(r => !r.Inactive
                            && r.Win
                            && r.FkBgdBoardGameMatchPlayerNavigation.FkBgdPlayer == group.Key
                            && shelfGameIds.Contains(r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.FkBgdBoardGame))
                        .Select(r => r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.FkBgdBoardGame)
                        .Distinct()
                        .ToListAsync();

                    if (shelfGameIds.All(playerWonGameIds.Contains))
                    {
                        await TryUnlockAsync(group.Key, $"shelf_sweep_{shelf.Key}", "Shelf Sweep", $"Won every game on {shelf.First().ShelfName}.", actor, now, boardGameNightId: nightId);
                    }
                }
            }
        }

        private async Task TryUnlockAsync(
            long playerId,
            string badgeCode,
            string title,
            string detail,
            string actor,
            DateTime now,
            long? boardGameId = null,
            long? matchId = null,
            long? boardGameNightId = null)
        {
            var exists = await _db.PlayerAchievements.AnyAsync(a =>
                !a.Inactive
                && a.FkBgdPlayer == playerId
                && a.BadgeCode == badgeCode
                && a.FkBgdBoardGame == boardGameId
                && a.FkBgdBoardGameMatch == matchId
                && a.FkBgdBoardGameNight == boardGameNightId);

            if (exists) return;

            _db.PlayerAchievements.Add(new PlayerAchievement
            {
                Gid = Guid.NewGuid(),
                FkBgdPlayer = playerId,
                BadgeCode = badgeCode,
                BadgeTitle = title,
                BadgeDetail = detail,
                FkBgdBoardGame = boardGameId,
                FkBgdBoardGameMatch = matchId,
                FkBgdBoardGameNight = boardGameNightId,
                UnlockedAt = now,
                TimeCreated = now,
                TimeModified = now,
                CreatedBy = actor,
                ModifiedBy = actor
            });
        }
    }
}

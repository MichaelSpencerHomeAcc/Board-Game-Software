using Board_Game_Software.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Board_Game_Software.Pages.Admin.PlayerMerge
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly BoardGameDbContext _db;

        public IndexModel(BoardGameDbContext db)
        {
            _db = db;
        }

        public SelectList SourcePlayerOptions { get; set; } = default!;
        public SelectList DestinationPlayerOptions { get; set; } = default!;
        public MergePreview? Preview { get; private set; }

        [BindProperty] public long? SourcePlayerId { get; set; }
        [BindProperty] public long? DestinationPlayerId { get; set; }
        [BindProperty] public bool ConfirmMerge { get; set; }

        [TempData] public string? StatusMessage { get; set; }

        public sealed class MergePreview
        {
            public PlayerSummary Source { get; init; } = new();
            public PlayerSummary Destination { get; init; } = new();
            public int MatchPlayers { get; init; }
            public int GameNightLinks { get; init; }
            public int Ratings { get; init; }
            public int TopTenEntries { get; init; }
            public int StarRatings { get; init; }
            public int Votes { get; init; }
            public int Achievements { get; init; }
        }

        public sealed class PlayerSummary
        {
            public long Id { get; init; }
            public string Name { get; init; } = string.Empty;
            public string? ClubName { get; init; }
            public string? UserId { get; init; }
            public bool HasAccount => !string.IsNullOrWhiteSpace(UserId);
        }

        public async Task OnGetAsync()
        {
            await LoadPlayerOptionsAsync();
        }

        public async Task<IActionResult> OnPostPreviewAsync()
        {
            await LoadPlayerOptionsAsync();
            Preview = await BuildPreviewAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostMergeAsync()
        {
            if (!ConfirmMerge)
            {
                ModelState.AddModelError(string.Empty, "Tick the confirmation box before merging.");
                await LoadPlayerOptionsAsync();
                Preview = await BuildPreviewAsync();
                return Page();
            }

            try
            {
                var actor = User.Identity?.Name ?? "system";
                var result = await MergePlayersAsync(actor);
                StatusMessage = result;
                return RedirectToPage();
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await LoadPlayerOptionsAsync();
                Preview = await BuildPreviewAsync();
                return Page();
            }
        }

        private async Task<string> MergePlayersAsync(string actor)
        {
            if (!SourcePlayerId.HasValue || !DestinationPlayerId.HasValue)
            {
                throw new InvalidOperationException("Choose both players.");
            }

            var sourceId = SourcePlayerId.Value;
            var destinationId = DestinationPlayerId.Value;
            if (sourceId == destinationId)
            {
                throw new InvalidOperationException("Source and destination players must be different.");
            }

            await using var transaction = await _db.Database.BeginTransactionAsync();

            var source = await _db.Players.FirstOrDefaultAsync(p => p.Id == sourceId)
                ?? throw new InvalidOperationException("Source player not found.");
            var destination = await _db.Players.FirstOrDefaultAsync(p => p.Id == destinationId)
                ?? throw new InvalidOperationException("Destination player not found.");

            if (string.IsNullOrWhiteSpace(destination.FkdboAspNetUsers))
            {
                throw new InvalidOperationException("Destination player must be claimed by a user account.");
            }

            if (!string.IsNullOrWhiteSpace(source.FkdboAspNetUsers)
                && !string.IsNullOrWhiteSpace(destination.FkdboAspNetUsers)
                && source.FkdboAspNetUsers != destination.FkdboAspNetUsers)
            {
                throw new InvalidOperationException("Both players are already claimed by different user accounts.");
            }

            var now = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(destination.FkdboAspNetUsers))
            {
                destination.FkdboAspNetUsers = source.FkdboAspNetUsers;
            }

            destination.FkBgdClub ??= source.FkBgdClub;
            destination.ModifiedBy = actor;
            destination.TimeModified = now;

            await MergePlayerClubsAsync(sourceId, destinationId, actor, now);
            await MergeGameNightPlayersAsync(sourceId, destinationId, actor, now);
            await MergeRatingsAsync(sourceId, destinationId, actor, now);
            await MergeTopTenAsync(sourceId, destinationId, actor, now);
            await MergeVotesAsync(sourceId, destinationId, actor, now);
            await MergeAchievementsAsync(sourceId, destinationId, actor, now);

            await _db.BoardGameMatchPlayers
                .Where(x => x.FkBgdPlayer == sourceId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.FkBgdPlayer, destinationId)
                    .SetProperty(x => x.ModifiedBy, actor)
                    .SetProperty(x => x.TimeModified, now));

            await _db.PlayerBoardGameStarRatings
                .Where(x => x.FkBgdPlayer == sourceId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.FkBgdPlayer, destinationId)
                    .SetProperty(x => x.ModifiedBy, actor)
                    .SetProperty(x => x.TimeModified, now));

            _db.Players.Remove(source);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return $"Merged source player #{sourceId} into destination player #{destinationId}.";
        }

        private async Task MergePlayerClubsAsync(long sourceId, long destinationId, string actor, DateTime now)
        {
            var sourceRows = await _db.PlayerClubs.Where(x => x.FkBgdPlayer == sourceId).ToListAsync();
            var destinationClubIds = (await _db.PlayerClubs
                .Where(x => x.FkBgdPlayer == destinationId && !x.Inactive)
                .Select(x => x.FkBgdClub)
                .ToListAsync())
                .ToHashSet();

            foreach (var row in sourceRows)
            {
                if (!row.Inactive && destinationClubIds.Contains(row.FkBgdClub))
                {
                    _db.PlayerClubs.Remove(row);
                    continue;
                }

                row.FkBgdPlayer = destinationId;
                row.ModifiedBy = actor;
                row.TimeModified = now;
            }
        }

        private async Task MergeGameNightPlayersAsync(long sourceId, long destinationId, string actor, DateTime now)
        {
            var sourceRows = await _db.BoardGameNightPlayers.Where(x => x.FkBgdPlayer == sourceId).ToListAsync();
            var destinationNightIds = (await _db.BoardGameNightPlayers
                .Where(x => x.FkBgdPlayer == destinationId)
                .Select(x => x.FkBgdBoardGameNight)
                .ToListAsync())
                .ToHashSet();

            foreach (var row in sourceRows)
            {
                if (destinationNightIds.Contains(row.FkBgdBoardGameNight))
                {
                    _db.BoardGameNightPlayers.Remove(row);
                    continue;
                }

                row.FkBgdPlayer = destinationId;
                row.ModifiedBy = actor;
                row.TimeModified = now;
            }
        }

        private async Task MergeRatingsAsync(long sourceId, long destinationId, string actor, DateTime now)
        {
            var sourceRows = await _db.PlayerBoardGameRatings.Where(x => x.FkBgdPlayer == sourceId).ToListAsync();
            var destinationRows = await _db.PlayerBoardGameRatings
                .Where(x => x.FkBgdPlayer == destinationId)
                .ToDictionaryAsync(x => x.FkBgdBoardGame);

            foreach (var row in sourceRows)
            {
                if (destinationRows.TryGetValue(row.FkBgdBoardGame, out var destination))
                {
                    destination.MatchesPlayed += row.MatchesPlayed;
                    destination.ModifiedBy = actor;
                    destination.TimeModified = now;
                    _db.PlayerBoardGameRatings.Remove(row);
                    continue;
                }

                row.FkBgdPlayer = destinationId;
                row.ModifiedBy = actor;
                row.TimeModified = now;
            }
        }

        private async Task MergeTopTenAsync(long sourceId, long destinationId, string actor, DateTime now)
        {
            var sourceRows = await _db.PlayerBoardGames.Where(x => x.FkBgdPlayer == sourceId).ToListAsync();
            var destinationActiveRanks = (await _db.PlayerBoardGames
                .Where(x => x.FkBgdPlayer == destinationId && !x.Inactive)
                .Select(x => x.Rank)
                .ToListAsync())
                .ToHashSet();

            foreach (var row in sourceRows)
            {
                if (!row.Inactive && destinationActiveRanks.Contains(row.Rank))
                {
                    _db.PlayerBoardGames.Remove(row);
                    continue;
                }

                row.FkBgdPlayer = destinationId;
                row.ModifiedBy = actor;
                row.TimeModified = now;
            }
        }

        private async Task MergeVotesAsync(long sourceId, long destinationId, string actor, DateTime now)
        {
            var sourceRows = await _db.BoardGameVotes.Where(x => x.FkBgdPlayer == sourceId).ToListAsync();
            var destinationVotes = (await _db.BoardGameVotes
                .Where(x => x.FkBgdPlayer == destinationId)
                .Select(x => new VoteKey(x.FkBgdBoardGameNight, x.FkBgdBoardGame))
                .ToListAsync())
                .ToHashSet();

            foreach (var row in sourceRows)
            {
                if (destinationVotes.Contains(new VoteKey(row.FkBgdBoardGameNight, row.FkBgdBoardGame)))
                {
                    _db.BoardGameVotes.Remove(row);
                    continue;
                }

                row.FkBgdPlayer = destinationId;
                row.ModifiedBy = actor;
                row.TimeModified = now;
            }
        }

        private async Task MergeAchievementsAsync(long sourceId, long destinationId, string actor, DateTime now)
        {
            var sourceRows = await _db.PlayerAchievements.Where(x => x.FkBgdPlayer == sourceId).ToListAsync();
            var destinationAchievements = (await _db.PlayerAchievements
                .Where(x => x.FkBgdPlayer == destinationId)
                .Select(x => new AchievementKey(x.BadgeCode, x.FkBgdBoardGame, x.FkBgdBoardGameMatch, x.FkBgdBoardGameNight))
                .ToListAsync())
                .ToHashSet();

            foreach (var row in sourceRows)
            {
                if (destinationAchievements.Contains(new AchievementKey(row.BadgeCode, row.FkBgdBoardGame, row.FkBgdBoardGameMatch, row.FkBgdBoardGameNight)))
                {
                    _db.PlayerAchievements.Remove(row);
                    continue;
                }

                row.FkBgdPlayer = destinationId;
                row.ModifiedBy = actor;
                row.TimeModified = now;
            }
        }

        private async Task<MergePreview?> BuildPreviewAsync()
        {
            if (!SourcePlayerId.HasValue || !DestinationPlayerId.HasValue || SourcePlayerId == DestinationPlayerId)
            {
                return null;
            }

            var source = await GetPlayerSummaryAsync(SourcePlayerId.Value);
            var destination = await GetPlayerSummaryAsync(DestinationPlayerId.Value);
            if (source == null || destination == null)
            {
                return null;
            }

            return new MergePreview
            {
                Source = source,
                Destination = destination,
                MatchPlayers = await _db.BoardGameMatchPlayers.CountAsync(x => x.FkBgdPlayer == source.Id),
                GameNightLinks = await _db.BoardGameNightPlayers.CountAsync(x => x.FkBgdPlayer == source.Id),
                Ratings = await _db.PlayerBoardGameRatings.CountAsync(x => x.FkBgdPlayer == source.Id),
                TopTenEntries = await _db.PlayerBoardGames.CountAsync(x => x.FkBgdPlayer == source.Id),
                StarRatings = await _db.PlayerBoardGameStarRatings.CountAsync(x => x.FkBgdPlayer == source.Id),
                Votes = await _db.BoardGameVotes.CountAsync(x => x.FkBgdPlayer == source.Id),
                Achievements = await _db.PlayerAchievements.CountAsync(x => x.FkBgdPlayer == source.Id)
            };
        }

        private async Task<PlayerSummary?> GetPlayerSummaryAsync(long playerId)
        {
            var player = await _db.Players
                .AsNoTracking()
                .Include(p => p.PlayerClubs.Where(pc => !pc.Inactive))
                    .ThenInclude(pc => pc.FkBgdClubNavigation)
                .Where(p => p.Id == playerId)
                .FirstOrDefaultAsync();

            if (player == null)
            {
                return null;
            }

            return new PlayerSummary
            {
                Id = player.Id,
                Name = ((player.FirstName ?? "") + " " + (player.MiddleName ?? "") + " " + (player.LastName ?? "")).Trim(),
                ClubName = string.Join(", ", player.PlayerClubs
                    .Where(pc => !pc.Inactive)
                    .Select(pc => pc.FkBgdClubNavigation.ClubName)
                    .OrderBy(name => name)),
                UserId = player.FkdboAspNetUsers
            };
        }

        private async Task LoadPlayerOptionsAsync()
        {
            var players = await _db.Players
                .AsNoTracking()
                .Include(p => p.PlayerClubs.Where(pc => !pc.Inactive))
                    .ThenInclude(pc => pc.FkBgdClubNavigation)
                .Where(p => !p.Inactive)
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .ToListAsync();

            var options = players
                .Select(p =>
                {
                    var clubs = string.Join(", ", p.PlayerClubs
                        .Where(pc => !pc.Inactive)
                        .Select(pc => pc.FkBgdClubNavigation.ClubName)
                        .OrderBy(name => name));

                    return new PlayerOption(
                        p.Id,
                        (((p.FirstName ?? "") + " " + (p.MiddleName ?? "") + " " + (p.LastName ?? "")).Trim()
                            + " | #" + p.Id
                            + (string.IsNullOrWhiteSpace(clubs) ? "" : " | " + clubs)
                            + (string.IsNullOrWhiteSpace(p.FkdboAspNetUsers) ? " | unclaimed" : " | claimed")),
                        !string.IsNullOrWhiteSpace(p.FkdboAspNetUsers));
                })
                .ToList();

            SourcePlayerOptions = new SelectList(options, "Id", "Label");
            DestinationPlayerOptions = new SelectList(
                options.Where(p => p.Claimed),
                "Id",
                "Label");
        }

        private sealed record VoteKey(long NightId, long BoardGameId);
        private sealed record AchievementKey(string BadgeCode, long? BoardGameId, long? MatchId, long? NightId);
        private sealed record PlayerOption(long Id, string Label, bool Claimed);
    }
}

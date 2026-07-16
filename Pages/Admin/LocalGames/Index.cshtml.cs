using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Board_Game_Software.Pages.Admin.LocalGames
{
    public class IndexModel : PageModel
    {
        private readonly BoardGameDbContext _db;

        public IndexModel(BoardGameDbContext db)
        {
            _db = db;
        }

        public List<LocalGameRow> LocalGames { get; private set; } = new();
        public List<SelectListItem> CanonicalOptions { get; private set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Status { get; set; } = "open";

        [TempData]
        public string? StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.IsInRole("Admin")) return Forbid();

            await LoadAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostApproveNewAsync(long id)
        {
            if (!User.IsInRole("Admin")) return Forbid();

            var localGame = await LoadReviewableLocalGameAsync(id);
            if (localGame == null) return NotFound();

            var now = DateTime.UtcNow;
            var actor = User.Identity?.Name ?? "system";

            await using var transaction = await _db.Database.BeginTransactionAsync();

            var canonical = new BoardGame
            {
                Gid = Guid.NewGuid(),
                Inactive = false,
                CreatedBy = actor,
                ModifiedBy = actor,
                TimeCreated = now,
                TimeModified = now,
                BoardGameName = localGame.BoardGameName,
                NormalizedName = string.IsNullOrWhiteSpace(localGame.NormalizedName)
                    ? BoardGameDefaults.NormalizeName(localGame.BoardGameName)
                    : localGame.NormalizedName,
                GameStatus = BoardGameDefaults.ApprovedStatus,
                GameSource = BoardGameDefaults.AdminCreatedSource,
                LocalGameStatus = null,
                FkBgdBoardGameType = localGame.FkBgdBoardGameType,
                FkBgdBoardGameVictoryConditionType = localGame.FkBgdBoardGameVictoryConditionType,
                FkBgdPublisher = localGame.FkBgdPublisher,
                PlayerCountMin = localGame.PlayerCountMin,
                PlayerCountMax = localGame.PlayerCountMax,
                PlayingTimeMinInMinutes = localGame.PlayingTimeMinInMinutes,
                PlayingTimeMaxInMinutes = localGame.PlayingTimeMaxInMinutes,
                ComplexityRating = localGame.ComplexityRating,
                ReleaseDate = localGame.ReleaseDate,
                HasMarkers = localGame.HasMarkers,
                IsExpansion = localGame.IsExpansion,
                HeightCm = localGame.HeightCm,
                WidthCm = localGame.WidthCm,
                BoardGameSummary = localGame.BoardGameSummary,
                HowToPlayHyperlink = localGame.HowToPlayHyperlink
            };

            _db.BoardGames.Add(canonical);
            await _db.SaveChangesAsync();

            await CopyGameConfigurationAsync(localGame.Id, canonical.Id, actor, now);

            localGame.FkBgdTemplateBoardGame = canonical.Id;
            localGame.FkBgdMergedIntoBoardGame = null;
            localGame.GameStatus = BoardGameDefaults.ApprovedStatus;
            localGame.LocalGameStatus = BoardGameDefaults.SharedCopyLocalStatus;
            localGame.ModifiedBy = actor;
            localGame.TimeModified = now;

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            StatusMessage = $"{localGame.BoardGameName} was approved as a shared library game.";
            return RedirectToPage(new { Search, Status });
        }

        public async Task<IActionResult> OnPostMergeAsync(long id, long canonicalGameId)
        {
            if (!User.IsInRole("Admin")) return Forbid();

            var localGame = await LoadReviewableLocalGameAsync(id);
            if (localGame == null) return NotFound();

            var canonical = await _db.BoardGames.FirstOrDefaultAsync(g =>
                g.Id == canonicalGameId &&
                !g.Inactive &&
                g.FkBgdClub == null &&
                g.GameStatus == BoardGameDefaults.ApprovedStatus);

            if (canonical == null)
            {
                StatusMessage = "Choose an approved shared library game to merge into.";
                return RedirectToPage(new { Search, Status });
            }

            var now = DateTime.UtcNow;
            var actor = User.Identity?.Name ?? "system";

            localGame.FkBgdTemplateBoardGame = canonical.Id;
            localGame.FkBgdMergedIntoBoardGame = canonical.Id;
            localGame.GameStatus = BoardGameDefaults.ApprovedStatus;
            localGame.LocalGameStatus = BoardGameDefaults.MergedLocalStatus;
            localGame.ModifiedBy = actor;
            localGame.TimeModified = now;

            await AddAliasIfMissingAsync(canonical.Id, localGame.BoardGameName, BoardGameDefaults.ClubSubmittedSource, actor, now);
            await _db.SaveChangesAsync();

            StatusMessage = $"{localGame.BoardGameName} was linked to {canonical.BoardGameName}. Existing match history was preserved.";
            return RedirectToPage(new { Search, Status });
        }

        public async Task<IActionResult> OnPostRejectAsync(long id)
        {
            if (!User.IsInRole("Admin")) return Forbid();

            var localGame = await LoadReviewableLocalGameAsync(id);
            if (localGame == null) return NotFound();

            localGame.GameStatus = BoardGameDefaults.RejectedStatus;
            localGame.LocalGameStatus = BoardGameDefaults.RejectedLocalStatus;
            localGame.ModifiedBy = User.Identity?.Name ?? "system";
            localGame.TimeModified = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            StatusMessage = $"{localGame.BoardGameName} was rejected.";
            return RedirectToPage(new { Search, Status });
        }

        private async Task LoadAsync()
        {
            var query = _db.BoardGames
                .AsNoTracking()
                .Include(g => g.FkBgdClubNavigation)
                .Include(g => g.FkBgdPublisherNavigation)
                .Include(g => g.FkBgdTemplateBoardGameNavigation)
                .Include(g => g.FkBgdMergedIntoBoardGameNavigation)
                .Where(g => g.FkBgdClub != null);

            query = Status switch
            {
                "linked" => query.Where(g => g.LocalGameStatus == BoardGameDefaults.SharedCopyLocalStatus ||
                    g.LocalGameStatus == BoardGameDefaults.MergedLocalStatus),
                "rejected" => query.Where(g => g.LocalGameStatus == BoardGameDefaults.RejectedLocalStatus ||
                    g.GameStatus == BoardGameDefaults.RejectedStatus),
                _ => query.Where(g => g.LocalGameStatus == BoardGameDefaults.LocalOnlyStatus ||
                    g.LocalGameStatus == BoardGameDefaults.PendingReviewLocalStatus)
            };

            if (!string.IsNullOrWhiteSpace(Search))
            {
                query = query.Where(g => g.BoardGameName.Contains(Search) ||
                    (g.FkBgdClubNavigation != null && g.FkBgdClubNavigation.ClubName.Contains(Search)));
            }

            var rows = await query
                .OrderBy(g => g.BoardGameName)
                .Take(100)
                .Select(g => new LocalGameRow
                {
                    Id = g.Id,
                    Gid = g.Gid,
                    Name = g.BoardGameName,
                    ClubName = g.FkBgdClubNavigation == null ? "Unknown club" : g.FkBgdClubNavigation.ClubName,
                    PublisherName = g.FkBgdPublisherNavigation == null ? null : g.FkBgdPublisherNavigation.PublisherName,
                    Status = g.LocalGameStatus ?? g.GameStatus,
                    SubmittedAt = g.TimeCreated,
                    MatchCount = g.BoardGameMatches.Count(m => !m.Inactive),
                    LinkedGameName = g.FkBgdTemplateBoardGameNavigation == null
                        ? null
                        : g.FkBgdTemplateBoardGameNavigation.BoardGameName,
                    MergedIntoName = g.FkBgdMergedIntoBoardGameNavigation == null
                        ? null
                        : g.FkBgdMergedIntoBoardGameNavigation.BoardGameName,
                    PlayerRange = FormatPlayerRange(g.PlayerCountMin, g.PlayerCountMax)
                })
                .ToListAsync();

            LocalGames = rows;

            var canonicalQuery = _db.BoardGames
                .AsNoTracking()
                .Where(g => !g.Inactive &&
                    g.FkBgdClub == null &&
                    g.GameStatus == BoardGameDefaults.ApprovedStatus);

            if (!string.IsNullOrWhiteSpace(Search))
            {
                canonicalQuery = canonicalQuery.Where(g => g.BoardGameName.Contains(Search));
            }

            CanonicalOptions = await canonicalQuery
                .OrderBy(g => g.BoardGameName)
                .Take(100)
                .Select(g => new SelectListItem(g.BoardGameName, g.Id.ToString()))
                .ToListAsync();
        }

        private async Task<BoardGame?> LoadReviewableLocalGameAsync(long id)
        {
            return await _db.BoardGames.FirstOrDefaultAsync(g =>
                g.Id == id &&
                !g.Inactive &&
                g.FkBgdClub != null &&
                (g.LocalGameStatus == BoardGameDefaults.LocalOnlyStatus ||
                 g.LocalGameStatus == BoardGameDefaults.PendingReviewLocalStatus));
        }

        private async Task CopyGameConfigurationAsync(long sourceGameId, long targetGameId, string actor, DateTime now)
        {
            var markerCopies = await _db.BoardGameMarkers
                .AsNoTracking()
                .Where(marker => !marker.Inactive && marker.FkBgdBoardGame == sourceGameId)
                .Select(marker => new BoardGameMarker
                {
                    Gid = Guid.NewGuid(),
                    Inactive = false,
                    CreatedBy = actor,
                    ModifiedBy = actor,
                    TimeCreated = now,
                    TimeModified = now,
                    FkBgdBoardGame = targetGameId,
                    FkBgdBoardGameMarkerType = marker.FkBgdBoardGameMarkerType
                })
                .ToListAsync();

            var eloCopies = await _db.BoardGameEloMethods
                .AsNoTracking()
                .Where(method => !method.Inactive && method.FkBgdBoardGame == sourceGameId)
                .Select(method => new BoardGameEloMethod
                {
                    Gid = Guid.NewGuid(),
                    Inactive = false,
                    CreatedBy = actor,
                    ModifiedBy = actor,
                    TimeCreated = now,
                    TimeModified = now,
                    FkBgdBoardGame = targetGameId,
                    FkBgdEloMethod = method.FkBgdEloMethod,
                    ExpectedWinRatioTeamA = method.ExpectedWinRatioTeamA,
                    Notes = method.Notes
                })
                .ToListAsync();

            _db.BoardGameMarkers.AddRange(markerCopies);
            _db.BoardGameEloMethods.AddRange(eloCopies);
        }

        private async Task AddAliasIfMissingAsync(long boardGameId, string aliasName, string source, string actor, DateTime now)
        {
            var normalizedAlias = BoardGameDefaults.NormalizeName(aliasName);
            if (string.IsNullOrWhiteSpace(normalizedAlias))
            {
                return;
            }

            var canonicalName = await _db.BoardGames
                .Where(g => g.Id == boardGameId)
                .Select(g => g.NormalizedName)
                .FirstOrDefaultAsync();

            if (canonicalName == normalizedAlias)
            {
                return;
            }

            var exists = await _db.BoardGameAliases.AnyAsync(alias =>
                !alias.Inactive &&
                alias.FkBgdBoardGame == boardGameId &&
                alias.NormalizedAliasName == normalizedAlias);

            if (exists)
            {
                return;
            }

            _db.BoardGameAliases.Add(new BoardGameAlias
            {
                Gid = Guid.NewGuid(),
                Inactive = false,
                CreatedBy = actor,
                ModifiedBy = actor,
                TimeCreated = now,
                TimeModified = now,
                FkBgdBoardGame = boardGameId,
                AliasName = aliasName.Trim(),
                NormalizedAliasName = normalizedAlias,
                Source = source,
                CreatedAt = now
            });
        }

        private static string FormatPlayerRange(byte? minPlayers, byte? maxPlayers)
        {
            return (minPlayers, maxPlayers) switch
            {
                ({ } min, { } max) when min == max => min.ToString(),
                ({ } min, { } max) => $"{min}-{max}",
                ({ } min, null) => $"{min}+",
                (null, { } max) => $"Up to {max}",
                _ => "Not set"
            };
        }

        public sealed class LocalGameRow
        {
            public long Id { get; init; }
            public Guid Gid { get; init; }
            public string Name { get; init; } = string.Empty;
            public string ClubName { get; init; } = string.Empty;
            public string? PublisherName { get; init; }
            public string Status { get; init; } = string.Empty;
            public DateTime SubmittedAt { get; init; }
            public int MatchCount { get; init; }
            public string? LinkedGameName { get; init; }
            public string? MergedIntoName { get; init; }
            public string PlayerRange { get; init; } = string.Empty;
        }
    }
}

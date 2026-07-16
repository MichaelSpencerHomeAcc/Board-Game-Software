using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using Board_Game_Software.Services;

namespace Board_Game_Software.Pages.Statistics
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

        public DashboardStats Summary { get; private set; } = new();
        public PersonalStats PersonalSummary { get; private set; } = new();
        public List<HighScoreEntry> GlobalRecords { get; set; } = new();
        public List<HighScoreEntry> PersonalBests { get; set; } = new();
        public List<RecentMatchRow> RecentMatches { get; private set; } = new();
        public List<MonthlyActivityRow> MonthlyActivity { get; private set; } = new();
        public List<ActivityEntry> MostPlayedGames { get; private set; } = new();
        public List<ActivityEntry> MostActivePlayers { get; private set; } = new();
        public SelectList PlayerList { get; set; } = default!;
        public SelectList YearList { get; set; } = default!;
        public SelectList MonthList { get; set; } = default!;
        public string SelectedPlayerName { get; private set; } = string.Empty;
        public string ScopeLabel { get; private set; } = "Personal stats";

        [BindProperty(SupportsGet = true)] public long? SelectedPlayerId { get; set; }
        [BindProperty(SupportsGet = true)] public int? SelectedYear { get; set; }
        [BindProperty(SupportsGet = true)] public int? SelectedMonth { get; set; }

        public async Task OnGetAsync()
        {
            var currentClub = await _currentClubService.GetCurrentClubAsync();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ScopeLabel = currentClub.CurrentClubName ?? (currentClub.IsPlatformAdminMode ? "Platform stats" : "Personal stats");

            await LoadFiltersAsync(currentClub, userId);

            var completedMatches = _context.BoardGameMatches.AsNoTracking()
                .Where(m => !m.Inactive && m.MatchComplete == true);

            completedMatches = ApplyMatchScope(completedMatches, currentClub, userId);

            if (SelectedYear.HasValue)
                completedMatches = completedMatches.Where(m => m.FinishedDate.HasValue && m.FinishedDate.Value.Year == SelectedYear.Value);

            if (SelectedMonth.HasValue)
                completedMatches = completedMatches.Where(m => m.FinishedDate.HasValue && m.FinishedDate.Value.Month == SelectedMonth.Value);

            var completedMatchIds = await completedMatches.Select(m => m.Id).ToListAsync();

            var playerResultRows = await _context.BoardGameMatchPlayerResults.AsNoTracking()
                .Where(r => !r.Inactive
                    && r.FkBgdBoardGameMatchPlayerNavigation.FkBgdPlayer.HasValue
                    && completedMatchIds.Contains(r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatch))
                .Select(r => new
                {
                    MatchId = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatch,
                    GameId = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.FkBgdBoardGame,
                    GameGid = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.FkBgdBoardGameNavigation.Gid,
                    GameName = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.FkBgdBoardGameNavigation.BoardGameName,
                    PlayerId = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdPlayer!.Value,
                    PlayerGid = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdPlayerNavigation!.Gid,
                    PlayerName = (r.FkBgdBoardGameMatchPlayerNavigation.FkBgdPlayerNavigation!.FirstName + " " + r.FkBgdBoardGameMatchPlayerNavigation.FkBgdPlayerNavigation.LastName).Trim(),
                    MatchDate = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.FinishedDate ?? r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.MatchDate,
                    MatchType = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.MatchType,
                    r.FinalScore,
                    r.Win,
                    IsPointGame = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.FkBgdBoardGameNavigation.FkBgdBoardGameVictoryConditionTypeNavigation!.Points == true
                })
                .ToListAsync();

            var competitiveRows = playerResultRows
                .Where(r => MatchDefaults.IsCompetitiveMatchType(r.MatchType))
                .ToList();

            Summary = new DashboardStats
            {
                CompletedMatches = playerResultRows.Select(r => r.MatchId).Distinct().Count(),
                ActivePlayers = playerResultRows.Select(r => r.PlayerId).Distinct().Count(),
                GamesPlayed = playerResultRows.Select(r => r.GameId).Distinct().Count(),
                TopGame = playerResultRows.GroupBy(r => r.GameName).OrderByDescending(g => g.Select(x => x.MatchId).Distinct().Count()).Select(g => g.Key).FirstOrDefault() ?? "None yet",
                TopWinner = competitiveRows.Where(r => r.Win).GroupBy(r => r.PlayerName).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault() ?? "None yet"
            };

            MostPlayedGames = playerResultRows
                .GroupBy(r => new { r.GameId, r.GameName, r.GameGid })
                .Select(g => new ActivityEntry
                {
                    Id = g.Key.GameId,
                    Gid = g.Key.GameGid,
                    Name = g.Key.GameName,
                    Count = g.Select(x => x.MatchId).Distinct().Count()
                })
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Name)
                .Take(5)
                .ToList();

            MostActivePlayers = playerResultRows
                .GroupBy(r => new { r.PlayerId, r.PlayerName, r.PlayerGid })
                .Select(g => new ActivityEntry
                {
                    Id = g.Key.PlayerId,
                    Gid = g.Key.PlayerGid,
                    Name = string.IsNullOrWhiteSpace(g.Key.PlayerName) ? $"Player {g.Key.PlayerId}" : g.Key.PlayerName,
                    Count = g.Select(x => x.MatchId).Distinct().Count()
                })
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Name)
                .Take(5)
                .ToList();

            RecentMatches = playerResultRows
                .GroupBy(r => r.MatchId)
                .Select(g => new RecentMatchRow
                {
                    MatchId = g.Key,
                    GameName = g.First().GameName,
                    GameGid = g.First().GameGid,
                    MatchDate = g.First().MatchDate,
                    Winners = string.Join(", ", g.Where(x => x.Win).Select(x => x.PlayerName).Distinct())
                })
                .OrderByDescending(r => r.MatchDate)
                .Take(8)
                .ToList();

            MonthlyActivity = playerResultRows
                .Where(r => r.MatchDate.HasValue)
                .GroupBy(r => new { r.MatchDate!.Value.Year, r.MatchDate.Value.Month })
                .Select(g => new MonthlyActivityRow
                {
                    SortKey = g.Key.Year * 100 + g.Key.Month,
                    Label = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(g.Key.Month) + " " + g.Key.Year,
                    Matches = g.Select(x => x.MatchId).Distinct().Count()
                })
                .OrderByDescending(x => x.SortKey)
                .Take(6)
                .OrderBy(x => x.SortKey)
                .ToList();

            GlobalRecords = competitiveRows
                .Where(r => r.IsPointGame && r.FinalScore.HasValue)
                .GroupBy(r => r.GameId)
                .Select(g => g.OrderByDescending(x => x.FinalScore).ThenByDescending(x => x.MatchDate).First())
                .OrderBy(x => x.GameName)
                .Select(x => new HighScoreEntry
                {
                    GameId = x.GameId,
                    GameGid = x.GameGid,
                    PlayerGid = x.PlayerGid,
                    GameName = x.GameName,
                    PlayerName = x.PlayerName,
                    Score = x.FinalScore ?? 0,
                    Date = x.MatchDate
                })
                .ToList();

            if (!SelectedPlayerId.HasValue) return;

            SelectedPlayerName = playerResultRows.FirstOrDefault(r => r.PlayerId == SelectedPlayerId.Value)?.PlayerName ?? string.Empty;
            var selectedPlayerRows = playerResultRows.Where(r => r.PlayerId == SelectedPlayerId.Value).ToList();
            var selectedCompetitiveRows = competitiveRows.Where(r => r.PlayerId == SelectedPlayerId.Value).ToList();

            var selectedCompetitiveMatchCount = selectedCompetitiveRows.Select(r => r.MatchId).Distinct().Count();
            PersonalSummary = new PersonalStats
            {
                TotalPlays = selectedPlayerRows.Select(r => r.MatchId).Distinct().Count(),
                Wins = selectedCompetitiveRows.Count(r => r.Win),
                GamesPlayed = selectedPlayerRows.Select(r => r.GameId).Distinct().Count(),
                WinRate = selectedCompetitiveMatchCount > 0
                    ? selectedCompetitiveRows.Count(r => r.Win) * 100m / selectedCompetitiveMatchCount
                    : 0,
                MostPlayedGame = selectedPlayerRows
                    .GroupBy(r => r.GameName)
                    .OrderByDescending(g => g.Select(x => x.MatchId).Distinct().Count())
                    .Select(g => g.Key)
                    .FirstOrDefault() ?? "None yet",
                LastPlayed = selectedPlayerRows
                    .OrderByDescending(r => r.MatchDate)
                    .Select(r => r.MatchDate)
                    .FirstOrDefault()
            };

            var playerScores = selectedCompetitiveRows.Where(r => r.IsPointGame && r.FinalScore.HasValue).ToList();
            var bestPerGame = playerScores.GroupBy(r => r.GameId).Select(g => g.OrderByDescending(x => x.FinalScore).ThenByDescending(x => x.MatchDate).First()).ToList();
            var bestGameIds = bestPerGame.Select(b => b.GameId).ToHashSet();

            var bestScoresByGame = competitiveRows
                .Where(r => r.IsPointGame && r.FinalScore.HasValue && bestGameIds.Contains(r.GameId))
                .GroupBy(r => new { r.GameId, r.PlayerId })
                .Select(g => new { g.Key.GameId, g.Key.PlayerId, Score = g.Max(x => x.FinalScore ?? 0) })
                .GroupBy(x => x.GameId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Score).ToList());

            PersonalBests = bestPerGame
                .Select(x =>
                {
                    bestScoresByGame.TryGetValue(x.GameId, out var ranks);
                    return new HighScoreEntry
                    {
                        GameId = x.GameId,
                        GameGid = x.GameGid,
                        GameName = x.GameName,
                        Score = x.FinalScore ?? 0,
                        Date = x.MatchDate,
                        Rank = ranks == null ? 0 : ranks.FindIndex(r => r.PlayerId == SelectedPlayerId.Value) + 1
                    };
                })
                .OrderBy(x => x.Rank == 0 ? int.MaxValue : x.Rank)
                .ThenBy(x => x.GameName)
                .ToList();
        }

        private async Task LoadFiltersAsync(CurrentClubContext currentClub, string? userId)
        {
            var playerQuery = _context.Players.AsNoTracking()
                .Where(p => !p.Inactive);

            if (!currentClub.IsPlatformAdminMode && currentClub.CurrentClubId.HasValue)
            {
                var clubId = currentClub.CurrentClubId.Value;
                playerQuery = playerQuery.Where(p => p.PlayerClubs.Any(pc => !pc.Inactive && pc.FkBgdClub == clubId));
            }
            else if (!currentClub.IsPlatformAdminMode)
            {
                playerQuery = playerQuery.Where(p => p.FkdboAspNetUsers == userId);
            }

            var players = await playerQuery
                .Select(p => new { p.Id, Name = (p.FirstName + " " + p.LastName).Trim() })
                .OrderBy(p => p.Name)
                .ToListAsync();

            PlayerList = new SelectList(players, "Id", "Name");

            var yearsQuery = ApplyMatchScope(_context.BoardGameMatches.AsNoTracking(), currentClub, userId);
            var years = await yearsQuery
                .Where(m => m.FinishedDate.HasValue)
                .Select(m => m.FinishedDate!.Value.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();

            YearList = new SelectList(years);
            MonthList = new SelectList(Enumerable.Range(1, 12).Select(m => new { Value = m, Text = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m) }), "Value", "Text");
        }

        private IQueryable<BoardGameMatch> ApplyMatchScope(IQueryable<BoardGameMatch> query, CurrentClubContext currentClub, string? userId)
        {
            if (User.IsInRole("Admin") && currentClub.IsPlatformAdminMode)
            {
                return query;
            }

            if (currentClub.CurrentClubId.HasValue)
            {
                var clubId = currentClub.CurrentClubId.Value;
                return query.Where(m => m.FkBgdClub == clubId);
            }

            return query.Where(m => !string.IsNullOrWhiteSpace(userId) &&
                m.BoardGameMatchPlayers.Any(mp =>
                    !mp.Inactive &&
                    mp.FkBgdPlayer.HasValue &&
                    mp.FkBgdPlayerNavigation!.FkdboAspNetUsers == userId));
        }

        public sealed class DashboardStats
        {
            public int CompletedMatches { get; init; }
            public int ActivePlayers { get; init; }
            public int GamesPlayed { get; init; }
            public string TopGame { get; init; } = "None yet";
            public string TopWinner { get; init; } = "None yet";
        }

        public sealed class PersonalStats
        {
            public int TotalPlays { get; init; }
            public int Wins { get; init; }
            public int GamesPlayed { get; init; }
            public decimal WinRate { get; init; }
            public string MostPlayedGame { get; init; } = "None yet";
            public DateTime? LastPlayed { get; init; }
        }

        public sealed class ActivityEntry
        {
            public long Id { get; init; }
            public Guid Gid { get; init; }
            public string Name { get; init; } = string.Empty;
            public int Count { get; init; }
        }

        public sealed class RecentMatchRow
        {
            public long MatchId { get; init; }
            public string GameName { get; init; } = string.Empty;
            public Guid GameGid { get; init; }
            public DateTime? MatchDate { get; init; }
            public string Winners { get; init; } = string.Empty;
        }

        public sealed class MonthlyActivityRow
        {
            public int SortKey { get; init; }
            public string Label { get; init; } = string.Empty;
            public int Matches { get; init; }
        }

        public sealed class HighScoreEntry
        {
            public long GameId { get; set; }
            public Guid GameGid { get; set; }
            public Guid? PlayerGid { get; set; }
            public string GameName { get; set; } = "";
            public string PlayerName { get; set; } = "";
            public decimal Score { get; set; }
            public DateTime? Date { get; set; }
            public int Rank { get; set; }
        }
    }
}

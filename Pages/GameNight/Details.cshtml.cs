using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Board_Game_Software.Models;
using Board_Game_Software.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Board_Game_Software.Pages.GameNight
{
    public class DetailsModel : PageModel
    {
        private readonly BoardGameDbContext _db;
        private readonly GameNightService _nightService;
        private readonly BoardGamePlayabilityService _playabilityService;
        private readonly ICurrentClubService _currentClubService;

        public DetailsModel(
            BoardGameDbContext db,
            GameNightService nightService,
            BoardGamePlayabilityService playabilityService,
            ICurrentClubService currentClubService)
        {
            _db = db;
            _nightService = nightService;
            _playabilityService = playabilityService;
            _currentClubService = currentClubService;
        }

        public BoardGameNight Night { get; private set; } = null!;

        public List<MatchRow> Matches { get; private set; } = new();
        public List<PlayerRow> Players { get; private set; } = new();
        public List<PlayerRow> RankedPlayers { get; private set; } = new();

        public List<PlayerNightScore> NightScores { get; private set; } = new();
        public bool HasStandings { get; private set; }

        public List<GameSuggestion> Suggestions { get; set; } = new();
        public List<RivalryInsight> Rivalries { get; private set; } = new();
        public List<AchievementHighlight> Achievements { get; private set; } = new();
        public bool IsAdmin { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public sealed class MatchRow
        {
            public long MatchId { get; init; }
            public string GameName { get; init; } = string.Empty;
            public Guid GameGid { get; init; }
            public string CoverUrl { get; init; } = "/images/default-cover.png";
            public DateTime? StartTime { get; init; }
            public List<string> Winners { get; init; } = new();
            public bool IsComplete { get; init; }
        }

        public sealed class PlayerRow
        {
            public long PlayerId { get; init; }
            public Guid PlayerGid { get; init; }
            public string Name { get; init; } = string.Empty;
            public string AvatarUrl { get; init; } = "/images/default-avatar.png";

            public double Points { get; init; }
            public double BestGamePoints { get; init; }
            public int Firsts { get; init; }
            public int Seconds { get; init; }
            public int Thirds { get; init; }
        }

        public sealed class GameSuggestion
        {
            public long GameId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string? CoverUrl { get; set; }
            public string Reason { get; set; } = string.Empty;
            public string Detail { get; set; } = string.Empty;
            public string ShelfLocation { get; set; } = string.Empty;
            public double Score { get; set; }
            public string CategoryIcon { get; set; } = "bi-people";
            public int VoteCount { get; set; }
        }

        public sealed class RivalryInsight
        {
            public string Title { get; init; } = string.Empty;
            public string Detail { get; init; } = string.Empty;
            public string Icon { get; init; } = "bi-lightning-charge";
            public string AccentClass { get; init; } = "text-info";
        }

        public sealed class AchievementHighlight
        {
            public string Title { get; init; } = string.Empty;
            public string Detail { get; init; } = string.Empty;
            public string Icon { get; init; } = "bi-award";
            public string AccentClass { get; init; } = "text-warning";
        }

        public async Task<IActionResult> OnGetAsync(long id)
        {
            IsAdmin = User.IsInRole("Admin");

            var night = await _db.BoardGameNights.AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == id);

            if (night == null) return NotFound();

            var currentClub = await _currentClubService.GetCurrentClubAsync();
            if (!CanAccessNight(night, currentClub))
            {
                return Forbid();
            }

            Night = night;

            // standings
            NightScores = await _nightService.GetCurrentScores(id);
            HasStandings = NightScores.Any(); // only true if there are completed matches

            var scoreMap = NightScores.ToDictionary(x => x.PlayerId, x => x);

            // roster (players) with points if available
            Players = await _db.BoardGameNightPlayers.AsNoTracking()
                .Where(x => x.FkBgdBoardGameNight == id && !x.Inactive)
                .Select(x => x.FkBgdPlayerNavigation)
                .Select(p => new PlayerRow
                {
                    PlayerId = p.Id,
                    PlayerGid = p.Gid,
                    Name = (p.FirstName + " " + p.LastName).Trim(),
                    AvatarUrl = $"/media/player/{p.Gid}",

                    Points = scoreMap.ContainsKey(p.Id) ? scoreMap[p.Id].Points : 0,
                    BestGamePoints = scoreMap.ContainsKey(p.Id) ? scoreMap[p.Id].BestGamePoints : 0,
                    Firsts = scoreMap.ContainsKey(p.Id) ? scoreMap[p.Id].Firsts : 0,
                    Seconds = scoreMap.ContainsKey(p.Id) ? scoreMap[p.Id].Seconds : 0,
                    Thirds = scoreMap.ContainsKey(p.Id) ? scoreMap[p.Id].Thirds : 0
                })
                .ToListAsync();

            RankedPlayers = HasStandings
                ? Players
                    .OrderByDescending(p => p.Points)
                    .ThenByDescending(p => p.BestGamePoints)
                    .ThenByDescending(p => p.Firsts)
                    .ThenByDescending(p => p.Seconds)
                    .ThenByDescending(p => p.Thirds)
                    .ThenBy(p => p.Name)
                    .ToList()
                : Players.OrderBy(p => p.Name).ToList();

            var activePlayerIds = Players.Select(p => p.PlayerId).ToList();

            Suggestions = await GetSuggestionsInternal(id, activePlayerIds, Players.Count, null, night.FkBgdClub);
            Rivalries = await GetRivalriesInternal(activePlayerIds);

            // matches (fast version)
            var matchesCore = await _db.BoardGameNightBoardGameMatches.AsNoTracking()
                .Where(nm => nm.FkBgdBoardGameNight == id && !nm.Inactive)
                .Select(nm => nm.FkBgdBoardGameMatchNavigation)
                .Where(m => m != null)
                .Select(m => new
                {
                    MatchId = m!.Id,
                    StartTime = m.MatchDate,
                    IsComplete = m.MatchComplete == true,
                    GameName = m.FkBgdBoardGameNavigation!.BoardGameName,
                    GameGid = m.FkBgdBoardGameNavigation.Gid
                })
                .OrderBy(m => m.StartTime)
                .ToListAsync();

            var matchIds = matchesCore.Select(m => m.MatchId).ToList();

            var winners = await _db.BoardGameMatchPlayerResults.AsNoTracking()
                .Where(r => !r.Inactive && r.Win && matchIds.Contains(r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatch))
                .Select(r => new
                {
                    MatchId = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatch,
                    FirstName = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdPlayerNavigation!.FirstName
                })
                .ToListAsync();

            var winnersMap = winners
                .GroupBy(x => x.MatchId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.FirstName).Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n!).Distinct().ToList());

            Matches = matchesCore.Select(m => new MatchRow
            {
                MatchId = m.MatchId,
                GameName = m.GameName,
                GameGid = m.GameGid,
                CoverUrl = $"/media/boardgame/front/{m.GameGid}",
                StartTime = m.StartTime,
                Winners = winnersMap.GetValueOrDefault(m.MatchId) ?? new List<string>(),
                IsComplete = m.IsComplete
            }).ToList();

            Achievements = await GetAchievementsInternal(id, activePlayerIds);

            ViewData["NightId"] = id;
            return Page();
        }

        public async Task<IActionResult> OnGetShuffleIntel(long id, int? seed)
        {
            var night = await _db.BoardGameNights.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id);
            if (night == null) return NotFound();
            var currentClub = await _currentClubService.GetCurrentClubAsync();
            if (!CanAccessNight(night, currentClub)) return Forbid();

            var activePlayerIds = await _db.BoardGameNightPlayers.AsNoTracking()
                .Where(x => x.FkBgdBoardGameNight == id && !x.Inactive)
                .Select(x => x.FkBgdPlayer)
                .ToListAsync();

            var nightClubId = await _db.BoardGameNights.AsNoTracking()
                .Where(n => n.Id == id)
                .Select(n => n.FkBgdClub)
                .FirstOrDefaultAsync();

            var suggestions = await GetSuggestionsInternal(id, activePlayerIds, activePlayerIds.Count, seed, nightClubId);
            ViewData["NightId"] = id;
            return Partial("_GameIntelPartial", suggestions);
        }

        private async Task<List<GameSuggestion>> GetSuggestionsInternal(long nightId, List<long> activePlayerIds, int groupSize, int? seed, long? clubId)
        {
            if (!activePlayerIds.Any()) return new List<GameSuggestion>();

            var playedTonightIds = await _db.BoardGameNightBoardGameMatches.AsNoTracking()
                .Where(nm => nm.FkBgdBoardGameNight == nightId && !nm.Inactive)
                .Select(nm => nm.FkBgdBoardGameMatchNavigation.FkBgdBoardGame)
                .ToListAsync();

            var rand = new Random(seed ?? DateTime.Now.Millisecond);

            var topTenRows = await _db.PlayerBoardGames.AsNoTracking()
                .Where(pbg => pbg.FkBgdPlayer.HasValue
                    && pbg.FkBgdBoardGame.HasValue
                    && activePlayerIds.Contains(pbg.FkBgdPlayer.Value)
                    && !pbg.Inactive)
                .Select(pbg => new
                {
                    PlayerId = pbg.FkBgdPlayer!.Value,
                    GameId = pbg.FkBgdBoardGame!.Value,
                    pbg.Rank
                })
                .ToListAsync();

            var topTenByGame = topTenRows
                .GroupBy(x => x.GameId)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        Count = g.Select(x => x.PlayerId).Distinct().Count(),
                        BestRank = g.Min(x => x.Rank),
                        AvgRank = g.Average(x => x.Rank)
                    });

            var starRatings = await _db.PlayerBoardGameStarRatings.AsNoTracking()
                .Where(r => r.FkBgdBoardGame.HasValue && !r.Inactive)
                .GroupBy(r => r.FkBgdBoardGame!.Value)
                .Select(g => new { GameId = g.Key, Average = g.Average(x => x.StarRating), Count = g.Count() })
                .ToListAsync();

            var ratingByGame = starRatings.ToDictionary(x => x.GameId, x => x);

            var voteRows = await _db.BoardGameVotes.AsNoTracking()
                .Where(v => v.FkBgdBoardGameNight == nightId
                    && !v.Inactive
                    && activePlayerIds.Contains(v.FkBgdPlayer))
                .Select(v => new
                {
                    v.FkBgdBoardGame,
                    PlayerName = (v.FkBgdPlayerNavigation.FirstName + " " + v.FkBgdPlayerNavigation.LastName).Trim()
                })
                .ToListAsync();

            var votesByGame = voteRows
                .GroupBy(v => v.FkBgdBoardGame)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        Count = g.Count(),
                        Names = string.Join(", ", g.Select(v => v.PlayerName).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().Take(3))
                    });

            var groupHistory = await _db.BoardGameMatchPlayers.AsNoTracking()
                .Where(mp => activePlayerIds.Contains(mp.FkBgdPlayer)
                    && !mp.Inactive
                    && mp.FkBgdBoardGameMatchNavigation.MatchComplete == true
                    && !mp.FkBgdBoardGameMatchNavigation.Inactive)
                .Select(mp => new
                {
                    MatchId = mp.FkBgdBoardGameMatch,
                    GameId = mp.FkBgdBoardGameMatchNavigation.FkBgdBoardGame,
                    mp.FkBgdPlayer,
                    MatchDate = mp.FkBgdBoardGameMatchNavigation.MatchDate
                })
                .ToListAsync();

            var lastPlayedByGame = groupHistory
                .Where(x => x.MatchDate.HasValue)
                .GroupBy(x => x.GameId)
                .ToDictionary(g => g.Key, g => g.Max(x => x.MatchDate!.Value));

            var activePlayerGameCounts = groupHistory
                .GroupBy(x => x.GameId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.FkBgdPlayer).Distinct().Count());

            var playableGames = await _playabilityService.GetPlayableBaseGamesAsync(clubId);
            var playableCandidateIds = playableGames
                .Where(bg => !playedTonightIds.Contains(bg.Id)
                    && (!bg.MinPlayers.HasValue || bg.MinPlayers.Value <= groupSize)
                    && (!bg.MaxPlayers.HasValue || bg.MaxPlayers.Value >= groupSize))
                .Select(bg => bg.Id)
                .ToList();

            var candidates = await _db.BoardGames.AsNoTracking()
                .Where(bg => playableCandidateIds.Contains(bg.Id))
                .Select(bg => new
                {
                    bg.Id,
                    bg.BoardGameName,
                    bg.Gid,
                    bg.PlayingTimeMinInMinutes,
                    bg.PlayingTimeMaxInMinutes,
                    ShelfLocation = bg.BoardGameShelfSections
                        .Where(link => !link.Inactive)
                        .Select(link => link.FkBgdShelfSectionNavigation.FkBgdShelfNavigation.ShelfName + " / " + link.FkBgdShelfSectionNavigation.SectionName)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var scored = candidates.Select(game =>
                {
                    topTenByGame.TryGetValue(game.Id, out var topTen);
                    ratingByGame.TryGetValue(game.Id, out var rating);
                    votesByGame.TryGetValue(game.Id, out var votes);
                    lastPlayedByGame.TryGetValue(game.Id, out var lastPlayed);
                    activePlayerGameCounts.TryGetValue(game.Id, out var playersWhoPlayed);

                    var daysSincePlayed = lastPlayed == default ? (int?)null : (DateTime.Today - lastPlayed.Date).Days;
                    var newToGroup = playersWhoPlayed == 0;
                    var notPlayedRecentlyScore = daysSincePlayed switch
                    {
                        null => 18,
                        > 120 => 18,
                        > 45 => 10,
                        > 14 => 4,
                        _ => 0
                    };
                    var timeFitScore = game.PlayingTimeMaxInMinutes switch
                    {
                        null => 3,
                        <= 45 => 10,
                        <= 90 => 7,
                        <= 150 => 3,
                        _ => 0
                    };

                    var score =
                        (topTen?.Count ?? 0) * 28
                        + (votes?.Count ?? 0) * 34
                        + Math.Max(0, 11 - (topTen?.BestRank ?? 11)) * 2
                        + (double)(rating?.Average ?? 0m) * 7
                        + notPlayedRecentlyScore
                        + (newToGroup ? 16 : 0)
                        + timeFitScore
                        + (!string.IsNullOrWhiteSpace(game.ShelfLocation) ? 4 : 0)
                        + rand.NextDouble() * 3;

                    var reason = "Smart Pick";
                    var icon = "bi-stars text-info";
                    var details = new List<string>();

                    if ((topTen?.Count ?? 0) > 0)
                    {
                        reason = topTen!.Count > 1 ? $"{topTen.Count} Top 10 Lists" : "Top 10 Choice";
                        icon = "bi-star-fill text-info";
                        details.Add($"best rank #{topTen.BestRank}");
                    }

                    if ((votes?.Count ?? 0) > 0)
                    {
                        reason = votes!.Count > 1 ? $"{votes.Count} Queue Votes" : "Queue Vote";
                        icon = "bi-hand-thumbs-up-fill text-warning";
                        details.Add(string.IsNullOrWhiteSpace(votes.Names) ? "wishlist vote" : $"wanted by {votes.Names}");
                    }

                    if (newToGroup)
                    {
                        reason = (topTen?.Count ?? 0) > 0 ? reason : "New To Group";
                        icon = (topTen?.Count ?? 0) > 0 ? icon : "bi-compass text-success";
                        details.Add("new to this table");
                    }
                    else if (daysSincePlayed.HasValue)
                    {
                        details.Add(daysSincePlayed.Value == 0 ? "played today" : $"{daysSincePlayed.Value} days since table played");
                    }

                    if (rating?.Count > 0)
                    {
                        details.Add($"{rating.Average:0.0} avg rating");
                    }

                    if (!string.IsNullOrWhiteSpace(game.ShelfLocation))
                    {
                        details.Add(game.ShelfLocation);
                    }

                    return new GameSuggestion
                    {
                        GameId = game.Id,
                        Name = game.BoardGameName,
                        CoverUrl = $"/media/boardgame/front/{game.Gid}",
                        Reason = reason,
                        Detail = string.Join(" · ", details.Take(3)),
                        ShelfLocation = game.ShelfLocation ?? string.Empty,
                        Score = score,
                        CategoryIcon = icon,
                        VoteCount = votes?.Count ?? 0
                    };
                })
                .OrderByDescending(x => x.Score)
                .Take(4)
                .ToList();

            return scored;
        }

        private async Task<List<RivalryInsight>> GetRivalriesInternal(List<long> activePlayerIds)
        {
            if (activePlayerIds.Count < 2) return new List<RivalryInsight>();

            var playerNames = Players.ToDictionary(p => p.PlayerId, p => p.Name);

            var rows = await _db.BoardGameMatchPlayers.AsNoTracking()
                .Where(mp => activePlayerIds.Contains(mp.FkBgdPlayer)
                    && !mp.Inactive
                    && mp.FkBgdBoardGameMatchNavigation.MatchComplete == true
                    && !mp.FkBgdBoardGameMatchNavigation.Inactive)
                .Select(mp => new
                {
                    MatchId = mp.FkBgdBoardGameMatch,
                    GameName = mp.FkBgdBoardGameMatchNavigation.FkBgdBoardGameNavigation.BoardGameName,
                    MatchDate = mp.FkBgdBoardGameMatchNavigation.MatchDate,
                    PlayerId = mp.FkBgdPlayer,
                    Result = mp.BoardGameMatchPlayerResults
                        .Where(r => !r.Inactive)
                        .Select(r => new { r.Win, r.FinalTeam })
                        .FirstOrDefault()
                })
                .ToListAsync();

            var headToHead = new Dictionary<(long Winner, long Loser), int>();
            var pairGames = new Dictionary<(long A, long B), int>();
            var teamWins = new Dictionary<(long A, long B), int>();

            foreach (var match in rows.GroupBy(r => r.MatchId))
            {
                var players = match.ToList();
                var winners = players.Where(p => p.Result?.Win == true).Select(p => p.PlayerId).Distinct().ToList();
                var losers = players.Where(p => p.Result?.Win != true).Select(p => p.PlayerId).Distinct().ToList();

                for (var i = 0; i < players.Count; i++)
                {
                    for (var j = i + 1; j < players.Count; j++)
                    {
                        var a = Math.Min(players[i].PlayerId, players[j].PlayerId);
                        var b = Math.Max(players[i].PlayerId, players[j].PlayerId);
                        pairGames[(a, b)] = pairGames.GetValueOrDefault((a, b)) + 1;

                        if (players[i].Result?.Win == true &&
                            players[j].Result?.Win == true &&
                            players[i].Result?.FinalTeam.HasValue == true &&
                            players[i].Result?.FinalTeam == players[j].Result?.FinalTeam)
                        {
                            teamWins[(a, b)] = teamWins.GetValueOrDefault((a, b)) + 1;
                        }
                    }
                }

                foreach (var winnerId in winners)
                {
                    foreach (var loserId in losers)
                    {
                        if (winnerId == loserId) continue;
                        headToHead[(winnerId, loserId)] = headToHead.GetValueOrDefault((winnerId, loserId)) + 1;
                    }
                }
            }

            var insights = new List<RivalryInsight>();

            var biggestEdge = headToHead
                .Select(kvp => new
                {
                    kvp.Key.Winner,
                    kvp.Key.Loser,
                    Wins = kvp.Value,
                    Reverse = headToHead.GetValueOrDefault((kvp.Key.Loser, kvp.Key.Winner))
                })
                .Where(x => x.Wins + x.Reverse >= 3 && x.Wins > x.Reverse)
                .OrderByDescending(x => x.Wins - x.Reverse)
                .ThenByDescending(x => x.Wins)
                .FirstOrDefault();

            if (biggestEdge != null)
            {
                insights.Add(new RivalryInsight
                {
                    Title = "Nemesis Watch",
                    Detail = $"{NameFor(playerNames, biggestEdge.Winner)} leads {NameFor(playerNames, biggestEdge.Loser)} {biggestEdge.Wins}-{biggestEdge.Reverse}.",
                    Icon = "bi-lightning-charge-fill",
                    AccentClass = "text-warning"
                });
            }

            var mostPlayedPair = pairGames
                .OrderByDescending(x => x.Value)
                .FirstOrDefault();

            if (mostPlayedPair.Value > 0)
            {
                insights.Add(new RivalryInsight
                {
                    Title = "Most Familiar Duel",
                    Detail = $"{NameFor(playerNames, mostPlayedPair.Key.A)} and {NameFor(playerNames, mostPlayedPair.Key.B)} have shared {mostPlayedPair.Value} matches.",
                    Icon = "bi-people-fill",
                    AccentClass = "text-info"
                });
            }

            var bestTeam = teamWins
                .OrderByDescending(x => x.Value)
                .FirstOrDefault();

            if (bestTeam.Value > 0)
            {
                insights.Add(new RivalryInsight
                {
                    Title = "Best Team Pairing",
                    Detail = $"{NameFor(playerNames, bestTeam.Key.A)} + {NameFor(playerNames, bestTeam.Key.B)} have {bestTeam.Value} team wins.",
                    Icon = "bi-shield-check",
                    AccentClass = "text-success"
                });
            }

            return insights.Take(4).ToList();
        }

        private async Task<List<AchievementHighlight>> GetAchievementsInternal(long nightId, List<long> activePlayerIds)
        {
            var highlights = new List<AchievementHighlight>();
            if (!activePlayerIds.Any()) return highlights;

            var playerNames = Players.ToDictionary(p => p.PlayerId, p => p.Name);

            var tonightRows = await _db.BoardGameMatchPlayerResults.AsNoTracking()
                .Where(r => !r.Inactive
                    && r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.MatchComplete == true
                    && r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.BoardGameNightBoardGameMatches
                        .Any(link => link.FkBgdBoardGameNight == nightId && !link.Inactive))
                .Select(r => new
                {
                    PlayerId = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdPlayer,
                    GameId = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.FkBgdBoardGame,
                    GameName = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.FkBgdBoardGameNavigation.BoardGameName,
                    MatchId = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatch,
                    r.Win,
                    r.FinalScore,
                    r.RatingChangeMu
                })
                .ToListAsync();

            if (!tonightRows.Any()) return highlights;

            var tonightMatchIds = tonightRows.Select(x => x.MatchId).Distinct().ToList();

            var previousWins = await _db.BoardGameMatchPlayerResults.AsNoTracking()
                .Where(r => !r.Inactive
                    && r.Win
                    && activePlayerIds.Contains(r.FkBgdBoardGameMatchPlayerNavigation.FkBgdPlayer)
                    && !tonightMatchIds.Contains(r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatch))
                .Select(r => r.FkBgdBoardGameMatchPlayerNavigation.FkBgdPlayer)
                .Distinct()
                .ToListAsync();

            var firstWinner = tonightRows
                .Where(x => x.Win && !previousWins.Contains(x.PlayerId))
                .Select(x => x.PlayerId)
                .Distinct()
                .FirstOrDefault();

            if (firstWinner != 0)
            {
                highlights.Add(new AchievementHighlight
                {
                    Title = "First Win",
                    Detail = $"{NameFor(playerNames, firstWinner)} logged their first recorded win.",
                    Icon = "bi-award-fill",
                    AccentClass = "text-warning"
                });
            }

            var biggestRatingGain = tonightRows
                .Where(x => x.RatingChangeMu.HasValue && x.RatingChangeMu.Value > 0)
                .OrderByDescending(x => x.RatingChangeMu)
                .FirstOrDefault();

            if (biggestRatingGain != null)
            {
                highlights.Add(new AchievementHighlight
                {
                    Title = "Most Improved",
                    Detail = $"{NameFor(playerNames, biggestRatingGain.PlayerId)} gained {biggestRatingGain.RatingChangeMu:0.00} rating on {biggestRatingGain.GameName}.",
                    Icon = "bi-graph-up-arrow",
                    AccentClass = "text-success"
                });
            }

            var scoredTonight = tonightRows
                .Where(x => x.FinalScore.HasValue)
                .ToList();

            if (scoredTonight.Any())
            {
                var gameIds = scoredTonight.Select(x => x.GameId).Distinct().ToList();
                var previousHighs = await _db.BoardGameMatchPlayerResults.AsNoTracking()
                    .Where(r => !r.Inactive
                        && r.FinalScore.HasValue
                        && gameIds.Contains(r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.FkBgdBoardGame)
                        && !tonightMatchIds.Contains(r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatch))
                    .GroupBy(r => r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.FkBgdBoardGame)
                    .Select(g => new { GameId = g.Key, BestScore = g.Max(r => r.FinalScore!.Value) })
                    .ToListAsync();

                var previousHighByGame = previousHighs.ToDictionary(x => x.GameId, x => x.BestScore);
                var newRecord = scoredTonight
                    .Where(x => !previousHighByGame.TryGetValue(x.GameId, out var best) || x.FinalScore!.Value > best)
                    .OrderByDescending(x => x.FinalScore)
                    .FirstOrDefault();

                if (newRecord != null)
                {
                    highlights.Add(new AchievementHighlight
                    {
                        Title = "High Score Breaker",
                        Detail = $"{NameFor(playerNames, newRecord.PlayerId)} set {newRecord.FinalScore:0.##} in {newRecord.GameName}.",
                        Icon = "bi-bullseye",
                        AccentClass = "text-info"
                    });
                }
            }

            var streak = await GetBestWinStreak(activePlayerIds, playerNames);
            if (streak.Count >= 5)
            {
                highlights.Add(new AchievementHighlight
                {
                    Title = "Five-Win Streak",
                    Detail = $"{streak.PlayerName} is on a {streak.Count}-win streak.",
                    Icon = "bi-fire",
                    AccentClass = "text-danger"
                });
            }

            var shelfAchievement = await GetShelfCompletionAchievement(activePlayerIds, playerNames);
            if (shelfAchievement != null)
            {
                highlights.Add(shelfAchievement);
            }

            return highlights
                .GroupBy(x => x.Title + x.Detail)
                .Select(g => g.First())
                .Take(5)
                .ToList();
        }

        private async Task<(string PlayerName, int Count)> GetBestWinStreak(List<long> activePlayerIds, Dictionary<long, string> playerNames)
        {
            var results = await _db.BoardGameMatchPlayerResults.AsNoTracking()
                .Where(r => !r.Inactive && activePlayerIds.Contains(r.FkBgdBoardGameMatchPlayerNavigation.FkBgdPlayer))
                .Select(r => new
                {
                    PlayerId = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdPlayer,
                    MatchDate = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.MatchDate,
                    r.Win
                })
                .ToListAsync();

            var bestPlayer = string.Empty;
            var bestCount = 0;

            foreach (var group in results.GroupBy(x => x.PlayerId))
            {
                var streak = 0;
                foreach (var row in group.OrderByDescending(x => x.MatchDate ?? DateTime.MinValue))
                {
                    if (!row.Win) break;
                    streak++;
                }

                if (streak > bestCount)
                {
                    bestCount = streak;
                    bestPlayer = NameFor(playerNames, group.Key);
                }
            }

            return (bestPlayer, bestCount);
        }

        private async Task<AchievementHighlight?> GetShelfCompletionAchievement(List<long> activePlayerIds, Dictionary<long, string> playerNames)
        {
            var shelfGames = await _db.BoardGameShelfSections.AsNoTracking()
                .Where(link => !link.Inactive
                    && !link.FkBgdShelfSectionNavigation.Inactive
                    && !link.FkBgdShelfSectionNavigation.FkBgdShelfNavigation.Inactive)
                .Select(link => new
                {
                    GameId = link.FkBgdBoardGame,
                    ShelfName = link.FkBgdShelfSectionNavigation.FkBgdShelfNavigation.ShelfName
                })
                .ToListAsync();

            if (!shelfGames.Any()) return null;

            var playerGameWins = await _db.BoardGameMatchPlayerResults.AsNoTracking()
                .Where(r => !r.Inactive
                    && r.Win
                    && activePlayerIds.Contains(r.FkBgdBoardGameMatchPlayerNavigation.FkBgdPlayer))
                .Select(r => new
                {
                    PlayerId = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdPlayer,
                    GameId = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdBoardGameMatchNavigation.FkBgdBoardGame
                })
                .Distinct()
                .ToListAsync();

            foreach (var shelf in shelfGames.GroupBy(x => x.ShelfName).Where(g => g.Select(x => x.GameId).Distinct().Count() >= 2))
            {
                var shelfGameIds = shelf.Select(x => x.GameId).Distinct().ToHashSet();

                foreach (var player in playerGameWins.GroupBy(x => x.PlayerId))
                {
                    var wins = player.Select(x => x.GameId).Distinct().ToHashSet();
                    if (shelfGameIds.All(wins.Contains))
                    {
                        return new AchievementHighlight
                        {
                            Title = "Shelf Sweep",
                            Detail = $"{NameFor(playerNames, player.Key)} has won every game on {shelf.Key}.",
                            Icon = "bi-bookshelf",
                            AccentClass = "text-info"
                        };
                    }
                }
            }

            return null;
        }

        private static string NameFor(Dictionary<long, string> names, long playerId)
            => names.TryGetValue(playerId, out var name) && !string.IsNullOrWhiteSpace(name)
                ? name
                : $"Player {playerId}";

        public async Task<IActionResult> OnPostDeleteMatchAsync(long id, long matchId)
        {
            if (!User.IsInRole("Admin")) return Forbid();
            var night = await _db.BoardGameNights.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id);
            if (night == null) return NotFound();
            var currentClub = await _currentClubService.GetCurrentClubAsync();
            if (!CanAccessNight(night, currentClub)) return Forbid();

            var link = await _db.BoardGameNightBoardGameMatches.FirstOrDefaultAsync(x => x.FkBgdBoardGameNight == id && x.FkBgdBoardGameMatch == matchId);
            if (link != null)
            {
                _db.BoardGameNightBoardGameMatches.Remove(link);
                await _db.SaveChangesAsync();
            }
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostEndNightAsync(long id)
        {
            if (!User.IsInRole("Admin")) return Forbid();
            var night = await _db.BoardGameNights.FindAsync(id);
            if (night != null)
            {
                var currentClub = await _currentClubService.GetCurrentClubAsync();
                if (!CanAccessNight(night, currentClub)) return Forbid();
                night.Finished = true;
                await _db.SaveChangesAsync();
            }
            return RedirectToPage("Recap", new { id });
        }

        public async Task<IActionResult> OnPostDeleteNightAsync(long id)
        {
            if (!User.IsInRole("Admin")) return Forbid();

            var night = await _db.BoardGameNights.FindAsync(id);
            if (night == null) return RedirectToPage("Index");
            var currentClub = await _currentClubService.GetCurrentClubAsync();
            if (!CanAccessNight(night, currentClub)) return Forbid();

            var hasMatches = await _db.BoardGameNightBoardGameMatches
                .AnyAsync(x => x.FkBgdBoardGameNight == id && !x.Inactive);

            if (hasMatches)
            {
                ErrorMessage = "Delete the matches on this night before deleting the game night.";
                return RedirectToPage(new { id });
            }

            await using var transaction = await _db.Database.BeginTransactionAsync();

            var nightPlayers = await _db.BoardGameNightPlayers
                .Where(x => x.FkBgdBoardGameNight == id)
                .ToListAsync();

            var nightMatchLinks = await _db.BoardGameNightBoardGameMatches
                .Where(x => x.FkBgdBoardGameNight == id)
                .ToListAsync();

            var nightVotes = await _db.BoardGameVotes
                .Where(x => x.FkBgdBoardGameNight == id)
                .ToListAsync();

            var nightAchievements = await _db.PlayerAchievements
                .Where(x => x.FkBgdBoardGameNight == id)
                .ToListAsync();

            _db.BoardGameVotes.RemoveRange(nightVotes);
            _db.PlayerAchievements.RemoveRange(nightAchievements);
            _db.BoardGameNightPlayers.RemoveRange(nightPlayers);
            _db.BoardGameNightBoardGameMatches.RemoveRange(nightMatchLinks);
            _db.BoardGameNights.Remove(night);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return RedirectToPage("Index");
        }

        private bool CanAccessNight(BoardGameNight night, CurrentClubContext currentClub)
        {
            if (User.IsInRole("Admin") && currentClub.IsPlatformAdminMode)
            {
                return true;
            }

            return night.FkBgdClub.HasValue && night.FkBgdClub == currentClub.CurrentClubId;
        }
    }
}

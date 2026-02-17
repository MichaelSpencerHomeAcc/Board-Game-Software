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

        public DetailsModel(BoardGameDbContext db, GameNightService nightService)
        {
            _db = db;
            _nightService = nightService;
        }

        public BoardGameNight Night { get; private set; } = null!;

        public List<MatchRow> Matches { get; private set; } = new();
        public List<PlayerRow> Players { get; private set; } = new();
        public List<PlayerRow> RankedPlayers { get; private set; } = new();

        public List<PlayerNightScore> NightScores { get; private set; } = new();
        public bool HasStandings { get; private set; }

        public List<GameSuggestion> Suggestions { get; set; } = new();
        public bool IsAdmin { get; set; }

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
            public string CategoryIcon { get; set; } = "bi-people";
        }

        public async Task<IActionResult> OnGetAsync(long id)
        {
            IsAdmin = User.IsInRole("Admin");

            Night = await _db.BoardGameNights.AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == id);

            if (Night == null) return NotFound();

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

            Suggestions = await GetSuggestionsInternal(id, activePlayerIds, Players.Count, null);

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
                    GameName = m.FkBgdBoardGameNavigation.BoardGameName,
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
                    FirstName = r.FkBgdBoardGameMatchPlayerNavigation.FkBgdPlayerNavigation.FirstName
                })
                .ToListAsync();

            var winnersMap = winners
                .GroupBy(x => x.MatchId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.FirstName).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToList());

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

            ViewData["NightId"] = id;
            return Page();
        }

        public async Task<IActionResult> OnGetShuffleIntel(long id, int? seed)
        {
            var activePlayerIds = await _db.BoardGameNightPlayers.AsNoTracking()
                .Where(x => x.FkBgdBoardGameNight == id && !x.Inactive)
                .Select(x => x.FkBgdPlayer)
                .ToListAsync();

            var suggestions = await GetSuggestionsInternal(id, activePlayerIds, activePlayerIds.Count, seed);
            ViewData["NightId"] = id;
            return Partial("_GameIntelPartial", suggestions);
        }

        private async Task<List<GameSuggestion>> GetSuggestionsInternal(long nightId, List<long> activePlayerIds, int groupSize, int? seed)
        {
            if (!activePlayerIds.Any()) return new List<GameSuggestion>();

            var playedTonightIds = await _db.BoardGameNightBoardGameMatches.AsNoTracking()
                .Where(nm => nm.FkBgdBoardGameNight == nightId && !nm.Inactive)
                .Select(nm => nm.FkBgdBoardGameMatchNavigation.FkBgdBoardGame)
                .ToListAsync();

            var rand = new Random(seed ?? DateTime.Now.Millisecond);
            var combinedList = new List<(long Id, string Name, Guid Gid, string Reason, string Icon)>();

            // Crowd Favorite
            var favs = await _db.PlayerBoardGames.AsNoTracking()
                .Where(pbg => pbg.FkBgdPlayer.HasValue && activePlayerIds.Contains(pbg.FkBgdPlayer.Value) && !pbg.Inactive)
                .Where(pbg => pbg.BoardGame != null && pbg.BoardGame.PlayerCountMin <= groupSize && pbg.BoardGame.PlayerCountMax >= groupSize)
                .Where(pbg => pbg.FkBgdBoardGame.HasValue && !playedTonightIds.Contains(pbg.FkBgdBoardGame.Value))
                .Select(pbg => new { pbg.BoardGame!.Id, pbg.BoardGame.BoardGameName, pbg.BoardGame.Gid, pbg.Rank })
                .ToListAsync();

            var favorite = favs.OrderBy(x => x.Rank).ThenBy(_ => rand.Next()).FirstOrDefault();
            if (favorite != null) combinedList.Add((favorite.Id, favorite.BoardGameName, favorite.Gid, "Top 10 Choice", "bi-star-fill text-info"));

            // Competitive
            var usedIds = combinedList.Select(c => c.Id).Concat(playedTonightIds).ToList();
            var compCandidates = await _db.PlayerBoardGameRatings.AsNoTracking()
                .Where(r => activePlayerIds.Contains(r.FkBgdPlayer) && !r.Inactive)
                .Where(r => r.FkBgdBoardGameNavigation.PlayerCountMin <= groupSize && r.FkBgdBoardGameNavigation.PlayerCountMax >= groupSize)
                .Where(r => !usedIds.Contains(r.FkBgdBoardGame))
                .Select(r => new { r.FkBgdBoardGameNavigation.Id, r.FkBgdBoardGameNavigation.BoardGameName, r.FkBgdBoardGameNavigation.Gid, r.MatchesPlayed })
                .ToListAsync();

            var competitive = compCandidates.OrderByDescending(x => x.MatchesPlayed).ThenBy(_ => rand.Next()).FirstOrDefault();
            if (competitive != null) combinedList.Add((competitive.Id, competitive.BoardGameName, competitive.Gid, "Grudge Match", "bi-fire text-warning"));

            // Library picks
            usedIds = combinedList.Select(c => c.Id).Concat(playedTonightIds).ToList();
            var libraryCandidates = await _db.BoardGames.AsNoTracking()
                .Where(bg => bg.PlayerCountMin <= groupSize && bg.PlayerCountMax >= groupSize && !usedIds.Contains(bg.Id) && !bg.Inactive)
                .Select(bg => new { bg.Id, bg.BoardGameName, bg.Gid })
                .ToListAsync();

            foreach (var lp in libraryCandidates.OrderBy(_ => rand.Next()).Take(2))
                combinedList.Add((lp.Id, lp.BoardGameName, lp.Gid, "Library Pick", "bi-people-fill text-secondary"));

            return combinedList.Select(c => new GameSuggestion
            {
                GameId = c.Id,
                Name = c.Name,
                CoverUrl = $"/media/boardgame/front/{c.Gid}",
                Reason = c.Reason,
                CategoryIcon = c.Icon
            }).ToList();
        }

        public async Task<IActionResult> OnPostDeleteMatchAsync(long id, long matchId)
        {
            if (!User.IsInRole("Admin")) return Forbid();
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
            if (night != null) { night.Finished = true; await _db.SaveChangesAsync(); }
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostDeleteNightAsync(long id)
        {
            if (!User.IsInRole("Admin")) return Forbid();
            var night = await _db.BoardGameNights.FindAsync(id);
            if (night != null) { _db.BoardGameNights.Remove(night); await _db.SaveChangesAsync(); }
            return RedirectToPage("Index");
        }
    }
}

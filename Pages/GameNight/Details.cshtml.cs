using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Board_Game_Software.Models;
using Board_Game_Software.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

namespace Board_Game_Software.Pages.GameNight
{
    public class DetailsModel : PageModel
    {
        private readonly BoardGameDbContext _db;
        private readonly BoardGameImagesService _images;
        private readonly IMongoCollection<BoardGameImages> _imagesCollection;
        private readonly GameNightService _nightService;

        public DetailsModel(BoardGameDbContext db, BoardGameImagesService images, IMongoClient mongo, IConfiguration config, GameNightService nightService)
        {
            _db = db;
            _images = images;
            _nightService = nightService;
            var dbName = config["MongoDbSettings:Database"];
            _imagesCollection = mongo.GetDatabase(dbName).GetCollection<BoardGameImages>("BoardGameImages");
        }

        public BoardGameNight Night { get; private set; } = null!;
        public List<MatchRow> Matches { get; private set; } = new();
        public List<PlayerRow> Players { get; private set; } = new();
        public List<PlayerNightScore> NightScores { get; private set; } = new();
        public List<GameSuggestion> Suggestions { get; set; } = new();
        public bool IsAdmin { get; set; }

        public sealed class MatchRow
        {
            public long MatchId { get; init; }
            public string GameName { get; init; } = string.Empty;
            public Guid GameGid { get; init; }
            public string? CoverDataUrl { get; init; }
            public DateTime? StartTime { get; init; }
            public List<string> Winners { get; init; } = new();
            public bool IsComplete { get; init; }
        }

        public sealed class PlayerRow
        {
            public long PlayerId { get; init; }
            public Guid PlayerGid { get; init; }
            public string Name { get; init; } = string.Empty;
            public string? AvatarDataUrl { get; init; }
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

            Night = await _db.BoardGameNights
                .Include(n => n.BoardGameNightBoardGameMatches).ThenInclude(nm => nm.FkBgdBoardGameMatchNavigation).ThenInclude(m => m.FkBgdBoardGameNavigation)
                .Include(n => n.BoardGameNightBoardGameMatches).ThenInclude(nm => nm.FkBgdBoardGameMatchNavigation).ThenInclude(m => m.BoardGameMatchPlayers).ThenInclude(bmp => bmp.FkBgdPlayerNavigation)
                .Include(n => n.BoardGameNightBoardGameMatches).ThenInclude(nm => nm.FkBgdBoardGameMatchNavigation).ThenInclude(m => m.BoardGameMatchPlayers).ThenInclude(bmp => bmp.BoardGameMatchPlayerResults)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (Night == null) return NotFound();

            NightScores = await _nightService.GetCurrentScores(id);

            var roster = await _db.BoardGameNightPlayers.AsNoTracking()
                .Where(x => x.FkBgdBoardGameNight == id && !x.Inactive)
                .Select(x => x.FkBgdPlayerNavigation).ToListAsync();

            var activePlayerIds = roster.Select(p => p.Id).ToList(); // long
            var playerGids = roster.Select(p => (Guid?)p.Gid).ToArray();

            var docs = await _imagesCollection.Find(Builders<BoardGameImages>.Filter.In(x => x.GID, playerGids)).ToListAsync();
            var avatarMap = docs.ToDictionary(d => d.GID!.Value, d => d.ImageBytes != null
                ? $"data:{d.ContentType};base64,{Convert.ToBase64String(d.ImageBytes)}" : null);

            Players = roster.Select(p => new PlayerRow
            {
                PlayerId = p.Id,
                PlayerGid = p.Gid,
                Name = $"{p.FirstName} {p.LastName}".Trim(),
                AvatarDataUrl = avatarMap.GetValueOrDefault(p.Gid)
            }).OrderBy(p => p.Name).ToList();

            Suggestions = await GetSuggestionsInternal(id, activePlayerIds, Players.Count, null);

            var frontTypeGid = await _db.BoardGameImageTypes.AsNoTracking().Where(t => t.TypeDesc == "Board Game Front").Select(t => t.Gid).FirstOrDefaultAsync();
            var bgList = Night.BoardGameNightBoardGameMatches.Select(nm => nm.FkBgdBoardGameMatchNavigation?.FkBgdBoardGameNavigation).Where(bg => bg != null).Distinct().ToList();
            var coverMap = frontTypeGid == Guid.Empty ? new Dictionary<Guid, string?>() : await _images.GetFrontImagesAsync(bgList.Select(bg => bg!.Gid), frontTypeGid);

            Matches = Night.BoardGameNightBoardGameMatches
                .Select(nm => {
                    var match = nm.FkBgdBoardGameMatchNavigation!;
                    var bg = match.FkBgdBoardGameNavigation!;
                    var winners = match.BoardGameMatchPlayers.SelectMany(p => p.BoardGameMatchPlayerResults).Where(r => r.Win && !r.Inactive)
                        .Select(r => match.BoardGameMatchPlayers.FirstOrDefault(p => p.Id == r.FkBgdBoardGameMatchPlayer)?.FkBgdPlayerNavigation)
                        .Where(p => p != null).Select(p => p!.FirstName).ToList();

                    return new MatchRow
                    {
                        MatchId = match.Id,
                        GameName = bg.BoardGameName,
                        GameGid = bg.Gid,
                        CoverDataUrl = coverMap.GetValueOrDefault(bg.Gid),
                        StartTime = match.MatchDate,
                        Winners = winners,
                        IsComplete = match.MatchComplete == true
                    };
                }).OrderBy(m => m.StartTime).ToList();

            ViewData["NightId"] = id;
            return Page();
        }

        public async Task<IActionResult> OnGetShuffleIntel(long id, int? seed)
        {
            var activePlayerIds = await _db.BoardGameNightPlayers.AsNoTracking()
                .Where(x => x.FkBgdBoardGameNight == id && !x.Inactive)
                .Select(x => x.FkBgdPlayer).ToListAsync();

            var suggestions = await GetSuggestionsInternal(id, activePlayerIds, activePlayerIds.Count, seed);
            ViewData["NightId"] = id;
            return Partial("_GameIntelPartial", suggestions);
        }

        private async Task<List<GameSuggestion>> GetSuggestionsInternal(long nightId, List<long> activePlayerIds, int groupSize, int? seed)
        {
            if (!activePlayerIds.Any()) return new List<GameSuggestion>();

            // Get standard long list of played games
            var playedTonightIds = await _db.BoardGameNightBoardGameMatches
                .Where(nm => nm.FkBgdBoardGameNight == nightId)
                .Select(nm => nm.FkBgdBoardGameMatchNavigation.FkBgdBoardGame)
                .ToListAsync();

            var rand = new Random(seed ?? DateTime.Now.Millisecond);
            var combinedList = new List<(long Id, string Name, Guid Gid, string Reason, string Icon)>();
            var frontTypeGid = await _db.BoardGameImageTypes.AsNoTracking().Where(t => t.TypeDesc == "Board Game Front").Select(t => t.Gid).FirstOrDefaultAsync();

            // 1. Crowd Favorite (PlayerBoardGame - Rank based)
            var favs = await _db.PlayerBoardGames
                .Where(pbg => pbg.FkBgdPlayer.HasValue && activePlayerIds.Contains(pbg.FkBgdPlayer.Value) && !pbg.Inactive)
                .Where(pbg => pbg.BoardGame != null && pbg.BoardGame.PlayerCountMin <= groupSize && pbg.BoardGame.PlayerCountMax >= groupSize)
                .Where(pbg => pbg.FkBgdBoardGame.HasValue && !playedTonightIds.Contains(pbg.FkBgdBoardGame.Value))
                .Select(pbg => new { pbg.BoardGame!.Id, pbg.BoardGame.BoardGameName, pbg.BoardGame.Gid, pbg.Rank })
                .ToListAsync();

            var favorite = favs.OrderBy(x => x.Rank).ThenBy(_ => rand.Next()).FirstOrDefault();
            if (favorite != null) combinedList.Add((favorite.Id, favorite.BoardGameName, favorite.Gid, "Top 10 Choice", "bi-star-fill text-info"));

            // 2. Competitive (Grudge Match based on Ratings)
            var usedIds = combinedList.Select(c => c.Id).Concat(playedTonightIds).ToList();
            var compCandidates = await _db.PlayerBoardGameRatings
                .Where(r => activePlayerIds.Contains(r.FkBgdPlayer) && !r.Inactive)
                .Where(r => r.FkBgdBoardGameNavigation.PlayerCountMin <= groupSize && r.FkBgdBoardGameNavigation.PlayerCountMax >= groupSize)
                .Where(r => !usedIds.Contains(r.FkBgdBoardGame))
                .Select(r => new { r.FkBgdBoardGameNavigation.Id, r.FkBgdBoardGameNavigation.BoardGameName, r.FkBgdBoardGameNavigation.Gid, r.MatchesPlayed })
                .ToListAsync();

            var competitive = compCandidates.OrderByDescending(x => x.MatchesPlayed).ThenBy(_ => rand.Next()).FirstOrDefault();
            if (competitive != null) combinedList.Add((competitive.Id, competitive.BoardGameName, competitive.Gid, "Grudge Match", "bi-fire text-warning"));

            // 3 & 4. Wildcards (Library fitting count)
            usedIds = combinedList.Select(c => c.Id).Concat(playedTonightIds).ToList();
            var libraryCandidates = await _db.BoardGames
                .Where(bg => bg.PlayerCountMin <= groupSize && bg.PlayerCountMax >= groupSize && !usedIds.Contains(bg.Id) && !bg.Inactive)
                .Select(bg => new { bg.Id, bg.BoardGameName, bg.Gid })
                .ToListAsync();

            foreach (var lp in libraryCandidates.OrderBy(_ => rand.Next()).Take(2))
                combinedList.Add((lp.Id, lp.BoardGameName, lp.Gid, "Library Pick", "bi-people-fill text-secondary"));

            var suggestionImages = await _images.GetFrontImagesAsync(combinedList.Select(c => c.Gid), frontTypeGid);
            return combinedList.Select(c => new GameSuggestion
            {
                GameId = c.Id,
                Name = c.Name,
                CoverUrl = suggestionImages.GetValueOrDefault(c.Gid),
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
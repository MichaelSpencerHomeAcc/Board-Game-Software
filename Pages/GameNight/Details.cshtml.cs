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

        public sealed class MatchRow
        {
            public long MatchId { get; init; }
            public string GameName { get; init; } = string.Empty;
            public Guid GameGid { get; init; }
            public string? CoverDataUrl { get; init; }
            public DateTime? StartTime { get; init; }
            public DateTime? EndTime { get; init; }
            public List<string> Winners { get; init; } = new();
            public decimal? HighScore { get; init; }
            public bool IsComplete { get; init; }
        }

        public sealed class PlayerRow
        {
            public long PlayerId { get; init; }
            public Guid PlayerGid { get; init; }
            public string Name { get; init; } = string.Empty;
            public string? AvatarDataUrl { get; init; }
        }

        public async Task<IActionResult> OnGetAsync(long id)
        {
            Night = await _db.BoardGameNights
                .Include(n => n.BoardGameNightBoardGameMatches)
                    .ThenInclude(nm => nm.FkBgdBoardGameMatchNavigation)
                        .ThenInclude(m => m.FkBgdBoardGameNavigation)
                .Include(n => n.BoardGameNightBoardGameMatches)
                    .ThenInclude(nm => nm.FkBgdBoardGameMatchNavigation)
                        .ThenInclude(m => m.BoardGameMatchPlayers)
                            .ThenInclude(bmp => bmp.FkBgdPlayerNavigation)
                .Include(n => n.BoardGameNightBoardGameMatches)
                    .ThenInclude(nm => nm.FkBgdBoardGameMatchNavigation)
                        .ThenInclude(m => m.BoardGameMatchPlayers)
                            .ThenInclude(bmp => bmp.BoardGameMatchPlayerResults)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (Night == null) return NotFound();

            // 1. Get Base Scores
            NightScores = await _nightService.GetCurrentScores(id);

            // 2. Fetch Roster & Avatars
            var roster = await _db.BoardGameNightPlayers.AsNoTracking()
                .Where(x => x.FkBgdBoardGameNight == id && !x.Inactive)
                .Select(x => x.FkBgdPlayerNavigation).ToListAsync();

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

            // 3. Attach Avatars to Scores
            foreach (var score in NightScores)
            {
                score.AvatarUrl = Players.FirstOrDefault(p => p.Name == score.PlayerName)?.AvatarDataUrl;
            }

            // 4. Fetch Match Rows (existing logic)
            var bgList = Night.BoardGameNightBoardGameMatches
                .Select(nm => nm.FkBgdBoardGameMatchNavigation?.FkBgdBoardGameNavigation)
                .Where(bg => bg != null).Distinct().ToList();

            var frontTypeGid = await _db.BoardGameImageTypes.AsNoTracking()
                .Where(t => t.TypeDesc == "Board Game Front").Select(t => t.Gid).FirstOrDefaultAsync();

            var coverMap = frontTypeGid == Guid.Empty ? new Dictionary<Guid, string?>()
                : await _images.GetFrontImagesAsync(bgList.Select(bg => bg!.Gid), frontTypeGid);

            Matches = Night.BoardGameNightBoardGameMatches
                .Select(nm =>
                {
                    var match = nm.FkBgdBoardGameMatchNavigation!;
                    var bg = match.FkBgdBoardGameNavigation!;
                    var winners = match.BoardGameMatchPlayers
                        .SelectMany(p => p.BoardGameMatchPlayerResults)
                        .Where(r => r.Win && !r.Inactive)
                        .Select(r => match.BoardGameMatchPlayers.FirstOrDefault(p => p.Id == r.FkBgdBoardGameMatchPlayer)?.FkBgdPlayerNavigation)
                        .Where(p => p != null).Select(p => $"{p!.FirstName} {p.LastName}".Trim()).ToList();

                    return new MatchRow
                    {
                        MatchId = match.Id,
                        GameName = bg.BoardGameName,
                        GameGid = bg.Gid,
                        CoverDataUrl = coverMap.GetValueOrDefault(bg.Gid),
                        StartTime = match.MatchDate,
                        EndTime = match.FinishedDate,
                        Winners = winners,
                        HighScore = match.BoardGameMatchPlayers.SelectMany(p => p.BoardGameMatchPlayerResults).Where(r => r.Win).FirstOrDefault()?.FinalScore,
                        IsComplete = match.MatchComplete == true
                    };
                }).OrderBy(m => m.StartTime).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostEndNightAsync(long id)
        {
            var night = await _db.BoardGameNights.FindAsync(id);
            if (night == null) return NotFound();
            night.Finished = true;
            night.TimeModified = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostDeleteMatchAsync(long id, long matchId)
        {
            var match = await _db.BoardGameMatches.FindAsync(matchId);
            if (match == null || match.MatchComplete == true) return Forbid();
            var link = await _db.BoardGameNightBoardGameMatches.FirstOrDefaultAsync(x => x.FkBgdBoardGameNight == id && x.FkBgdBoardGameMatch == matchId);
            if (link == null) return NotFound();
            _db.BoardGameNightBoardGameMatches.Remove(link);
            await _db.SaveChangesAsync();
            return RedirectToPage(new { id });
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Board_Game_Software.Services;

namespace Board_Game_Software.Pages.Match
{
    public class ResultsModel : PageModel
    {
        private readonly BoardGameDbContext _db;
        private readonly IMongoCollection<BoardGameImages> _imagesCollection;
        private readonly BoardGameImagesService _imagesService;
        private readonly RatingService _ratingService;

        public ResultsModel(BoardGameDbContext db, IMongoClient mongo, IConfiguration config, BoardGameImagesService imagesService, RatingService ratingService)
        {
            _db = db;
            _imagesService = imagesService;
            _ratingService = ratingService;
            var dbName = config["MongoDbSettings:Database"];
            _imagesCollection = mongo.GetDatabase(dbName).GetCollection<BoardGameImages>("BoardGameImages");
        }

        public string MatchGameName { get; private set; } = string.Empty;
        public string? GameBannerUrl { get; private set; }
        public DateTime? MatchDate { get; private set; }
        public bool ShowPoints { get; private set; }
        public bool ShowTeams { get; private set; }
        public bool IsTeamVictoryGame { get; private set; }
        public bool? MatchComplete { get; private set; }
        public long NightId { get; private set; }

        [BindProperty] public InputModel Input { get; set; } = new();

        public List<ResultTypeRow> ResultTypes { get; private set; } = new();
        public List<PlayerRow> Players { get; private set; } = new();

        public sealed class ResultTypeRow
        {
            public long Id { get; init; }
            public string Name { get; init; } = string.Empty;
            public bool IsVictory { get; init; }
            public bool IsDefeat { get; init; }
        }

        public sealed class PlayerRow
        {
            public long MatchPlayerId { get; init; }
            public string PlayerName { get; init; } = string.Empty;
            public long? ExistingResultTypeId { get; init; }
            public decimal? ExistingScore { get; init; }
            public bool ExistingIsWinner { get; init; }
            public FinalTeam? ExistingFinalTeam { get; init; }
            public string? MarkerTypeName { get; init; }
            public string? MarkerImageDataUrl { get; set; }
            public string? PlayerAvatarUrl { get; set; }
            public Guid? MarkerTypeGid { get; init; }
            public Guid? PlayerGid { get; init; }
        }

        public sealed class InputModel
        {
            public long MatchId { get; set; }
            public long NightId { get; set; }
            public DateTime? FinishedDate { get; set; }
            public List<PlayerResultInput> PlayerResults { get; set; } = new();
        }

        public sealed class PlayerResultInput
        {
            public long MatchPlayerId { get; set; }
            public long? ResultTypeId { get; set; }
            public decimal? Score { get; set; }
            public bool IsWinner { get; set; }
            public FinalTeam? FinalTeam { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(long id)
        {
            var match = await _db.BoardGameMatches
                .Include(m => m.FkBgdBoardGameNavigation)
                    .ThenInclude(bg => bg.FkBgdBoardGameVictoryConditionTypeNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (match == null) return NotFound();

            var game = match.FkBgdBoardGameNavigation;
            ShowPoints = game?.FkBgdBoardGameVictoryConditionTypeNavigation?.Points ?? false;
            IsTeamVictoryGame = game?.FkBgdBoardGameVictoryConditionTypeNavigation?.TypeDesc == "Team Victory";
            MatchComplete = match.MatchComplete;

            var bannerTypeId = await _db.BoardGameImageTypes.Where(t => t.TypeDesc == "Board Game Front").Select(t => t.Gid).FirstOrDefaultAsync();
            if (game != null)
            {
                var bannerMap = await _imagesService.GetFrontImagesAsync(new[] { game.Gid }, bannerTypeId);
                GameBannerUrl = bannerMap.GetValueOrDefault(game.Gid);
            }

            var link = await _db.BoardGameNightBoardGameMatches
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.FkBgdBoardGameMatch == id && !x.Inactive);

            NightId = link?.FkBgdBoardGameNight ?? 0;
            MatchGameName = game?.BoardGameName ?? "Match";
            MatchDate = match.MatchDate;

            ResultTypes = await _db.ResultTypes.AsNoTracking()
                .Select(r => new ResultTypeRow
                {
                    Id = r.Id,
                    Name = r.TypeDesc,
                    IsVictory = r.IsVictory == true,
                    IsDefeat = r.IsDefeat == true
                }).ToListAsync();

            var matchPlayers = await _db.BoardGameMatchPlayers
                .AsNoTracking()
                .Include(mp => mp.FkBgdPlayerNavigation)
                .Include(mp => mp.FkBgdBoardGameMarkerNavigation)
                    .ThenInclude(mk => mk.FkBgdBoardGameMarkerTypeNavigation)
                .Where(mp => mp.FkBgdBoardGameMatch == id && !mp.Inactive)
                .ToListAsync();

            ShowTeams = matchPlayers.Count > 0 && matchPlayers.All(mp =>
                mp.FkBgdBoardGameMarkerNavigation?.FkBgdBoardGameMarkerTypeNavigation?.FkBgdMarkerAlignmentType != null);

            var mpIds = matchPlayers.Select(mp => mp.Id).ToList();
            var existingResults = await _db.BoardGameMatchPlayerResults
                .AsNoTracking()
                .Where(r => !r.Inactive && mpIds.Contains(r.FkBgdBoardGameMatchPlayer))
                .ToListAsync();
            var existingByMp = existingResults.ToDictionary(r => r.FkBgdBoardGameMatchPlayer, r => r);

            var rows = new List<PlayerRow>();
            var wantGids = new HashSet<Guid>();

            foreach (var mp in matchPlayers.OrderBy(m => m.FkBgdPlayerNavigation!.FirstName))
            {
                existingByMp.TryGetValue(mp.Id, out var res);
                var markerType = mp.FkBgdBoardGameMarkerNavigation?.FkBgdBoardGameMarkerTypeNavigation;

                var row = new PlayerRow
                {
                    MatchPlayerId = mp.Id,
                    PlayerName = $"{mp.FkBgdPlayerNavigation?.FirstName} {mp.FkBgdPlayerNavigation?.LastName}",
                    ExistingResultTypeId = res?.FkBgdResultType,
                    ExistingScore = res?.FinalScore,
                    // TREATING AS BOOL
                    ExistingIsWinner = res?.Win ?? false,
                    ExistingFinalTeam = res?.FinalTeam,
                    MarkerTypeName = markerType?.TypeDesc,
                    MarkerTypeGid = markerType?.Gid,
                    PlayerGid = mp.FkBgdPlayerNavigation?.Gid
                };
                if (row.MarkerTypeGid.HasValue) wantGids.Add(row.MarkerTypeGid.Value);
                if (row.PlayerGid.HasValue) wantGids.Add(row.PlayerGid.Value);
                rows.Add(row);
            }

            if (wantGids.Any())
            {
                var images = await _imagesCollection.Find(Builders<BoardGameImages>.Filter.In(x => x.GID, wantGids.Cast<Guid?>())).ToListAsync();
                var byGid = images.ToDictionary(d => d.GID!.Value, d => $"data:{d.ContentType};base64,{Convert.ToBase64String(d.ImageBytes)}");

                foreach (var r in rows)
                {
                    if (r.MarkerTypeGid.HasValue && byGid.TryGetValue(r.MarkerTypeGid.Value, out var mUrl)) r.MarkerImageDataUrl = mUrl;
                    if (r.PlayerGid.HasValue && byGid.TryGetValue(r.PlayerGid.Value, out var pUrl)) r.PlayerAvatarUrl = pUrl;
                }
            }

            Players = rows;
            Input = new InputModel
            {
                MatchId = id,
                NightId = NightId,
                FinishedDate = match.FinishedDate ?? match.MatchDate,
                PlayerResults = Players.Select(p => new PlayerResultInput
                {
                    MatchPlayerId = p.MatchPlayerId,
                    ResultTypeId = p.ExistingResultTypeId ?? ResultTypes.FirstOrDefault()?.Id,
                    Score = p.ExistingScore,
                    IsWinner = p.ExistingIsWinner,
                    FinalTeam = p.ExistingFinalTeam
                }).ToList()
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var match = await _db.BoardGameMatches.FindAsync(Input.MatchId);
            if (match == null || match.MatchComplete == true) return RedirectToPage(new { id = Input.MatchId });

            if (!ModelState.IsValid) return await OnGetAsync(Input.MatchId);

            match.FinishedDate = Input.FinishedDate ?? match.MatchDate;
            match.TimeModified = DateTime.UtcNow;

            foreach (var row in Input.PlayerResults)
            {
                var existing = await _db.BoardGameMatchPlayerResults.FirstOrDefaultAsync(r => r.FkBgdBoardGameMatchPlayer == row.MatchPlayerId);
                if (existing != null)
                {
                    existing.FkBgdResultType = row.ResultTypeId ?? 1;
                    existing.FinalScore = row.Score;
                    // SAVING AS BOOL
                    existing.Win = row.IsWinner;
                    existing.FinalTeam = row.FinalTeam;
                    existing.TimeModified = DateTime.UtcNow;
                }
                else
                {
                    _db.BoardGameMatchPlayerResults.Add(new BoardGameMatchPlayerResult
                    {
                        Gid = Guid.NewGuid(),
                        FkBgdBoardGameMatchPlayer = row.MatchPlayerId,
                        FkBgdResultType = row.ResultTypeId ?? 1,
                        FinalScore = row.Score,
                        Win = row.IsWinner,
                        FinalTeam = row.FinalTeam,
                        TimeCreated = DateTime.UtcNow,
                        TimeModified = DateTime.UtcNow,
                        CreatedBy = User.Identity?.Name ?? "system",
                        ModifiedBy = User.Identity?.Name ?? "system"
                    });
                }
            }
            await _db.SaveChangesAsync();
            return RedirectToPage("/GameNight/Details", new { id = Input.NightId });
        }

        public async Task<IActionResult> OnPostCompleteAsync()
        {
            var match = await _db.BoardGameMatches.FindAsync(Input.MatchId);
            if (match == null || match.MatchComplete == true) return NotFound();

            await OnPostAsync();
            await _ratingService.CalculateAndApplyResults(match.Id);

            match.MatchComplete = true;
            match.TimeModified = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return RedirectToPage("/GameNight/Details", new { id = Input.NightId });
        }

        public async Task<IActionResult> OnPostUnlockAsync()
        {
            if (!User.IsInRole("Admin")) return Forbid();

            var match = await _db.BoardGameMatches
                .Include(m => m.BoardGameMatchPlayers)
                    .ThenInclude(mp => mp.BoardGameMatchPlayerResults)
                .FirstOrDefaultAsync(m => m.Id == Input.MatchId);

            if (match == null) return NotFound();

            foreach (var mp in match.BoardGameMatchPlayers)
            {
                foreach (var res in mp.BoardGameMatchPlayerResults.Where(r => !r.Inactive))
                {
                    if (res.RatingChangeMu.HasValue)
                    {
                        var rating = await _db.PlayerBoardGameRatings
                            .FirstOrDefaultAsync(r => r.FkBgdPlayer == mp.FkBgdPlayer && r.FkBgdBoardGame == match.FkBgdBoardGame);

                        if (rating != null)
                        {
                            rating.RatingMu -= res.RatingChangeMu.Value;
                            rating.RatingSigma -= res.RatingChangeSigma ?? 0;
                            rating.MatchesPlayed--;
                        }
                        res.RatingChangeMu = null;
                        res.RatingChangeSigma = null;
                        res.PreMatchRatingMu = null;
                        res.PreMatchRatingSigma = null;
                    }
                }
            }

            match.MatchComplete = false;
            match.TimeModified = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return RedirectToPage(new { id = Input.MatchId });
        }
    }
}
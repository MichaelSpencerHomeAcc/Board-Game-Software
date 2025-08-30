using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace Board_Game_Software.Pages.Match
{
    public class ResultsModel : PageModel
    {
        private readonly BoardGameDbContext _db;
        private readonly IMongoCollection<BoardGameImages> _imagesCollection;

        public ResultsModel(BoardGameDbContext db, IMongoClient mongo, IConfiguration config)
        {
            _db = db;
            var dbName = config["MongoDbSettings:Database"];
            var database = mongo.GetDatabase(dbName);
            _imagesCollection = database.GetCollection<BoardGameImages>("BoardGameImages");
        }

        // Display
        public long MatchId { get; private set; }
        public string MatchGameName { get; private set; } = string.Empty;
        public DateOnly? MatchDate { get; private set; }
        public long NightId { get; private set; }

        public List<ResultTypeRow> ResultTypes { get; private set; } = new();
        public List<PlayerRow> Players { get; private set; } = new();

        // Bind
        [BindProperty] public InputModel Input { get; set; } = new();

        public sealed class ResultTypeRow
        {
            public long Id { get; init; }
            public string Name { get; init; } = string.Empty;
        }

        public sealed class PlayerRow
        {
            public long MatchPlayerId { get; init; }
            public long PlayerId { get; init; }
            public string PlayerName { get; init; } = string.Empty;

            public long? ExistingResultTypeId { get; init; }
            public decimal? ExistingScore { get; init; }
            public bool ExistingIsWinner { get; init; }
            public FinalTeam? ExistingFinalTeam { get; init; }

            // Marker (from MATCH-PLAYER → Marker → MarkerType)
            public Guid? MarkerTypeGid { get; init; }
            public string? MarkerTypeName { get; init; }
            public string? MarkerImageDataUrl { get; set; }  // base64 data URL
        }

        public sealed class InputModel
        {
            public long MatchId { get; set; }
            public long NightId { get; set; }
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
            // Match header
            var match = await _db.BoardGameMatches
                .Include(m => m.FkBgdBoardGameNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (match == null) return NotFound();

            var link = await _db.BoardGameNightBoardGameMatches
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.FkBgdBoardGameMatch == id && !x.Inactive);

            NightId = link?.FkBgdBoardGameNight ?? 0;
            MatchId = id;
            MatchGameName = match.FkBgdBoardGameNavigation?.BoardGameName ?? "Match";
            MatchDate = match.MatchDate;

            // Result types
            ResultTypes = await _db.ResultTypes
                .AsNoTracking()
                .Select(r => new ResultTypeRow { Id = r.Id, Name = r.TypeDesc ?? r.ToString() })
                .ToListAsync();

            // Players with marker & marker type (strictly via MATCH-PLAYER)
            var matchPlayers = await _db.BoardGameMatchPlayers
                .AsNoTracking()
                .Include(mp => mp.FkBgdPlayerNavigation)
                .Include(mp => mp.FkBgdBoardGameMarkerNavigation)
                    .ThenInclude(mk => mk.FkBgdBoardGameMarkerTypeNavigation)
                .Where(mp => mp.FkBgdBoardGameMatch == id && !mp.Inactive)
                .ToListAsync();

            var mpIds = matchPlayers.Select(mp => mp.Id).ToList();

            // Existing results
            var existingResults = await _db.BoardGameMatchPlayerResults
                .AsNoTracking()
                .Where(r => !r.Inactive && mpIds.Contains(r.FkBgdBoardGameMatchPlayer))
                .ToListAsync();
            var existingByMp = existingResults.ToDictionary(r => r.FkBgdBoardGameMatchPlayer, r => r);

            // Build rows and collect marker GIDs for one Mongo fetch
            var rows = new List<PlayerRow>();
            var wantGids = new HashSet<Guid>();

            foreach (var mp in matchPlayers
                     .OrderBy(m => m.FkBgdPlayerNavigation!.FirstName)
                     .ThenBy(m => m.FkBgdPlayerNavigation!.LastName))
            {
                existingByMp.TryGetValue(mp.Id, out var res);

                var markerType = mp.FkBgdBoardGameMarkerNavigation?.FkBgdBoardGameMarkerTypeNavigation;
                var gid = markerType?.Gid ?? Guid.Empty;

                var row = new PlayerRow
                {
                    MatchPlayerId = mp.Id,
                    PlayerId = mp.FkBgdPlayer,
                    PlayerName = string.Join(" ", new[]
                    {
                        mp.FkBgdPlayerNavigation?.FirstName,
                        mp.FkBgdPlayerNavigation?.LastName
                    }.Where(s => !string.IsNullOrWhiteSpace(s))),

                    ExistingResultTypeId = res?.FkBgdResultType,
                    ExistingScore = res?.FinalScore,
                    ExistingIsWinner = res?.Win ?? false,
                    ExistingFinalTeam = res?.FinalTeam,

                    // These two drive the UI you asked for
                    MarkerTypeName = markerType?.TypeDesc,
                    MarkerTypeGid = gid == Guid.Empty ? null : gid
                };

                if (row.MarkerTypeGid is Guid g) wantGids.Add(g);
                rows.Add(row);
            }

            // Fetch marker images by GID (if any)
            if (wantGids.Any())
            {
                var imgFilter = Builders<BoardGameImages>.Filter.And(
                    Builders<BoardGameImages>.Filter.Eq(x => x.SQLTable, "bgd.BoardGameMarkerType"),
                    Builders<BoardGameImages>.Filter.In(x => x.GID, wantGids.Select(g => (Guid?)g))
                );

                var images = await _imagesCollection.Find(imgFilter).ToListAsync();

                var byGid = images
                    .Where(d => d.GID != null && d.ImageBytes != null && !string.IsNullOrWhiteSpace(d.ContentType))
                    .ToDictionary(
                        d => d.GID!.Value,
                        d => $"data:{d.ContentType};base64,{Convert.ToBase64String(d.ImageBytes)}"
                    );

                foreach (var r in rows)
                {
                    if (r.MarkerTypeGid is Guid g && byGid.TryGetValue(g, out var url))
                        r.MarkerImageDataUrl = url;
                }
            }

            Players = rows;

            // Bind defaults
            Input = new InputModel
            {
                MatchId = id,
                NightId = NightId,
                PlayerResults = Players.Select(p => new PlayerResultInput
                {
                    MatchPlayerId = p.MatchPlayerId,
                    ResultTypeId = p.ExistingResultTypeId,
                    Score = p.ExistingScore,
                    IsWinner = p.ExistingIsWinner,
                    FinalTeam = p.ExistingFinalTeam
                }).ToList()
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Input?.PlayerResults == null)
            {
                ModelState.AddModelError(string.Empty, "No results submitted.");
                return await OnGetAsync(Input?.MatchId ?? 0);
            }

            foreach (var row in Input.PlayerResults)
            {
                if (row.ResultTypeId == null)
                {
                    ModelState.AddModelError(string.Empty, "Each player must have a result type.");
                    return await OnGetAsync(Input.MatchId);
                }
            }

            var now = DateTime.UtcNow;
            var who = User?.Identity?.Name ?? "system";

            var matchPlayers = await _db.BoardGameMatchPlayers
                .Where(mp => mp.FkBgdBoardGameMatch == Input.MatchId && !mp.Inactive)
                .ToListAsync();

            var mpIds = matchPlayers.Select(mp => mp.Id).ToList();

            var results = await _db.BoardGameMatchPlayerResults
                .Where(r => mpIds.Contains(r.FkBgdBoardGameMatchPlayer))
                .ToListAsync();

            var byMp = results.ToDictionary(r => r.FkBgdBoardGameMatchPlayer, r => r);

            foreach (var row in Input.PlayerResults)
            {
                if (!mpIds.Contains(row.MatchPlayerId)) continue;

                if (!byMp.TryGetValue(row.MatchPlayerId, out var existing))
                {
                    var entity = new BoardGameMatchPlayerResult
                    {
                        Gid = Guid.NewGuid(),
                        Inactive = false,
                        FkBgdBoardGameMatchPlayer = row.MatchPlayerId,
                        FkBgdResultType = row.ResultTypeId!.Value,
                        FinalScore = row.Score,
                        Win = row.IsWinner,
                        FinalTeam = row.FinalTeam,
                        TimeCreated = now,
                        TimeModified = now,
                        CreatedBy = who,
                        ModifiedBy = who
                    };
                    _db.BoardGameMatchPlayerResults.Add(entity);
                }
                else
                {
                    existing.FkBgdResultType = row.ResultTypeId!.Value;
                    existing.FinalScore = row.Score;
                    existing.Win = row.IsWinner;
                    existing.FinalTeam = row.FinalTeam;
                    existing.TimeModified = now;
                    existing.ModifiedBy = who;
                }
            }

            await _db.SaveChangesAsync();

            return RedirectToPage("/GameNight/Details", new { id = Input.NightId });
        }
    }
}

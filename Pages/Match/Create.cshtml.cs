using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Board_Game_Software.Models;
using Board_Game_Software.Services;
using System.Text.Json;
using System.Diagnostics;

namespace Board_Game_Software.Pages.Match
{
    public class CreateMatchModel : PageModel
    {
        private readonly BoardGameDbContext _db;
        private readonly BoardGameImagesService _images;
        private readonly IMongoCollection<BoardGameImages> _imagesCollection;

        public CreateMatchModel(BoardGameDbContext db, BoardGameImagesService images, IMongoClient mongo, IConfiguration config)
        {
            _db = db;
            _images = images;
            var dbName = config["MongoDbSettings:Database"];
            _imagesCollection = mongo.GetDatabase(dbName).GetCollection<BoardGameImages>("BoardGameImages");
        }

        public DateOnly? NightDate { get; private set; }
        public List<GameRow> Games { get; private set; } = new();
        public List<NightPlayerRow> Players { get; private set; } = new();

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public sealed class GameRow
        {
            public long Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string? CoverDataUrl { get; set; }
            public int? MinPlayers { get; set; }
            public int? MaxPlayers { get; set; }
        }

        public sealed class NightPlayerRow
        {
            public long PlayerId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string? AvatarDataUrl { get; set; }
        }

        public sealed class PlayerMarkerInput
        {
            public long PlayerId { get; set; }
            public long? MarkerId { get; set; }
        }

        public sealed class InputModel
        {
            [Required] public long NightId { get; set; }
            public string? ReturnUrl { get; set; }
            [Required] public long? BoardGameId { get; set; }
            [Required] public DateTime MatchDate { get; set; }
            public List<long> SelectedPlayerIds { get; set; } = new();
            public string? PlayerMarkersJson { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(long nightId, string? returnUrl = null)
        {
            var night = await _db.BoardGameNights.AsNoTracking().FirstOrDefaultAsync(n => n.Id == nightId);
            if (night == null) return NotFound();

            NightDate = night.GameNightDate;
            Input.NightId = nightId;
            Input.ReturnUrl = returnUrl;
            Input.MatchDate = night.GameNightDate.ToDateTime(new TimeOnly(DateTime.Now.Hour, DateTime.Now.Minute));

            var games = await _db.BoardGames.AsNoTracking().Where(g => !g.Inactive).OrderBy(g => g.BoardGameName).ToListAsync();
            var frontTypeGid = await _db.BoardGameImageTypes.AsNoTracking().Where(t => t.TypeDesc == "Board Game Front").Select(t => t.Gid).FirstOrDefaultAsync();
            var coverMap = await _images.GetFrontImagesAsync(games.Select(x => x.Gid), frontTypeGid);

            Games = games.Select(x => new GameRow
            {
                Id = x.Id,
                Name = x.BoardGameName,
                CoverDataUrl = coverMap.GetValueOrDefault(x.Gid),
                MinPlayers = x.PlayerCountMin,
                MaxPlayers = x.PlayerCountMax
            }).ToList();

            var roster = await _db.BoardGameNightPlayers.AsNoTracking()
                .Include(r => r.FkBgdPlayerNavigation)
                .Where(r => r.FkBgdBoardGameNight == nightId && !r.Inactive)
                .Select(r => r.FkBgdPlayerNavigation).ToListAsync();

            var playerGids = roster.Select(p => (Guid?)p.Gid).ToArray();
            var docs = await _imagesCollection.Find(Builders<BoardGameImages>.Filter.In(d => d.GID, playerGids)).ToListAsync();
            var avatarMap = docs.ToDictionary(d => d.GID!.Value, d => d.ImageBytes != null ? $"data:{d.ContentType};base64,{Convert.ToBase64String(d.ImageBytes)}" : null);

            Players = roster.Select(p => new NightPlayerRow { PlayerId = p.Id, Name = $"{p.FirstName} {p.LastName}", AvatarDataUrl = avatarMap.GetValueOrDefault(p.Gid) }).ToList();

            return Page();
        }

        public async Task<IActionResult> OnGetMarkersAsync(long gameId)
        {
            var markers = await _db.BoardGameMarkers.AsNoTracking()
                .Where(m => !m.Inactive && m.FkBgdBoardGame == gameId)
                .Include(m => m.FkBgdBoardGameMarkerTypeNavigation)
                .Select(m => new { id = m.Id, name = m.FkBgdBoardGameMarkerTypeNavigation.TypeDesc, typeGid = m.FkBgdBoardGameMarkerTypeNavigation.Gid })
                .ToListAsync();

            var docs = await _imagesCollection.Find(Builders<BoardGameImages>.Filter.In(x => x.GID, markers.Select(m => (Guid?)m.typeGid))).ToListAsync();
            var imgMap = docs.ToDictionary(d => d.GID!.Value, d => d.ImageBytes != null ? $"data:{d.ContentType};base64,{Convert.ToBase64String(d.ImageBytes)}" : null);

            return new JsonResult(markers.Select(x => new { id = x.id, name = x.name, imageDataUrl = imgMap.GetValueOrDefault(x.typeGid) }));
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync(Input.NightId, Input.ReturnUrl);
                return Page();
            }

            var playerMarkers = string.IsNullOrWhiteSpace(Input.PlayerMarkersJson)
                ? new List<PlayerMarkerInput>()
                : JsonSerializer.Deserialize<List<PlayerMarkerInput>>(Input.PlayerMarkersJson);

            var now = DateTime.UtcNow;
            var who = User.Identity?.Name ?? "system";

            // 1. Fetch Game to determine Victory Type for Result Defaulting
            var game = await _db.BoardGames
                .Include(g => g.FkBgdBoardGameVictoryConditionTypeNavigation)
                .FirstOrDefaultAsync(g => g.Id == Input.BoardGameId);

            // Logic: Default everyone to Team Loss (4) if game is Team Victory, else Solo Loss (2)
            long defaultResultId = 2; // Solo Loss
            if (game?.FkBgdBoardGameVictoryConditionTypeNavigation?.TypeDesc == "Team Victory")
            {
                defaultResultId = 4; // Team Loss
            }

            // 2. Create the Match
            var match = new BoardGameMatch
            {
                Gid = Guid.NewGuid(),
                FkBgdBoardGame = Input.BoardGameId!.Value,
                MatchDate = Input.MatchDate,
                FinishedDate = Input.MatchDate,
                TimeCreated = now,
                TimeModified = now,
                CreatedBy = who,
                ModifiedBy = who,
                MatchComplete = false
            };

            _db.BoardGameMatches.Add(match);
            await _db.SaveChangesAsync();

            // 3. Link to Night
            _db.BoardGameNightBoardGameMatches.Add(new BoardGameNightBoardGameMatch
            {
                FkBgdBoardGameNight = Input.NightId,
                FkBgdBoardGameMatch = match.Id,
                Gid = Guid.NewGuid(),
                TimeCreated = now,
                TimeModified = now,
                CreatedBy = who,
                ModifiedBy = who
            });

            // 4. Process Players and Pre-create Result Records
            foreach (var pid in Input.SelectedPlayerIds)
            {
                var markerId = playerMarkers?.FirstOrDefault(m => m.PlayerId == pid)?.MarkerId;

                var newPlayer = new BoardGameMatchPlayer
                {
                    FkBgdBoardGameMatch = match.Id,
                    FkBgdPlayer = pid,
                    FkBgdBoardGameMarker = markerId,
                    Gid = Guid.NewGuid(),
                    TimeCreated = now,
                    TimeModified = now,
                    CreatedBy = who,
                    ModifiedBy = who,
                };
                _db.BoardGameMatchPlayers.Add(newPlayer);
                await _db.SaveChangesAsync(); // Get ID for result record

                // AUTOFILL LOGIC
                FinalTeam? teamAlignment = null;
                if (markerId.HasValue)
                {
                    var marker = await _db.BoardGameMarkers
                        .Include(m => m.FkBgdBoardGameMarkerTypeNavigation)
                        .FirstOrDefaultAsync(m => m.Id == markerId.Value);

                    var alignmentId = marker?.FkBgdBoardGameMarkerTypeNavigation?.FkBgdMarkerAlignmentType;
                    if (alignmentId.HasValue)
                    {
                        teamAlignment = (FinalTeam)alignmentId.Value;
                    }
                }

                _db.BoardGameMatchPlayerResults.Add(new BoardGameMatchPlayerResult
                {
                    Gid = Guid.NewGuid(),
                    FkBgdBoardGameMatchPlayer = newPlayer.Id,
                    FkBgdResultType = defaultResultId, // Autofill: Based on Victory Type
                    FinalTeam = teamAlignment,         // Autofill: Based on Marker Alignment
                    TimeCreated = now,
                    TimeModified = now,
                    CreatedBy = who,
                    ModifiedBy = who
                });
            }

            await _db.SaveChangesAsync();
            return Redirect(Input.ReturnUrl ?? $"/GameNight/Details/{Input.NightId}");
        }
    }
}
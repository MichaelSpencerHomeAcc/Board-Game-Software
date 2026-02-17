using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Board_Game_Software.Pages.Match
{
    public class CreateMatchModel : PageModel
    {
        private readonly BoardGameDbContext _db;

        public CreateMatchModel(BoardGameDbContext db)
        {
            _db = db;
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
            public string? CoverUrl { get; set; }  
            public int? MinPlayers { get; set; }
            public int? MaxPlayers { get; set; }
            public Guid GameGid { get; set; }           
        }

        public sealed class NightPlayerRow
        {
            public long PlayerId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string? AvatarUrl { get; set; }       
            public Guid PlayerGid { get; set; }       
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

            var games = await _db.BoardGames.AsNoTracking()
                .Where(g => !g.Inactive)
                .OrderBy(g => g.BoardGameName)
                .Select(g => new { g.Id, g.Gid, g.BoardGameName, g.PlayerCountMin, g.PlayerCountMax })
                .ToListAsync();

            Games = games.Select(g => new GameRow
            {
                Id = g.Id,
                GameGid = g.Gid,
                Name = g.BoardGameName,
                CoverUrl = $"/media/boardgame/front/{g.Gid}",
                MinPlayers = g.PlayerCountMin,
                MaxPlayers = g.PlayerCountMax
            }).ToList();

            var roster = await _db.BoardGameNightPlayers.AsNoTracking()
                .Include(r => r.FkBgdPlayerNavigation)
                .Where(r => r.FkBgdBoardGameNight == nightId && !r.Inactive)
                .Select(r => r.FkBgdPlayerNavigation)
                .Select(p => new { p.Id, p.Gid, p.FirstName, p.LastName })
                .ToListAsync();

            Players = roster.Select(p => new NightPlayerRow
            {
                PlayerId = p.Id,
                PlayerGid = p.Gid,
                Name = $"{p.FirstName} {p.LastName}",
                AvatarUrl = $"/media/player/{p.Gid}"
            }).ToList();

            return Page();
        }

        public async Task<IActionResult> OnGetMarkersAsync(long gameId)
        {
            var markers = await _db.BoardGameMarkers.AsNoTracking()
                .Where(m => !m.Inactive && m.FkBgdBoardGame == gameId)
                .Include(m => m.FkBgdBoardGameMarkerTypeNavigation)
                .Select(m => new
                {
                    id = m.Id,
                    name = m.FkBgdBoardGameMarkerTypeNavigation.TypeDesc,
                    typeGid = m.FkBgdBoardGameMarkerTypeNavigation.Gid
                })
                .ToListAsync();

            return new JsonResult(markers.Select(x => new
            {
                id = x.id,
                name = x.name,
                imageUrl = $"/media/marker-type/{x.typeGid}"
            }));
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
                : (JsonSerializer.Deserialize<List<PlayerMarkerInput>>(Input.PlayerMarkersJson) ?? new List<PlayerMarkerInput>());

            var now = DateTime.UtcNow;
            var who = User.Identity?.Name ?? "system";

            // Fetch game to set default result logic
            var game = await _db.BoardGames
                .Include(g => g.FkBgdBoardGameVictoryConditionTypeNavigation)
                .FirstOrDefaultAsync(g => g.Id == Input.BoardGameId);

            long defaultResultId = 2; // Solo Loss
            if (game?.FkBgdBoardGameVictoryConditionTypeNavigation?.TypeDesc == "Team Victory")
                defaultResultId = 4; // Team Loss

            // 1) Create match
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
            await _db.SaveChangesAsync(); // Need match.Id

            // 2) Link to night
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

            var markerByPlayerId = playerMarkers.ToDictionary(x => x.PlayerId, x => x.MarkerId);

            var selectedMarkerIds = markerByPlayerId.Values
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .Distinct()
                .ToList();

            var markerAlignmentByMarkerId = new Dictionary<long, long?>();
            if (selectedMarkerIds.Count > 0)
            {
                markerAlignmentByMarkerId = await _db.BoardGameMarkers.AsNoTracking()
                    .Where(m => selectedMarkerIds.Contains(m.Id))
                    .Include(m => m.FkBgdBoardGameMarkerTypeNavigation)
                    .ToDictionaryAsync(
                        m => m.Id,
                        m => (long?)m.FkBgdBoardGameMarkerTypeNavigation.FkBgdMarkerAlignmentType
                    );
            }

            // 3) Create all match players (batch)
            var matchPlayers = new List<BoardGameMatchPlayer>();

            foreach (var pid in Input.SelectedPlayerIds)
            {
                markerByPlayerId.TryGetValue(pid, out var markerId);

                matchPlayers.Add(new BoardGameMatchPlayer
                {
                    FkBgdBoardGameMatch = match.Id,
                    FkBgdPlayer = pid,
                    FkBgdBoardGameMarker = markerId,
                    Gid = Guid.NewGuid(),
                    TimeCreated = now,
                    TimeModified = now,
                    CreatedBy = who,
                    ModifiedBy = who,
                });
            }

            _db.BoardGameMatchPlayers.AddRange(matchPlayers);
            await _db.SaveChangesAsync(); 

            // 4) Create all results (batch)
            var results = new List<BoardGameMatchPlayerResult>();

            foreach (var mp in matchPlayers)
            {
                FinalTeam? teamAlignment = null;

                if (mp.FkBgdBoardGameMarker.HasValue &&
                    markerAlignmentByMarkerId.TryGetValue(mp.FkBgdBoardGameMarker.Value, out var alignId) &&
                    alignId.HasValue)
                {
                    teamAlignment = (FinalTeam)alignId.Value;
                }

                results.Add(new BoardGameMatchPlayerResult
                {
                    Gid = Guid.NewGuid(),
                    FkBgdBoardGameMatchPlayer = mp.Id,
                    FkBgdResultType = defaultResultId,
                    FinalTeam = teamAlignment,
                    TimeCreated = now,
                    TimeModified = now,
                    CreatedBy = who,
                    ModifiedBy = who
                });
            }

            _db.BoardGameMatchPlayerResults.AddRange(results);

            await _db.SaveChangesAsync();
            return Redirect(Input.ReturnUrl ?? $"/GameNight/Details/{Input.NightId}");
        }
    }
}

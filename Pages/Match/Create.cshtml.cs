using Board_Game_Software.Models;
using Board_Game_Software.Services;
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
        private readonly ICurrentClubService _currentClubService;

        public CreateMatchModel(BoardGameDbContext db, ICurrentClubService currentClubService)
        {
            _db = db;
            _currentClubService = currentClubService;
        }

        public DateOnly? NightDate { get; private set; }
        public List<GameRow> Games { get; private set; } = new();
        public List<NightPlayerRow> Players { get; private set; } = new();
        public GameRow? PreselectedGame { get; private set; }

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

        public async Task<IActionResult> OnGetAsync(long nightId, string? returnUrl = null, long? boardGameId = null)
        {
            var night = await _db.BoardGameNights.AsNoTracking().FirstOrDefaultAsync(n => n.Id == nightId);
            if (night == null) return NotFound();
            if (!await CanAccessNightAsync(night)) return Forbid();

            NightDate = night.GameNightDate;
            Input.NightId = nightId;
            Input.ReturnUrl = returnUrl;
            Input.MatchDate = night.GameNightDate.ToDateTime(new TimeOnly(DateTime.Now.Hour, DateTime.Now.Minute));

            var linkedExpansionIds = _db.BoardGameExpansions
                .Where(link => !link.Inactive)
                .Select(link => link.FkBgdExpansionBoardGame);

            var gamesQuery = _db.BoardGames.AsNoTracking()
                .Include(g => g.BoardGameExpansionBaseGames)
                    .ThenInclude(link => link.FkBgdExpansionBoardGameNavigation)
                .Where(g => !g.Inactive
                    && !g.IsExpansion
                    && !linkedExpansionIds.Contains(g.Id));

            if (night.FkBgdClub.HasValue)
            {
                var nightClubId = night.FkBgdClub.Value;
                gamesQuery = gamesQuery.Where(g => g.FkBgdClub == nightClubId);
            }

            var games = await gamesQuery
                .OrderBy(g => g.BoardGameName)
                .ToListAsync();

            Games = games.Select(g => new GameRow
            {
                Id = g.Id,
                GameGid = g.Gid,
                Name = g.BoardGameName,
                CoverUrl = $"/media/boardgame/front/{g.Gid}",
                MinPlayers = GetCombinedMinPlayers(g),
                MaxPlayers = GetCombinedMaxPlayers(g)
            }).ToList();

            if (boardGameId.HasValue)
            {
                PreselectedGame = Games.FirstOrDefault(g => g.Id == boardGameId.Value);
                Input.BoardGameId = PreselectedGame?.Id;
            }

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
            var gameIds = await GetGameAndExpansionIds(gameId);

            var markers = await _db.BoardGameMarkers.AsNoTracking()
                .Where(m => !m.Inactive && gameIds.Contains(m.FkBgdBoardGame))
                .Include(m => m.FkBgdBoardGameMarkerTypeNavigation)
                .Include(m => m.FkBgdBoardGameNavigation)
                .OrderBy(m => m.FkBgdBoardGame == gameId ? 0 : 1)
                .ThenBy(m => m.FkBgdBoardGameNavigation!.BoardGameName)
                .ThenBy(m => m.FkBgdBoardGameMarkerTypeNavigation!.TypeDesc)
                .Select(m => new
                {
                    id = m.Id,
                    name = m.FkBgdBoardGameMarkerTypeNavigation!.TypeDesc,
                    typeGid = m.FkBgdBoardGameMarkerTypeNavigation.Gid,
                    sourceGameId = m.FkBgdBoardGame,
                    sourceGameName = m.FkBgdBoardGameNavigation!.BoardGameName
                })
                .ToListAsync();

            return new JsonResult(markers.Select(x => new
            {
                id = x.id,
                name = x.name,
                sourceGameId = x.sourceGameId,
                sourceGameName = x.sourceGameName,
                isExpansionMarker = x.sourceGameId != gameId,
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

            var night = await _db.BoardGameNights.AsNoTracking().FirstOrDefaultAsync(n => n.Id == Input.NightId);
            if (night == null) return NotFound();
            if (!await CanAccessNightAsync(night)) return Forbid();

            var now = DateTime.UtcNow;
            var who = User.Identity?.Name ?? "system";

            // Fetch game to set default result logic
            var game = await _db.BoardGames
                .Include(g => g.FkBgdBoardGameVictoryConditionTypeNavigation)
                .FirstOrDefaultAsync(g => g.Id == Input.BoardGameId);

            if (game == null || game.Inactive || (night.FkBgdClub.HasValue && game.FkBgdClub != night.FkBgdClub.Value))
            {
                ModelState.AddModelError(nameof(Input.BoardGameId), "Choose a game from this club's library.");
                await OnGetAsync(Input.NightId, Input.ReturnUrl);
                return Page();
            }

            var validNightPlayerIds = await _db.BoardGameNightPlayers.AsNoTracking()
                .Where(np => np.FkBgdBoardGameNight == Input.NightId && !np.Inactive)
                .Select(np => np.FkBgdPlayer)
                .ToListAsync();

            Input.SelectedPlayerIds = Input.SelectedPlayerIds
                .Distinct()
                .Where(validNightPlayerIds.Contains)
                .ToList();

            if (!Input.SelectedPlayerIds.Any())
            {
                ModelState.AddModelError(nameof(Input.SelectedPlayerIds), "Select at least one player from this game night.");
                await OnGetAsync(Input.NightId, Input.ReturnUrl);
                return Page();
            }

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
                        m => (long?)m.FkBgdBoardGameMarkerTypeNavigation!.FkBgdMarkerAlignmentType
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

        private static int? GetCombinedMinPlayers(BoardGame game)
        {
            var values = game.BoardGameExpansionBaseGames
                .Where(link => !link.Inactive && !link.FkBgdExpansionBoardGameNavigation.Inactive)
                .Select(link => (int?)link.FkBgdExpansionBoardGameNavigation.PlayerCountMin)
                .Append(game.PlayerCountMin)
                .Where(value => value.HasValue)
                .Select(value => value!.Value)
                .ToList();

            return values.Any() ? values.Min() : null;
        }

        private static int? GetCombinedMaxPlayers(BoardGame game)
        {
            var values = game.BoardGameExpansionBaseGames
                .Where(link => !link.Inactive && !link.FkBgdExpansionBoardGameNavigation.Inactive)
                .Select(link => (int?)link.FkBgdExpansionBoardGameNavigation.PlayerCountMax)
                .Append(game.PlayerCountMax)
                .Where(value => value.HasValue)
                .Select(value => value!.Value)
                .ToList();

            return values.Any() ? values.Max() : null;
        }

        private async Task<List<long>> GetGameAndExpansionIds(long gameId)
        {
            var expansionIds = await _db.BoardGameExpansions.AsNoTracking()
                .Where(link => !link.Inactive
                    && link.FkBgdBoardGame == gameId
                    && !link.FkBgdExpansionBoardGameNavigation!.Inactive)
                .Select(link => link.FkBgdExpansionBoardGame)
                .ToListAsync();

            expansionIds.Insert(0, gameId);
            return expansionIds;
        }

        private async Task<bool> CanAccessNightAsync(BoardGameNight night)
        {
            if (User.IsInRole("Admin")) return true;

            var currentClub = await _currentClubService.GetCurrentClubAsync();
            return night.FkBgdClub.HasValue && night.FkBgdClub == currentClub.CurrentClubId;
        }
    }
}

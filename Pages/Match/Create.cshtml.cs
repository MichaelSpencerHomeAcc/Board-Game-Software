using Board_Game_Software.Models;
using Board_Game_Software.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;

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
        public string PageTitle { get; private set; } = "Record Match";
        public string CancelUrl { get; private set; } = "/GameNight";
        public List<GameRow> Games { get; private set; } = new();
        public List<NightPlayerRow> Players { get; private set; } = new();
        public GameRow? PreselectedGame { get; private set; }
        public List<SelectListItem> MatchTypeOptions { get; } = MatchDefaults.MatchTypes
            .Select(type => new SelectListItem(MatchDefaults.GetMatchTypeLabel(type), type))
            .ToList();

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

        private sealed class RosterQueryRow
        {
            public long Id { get; init; }
            public Guid Gid { get; init; }
            public string? FirstName { get; init; }
            public string? LastName { get; init; }
        }

        public sealed class PlayerMarkerInput
        {
            public long PlayerId { get; set; }
            public long? MarkerId { get; set; }
        }

        public sealed class InputModel
        {
            public long? NightId { get; set; }
            public string? ReturnUrl { get; set; }
            public string PlayContext { get; set; } = MatchDefaults.ClubGameNightContext;
            public string MatchType { get; set; } = MatchDefaults.ScoredMatchType;
            [Required] public long? BoardGameId { get; set; }
            [Required] public DateTime MatchDate { get; set; }
            public List<long> SelectedPlayerIds { get; set; } = new();
            public string? GuestNames { get; set; }
            public string? PlayerMarkersJson { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(long? nightId = null, string? returnUrl = null, long? boardGameId = null)
        {
            BoardGameNight? night = null;
            var currentClub = await _currentClubService.GetCurrentClubAsync();

            if (nightId.HasValue)
            {
                night = await _db.BoardGameNights.AsNoTracking().FirstOrDefaultAsync(n => n.Id == nightId.Value);
                if (night == null) return NotFound();
                if (!await CanAccessNightAsync(night)) return Forbid();

                NightDate = night.GameNightDate;
                PageTitle = "Record Game Night Match";
                CancelUrl = returnUrl ?? $"/GameNight/Details/{nightId.Value}";
            }
            else
            {
                PageTitle = currentClub.CurrentClubId.HasValue ? "Record One-Off Match" : "Record Personal Play";
                CancelUrl = returnUrl ?? "/Browsing/BoardGames";
            }

            Input.NightId = nightId;
            Input.ReturnUrl = returnUrl;
            Input.MatchType = MatchDefaults.ScoredMatchType;
            Input.MatchDate = night?.GameNightDate.ToDateTime(new TimeOnly(DateTime.Now.Hour, DateTime.Now.Minute)) ?? DateTime.Now;

            var matchClubId = night?.FkBgdClub ?? (!currentClub.IsPlatformAdminMode ? currentClub.CurrentClubId : null);
            var clubType = await GetClubTypeAsync(matchClubId);
            Input.PlayContext = GetPlayContext(matchClubId, clubType, night != null);

            var linkedExpansionIds = _db.BoardGameExpansions
                .Where(link => !link.Inactive)
                .Select(link => link.FkBgdExpansionBoardGame);

            var gamesQuery = _db.BoardGames.AsNoTracking()
                .Include(g => g.BoardGameExpansionBaseGames)
                    .ThenInclude(link => link.FkBgdExpansionBoardGameNavigation)
                .Where(g => !g.Inactive
                    && g.GameStatus != BoardGameDefaults.RejectedStatus
                    && g.GameStatus != BoardGameDefaults.MergedStatus
                    && !g.IsExpansion
                    && !linkedExpansionIds.Contains(g.Id));

            if (matchClubId.HasValue)
            {
                gamesQuery = gamesQuery.Where(g => g.FkBgdClub == matchClubId.Value);
            }
            else
            {
                gamesQuery = gamesQuery.Where(g => g.FkBgdClub == null);
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

            var roster = nightId.HasValue
                ? await _db.BoardGameNightPlayers.AsNoTracking()
                    .Include(r => r.FkBgdPlayerNavigation)
                    .Where(r => r.FkBgdBoardGameNight == nightId.Value && !r.Inactive)
                    .Select(r => r.FkBgdPlayerNavigation)
                    .Select(p => new RosterQueryRow { Id = p.Id, Gid = p.Gid, FirstName = p.FirstName, LastName = p.LastName })
                    .ToListAsync()
                : await LoadOneOffRosterAsync(matchClubId);

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
                await OnGetAsync(Input.NightId, Input.ReturnUrl, Input.BoardGameId);
                return Page();
            }

            var playerMarkers = string.IsNullOrWhiteSpace(Input.PlayerMarkersJson)
                ? new List<PlayerMarkerInput>()
                : (JsonSerializer.Deserialize<List<PlayerMarkerInput>>(Input.PlayerMarkersJson) ?? new List<PlayerMarkerInput>());

            if (!MatchDefaults.IsValidMatchType(Input.MatchType))
            {
                ModelState.AddModelError(nameof(Input.MatchType), "Choose a valid match type.");
                await OnGetAsync(Input.NightId, Input.ReturnUrl, Input.BoardGameId);
                return Page();
            }

            BoardGameNight? night = null;
            if (Input.NightId.HasValue)
            {
                night = await _db.BoardGameNights.AsNoTracking().FirstOrDefaultAsync(n => n.Id == Input.NightId.Value);
                if (night == null) return NotFound();
                if (!await CanAccessNightAsync(night)) return Forbid();
            }

            var currentClub = await _currentClubService.GetCurrentClubAsync();
            var matchClubId = night?.FkBgdClub ?? (!currentClub.IsPlatformAdminMode ? currentClub.CurrentClubId : null);
            var clubType = await GetClubTypeAsync(matchClubId);
            Input.PlayContext = GetPlayContext(matchClubId, clubType, night != null);

            var now = DateTime.UtcNow;
            var who = User.Identity?.Name ?? "system";

            // Fetch game to set default result logic
            var game = await _db.BoardGames
                .Include(g => g.FkBgdBoardGameVictoryConditionTypeNavigation)
                .FirstOrDefaultAsync(g => g.Id == Input.BoardGameId);

            if (game == null ||
                game.Inactive ||
                game.GameStatus is BoardGameDefaults.RejectedStatus or BoardGameDefaults.MergedStatus ||
                game.FkBgdClub != matchClubId)
            {
                ModelState.AddModelError(nameof(Input.BoardGameId), matchClubId.HasValue
                    ? "Choose a game from this club's library."
                    : "Choose a shared library game for personal play.");
                await OnGetAsync(Input.NightId, Input.ReturnUrl, Input.BoardGameId);
                return Page();
            }

            var validPlayerIds = Input.NightId.HasValue
                ? await _db.BoardGameNightPlayers.AsNoTracking()
                    .Where(np => np.FkBgdBoardGameNight == Input.NightId.Value && !np.Inactive)
                    .Select(np => np.FkBgdPlayer)
                    .ToListAsync()
                : (await LoadOneOffRosterAsync(matchClubId)).Select(p => p.Id).ToList();

            Input.SelectedPlayerIds = Input.SelectedPlayerIds
                .Distinct()
                .Where(validPlayerIds.Contains)
                .ToList();

            var guestNames = ParseGuestNames(Input.GuestNames);

            if (!Input.SelectedPlayerIds.Any() && !guestNames.Any())
            {
                ModelState.AddModelError(nameof(Input.SelectedPlayerIds), Input.NightId.HasValue
                    ? "Select at least one game-night player or add a guest."
                    : "Select at least one player or add a guest.");
                await OnGetAsync(Input.NightId, Input.ReturnUrl, Input.BoardGameId);
                return Page();
            }

            var totalParticipants = Input.SelectedPlayerIds.Count + guestNames.Count;
            var maxPlayers = GetCombinedMaxPlayers(game);
            if (maxPlayers.HasValue && totalParticipants > maxPlayers.Value)
            {
                ModelState.AddModelError(nameof(Input.SelectedPlayerIds), $"This game allows up to {maxPlayers.Value} players.");
                await OnGetAsync(Input.NightId, Input.ReturnUrl, Input.BoardGameId);
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
                FkBgdClub = matchClubId,
                PlayContext = Input.PlayContext,
                MatchType = Input.MatchType,
                Visibility = GetMatchVisibility(matchClubId, clubType),
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

            // 2) Link to night when this is a scheduled game-night match.
            if (Input.NightId.HasValue)
            {
                _db.BoardGameNightBoardGameMatches.Add(new BoardGameNightBoardGameMatch
                {
                    FkBgdBoardGameNight = Input.NightId.Value,
                    FkBgdBoardGameMatch = match.Id,
                    Gid = Guid.NewGuid(),
                    TimeCreated = now,
                    TimeModified = now,
                    CreatedBy = who,
                    ModifiedBy = who
                });
            }

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

            foreach (var guestName in guestNames)
            {
                matchPlayers.Add(new BoardGameMatchPlayer
                {
                    FkBgdBoardGameMatch = match.Id,
                    GuestName = guestName,
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
            return Redirect(Input.ReturnUrl ?? (Input.NightId.HasValue ? $"/GameNight/Details/{Input.NightId.Value}" : $"/Match/Results/{match.Id}"));
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

        private async Task<List<RosterQueryRow>> LoadOneOffRosterAsync(long? clubId)
        {
            if (clubId.HasValue)
            {
                return await _db.Players
                    .AsNoTracking()
                    .Where(p => !p.Inactive &&
                        p.PlayerClubs.Any(pc => !pc.Inactive && pc.FkBgdClub == clubId.Value))
                    .OrderBy(p => p.FirstName)
                    .ThenBy(p => p.LastName)
                    .Select(p => new RosterQueryRow { Id = p.Id, Gid = p.Gid, FirstName = p.FirstName, LastName = p.LastName })
                    .ToListAsync();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return await _db.Players
                .AsNoTracking()
                .Where(p => !p.Inactive && p.FkdboAspNetUsers == userId)
                .OrderBy(p => p.FirstName)
                .ThenBy(p => p.LastName)
                .Select(p => new RosterQueryRow { Id = p.Id, Gid = p.Gid, FirstName = p.FirstName, LastName = p.LastName })
                .ToListAsync();
        }

        private static List<string> ParseGuestNames(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return new List<string>();
            }

            return Regex.Split(value, @"[\r\n,;]+")
                .Select(name => name.Trim())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(24)
                .ToList();
        }

        private async Task<string?> GetClubTypeAsync(long? clubId)
        {
            if (!clubId.HasValue)
            {
                return null;
            }

            return await _db.Clubs
                .AsNoTracking()
                .Where(c => c.Id == clubId.Value && !c.Inactive)
                .Select(c => c.ClubType)
                .FirstOrDefaultAsync();
        }

        private static string GetPlayContext(long? clubId, string? clubType, bool hasGameNight)
        {
            if (!clubId.HasValue)
            {
                return MatchDefaults.PersonalContext;
            }

            if (hasGameNight)
            {
                return MatchDefaults.ClubGameNightContext;
            }

            return clubType == ClubDefaults.PrivateGroupType
                ? MatchDefaults.PrivateGroupContext
                : MatchDefaults.ClubOneOffContext;
        }

        private static string GetMatchVisibility(long? clubId, string? clubType)
        {
            if (!clubId.HasValue)
            {
                return MatchDefaults.PrivateVisibility;
            }

            return clubType == ClubDefaults.PrivateGroupType
                ? MatchDefaults.PrivateVisibility
                : MatchDefaults.MembersOnlyVisibility;
        }

        private async Task<bool> CanAccessNightAsync(BoardGameNight night)
        {
            if (User.IsInRole("Admin")) return true;

            var currentClub = await _currentClubService.GetCurrentClubAsync();
            return night.FkBgdClub.HasValue && night.FkBgdClub == currentClub.CurrentClubId;
        }
    }
}

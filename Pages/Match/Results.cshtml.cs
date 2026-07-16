using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Board_Game_Software.Services;
using System.Security.Claims;

namespace Board_Game_Software.Pages.Match
{
    public class ResultsModel : PageModel
    {
        private readonly BoardGameDbContext _db;
        private readonly RatingService _ratingService;
        private readonly AchievementService _achievementService;
        private readonly ICurrentClubService _currentClubService;

        public ResultsModel(
            BoardGameDbContext db,
            RatingService ratingService,
            AchievementService achievementService,
            ICurrentClubService currentClubService)
        {
            _db = db;
            _ratingService = ratingService;
            _achievementService = achievementService;
            _currentClubService = currentClubService;
        }

        public string MatchGameName { get; private set; } = string.Empty;
        public long MatchGameId { get; private set; }
        public string MatchTypeLabel { get; private set; } = MatchDefaults.GetMatchTypeLabel(MatchDefaults.ScoredMatchType);
        public string? GameBannerUrl { get; private set; }
        public DateTime? MatchDate { get; private set; }
        public bool ShowPoints { get; private set; }
        public bool ShowTeams { get; private set; }
        public bool IsTeamVictoryGame { get; private set; }
        public bool? MatchComplete { get; private set; }
        public long NightId { get; private set; }
        public string? GameBoxArt { get; private set; }
        public string RatingMethodName { get; private set; } = "Rating";
        public string RatingMethodReason { get; private set; } = "Ratings are calculated when the match is completed.";
        public string? StatusMessage { get; private set; }

        [BindProperty] public InputModel Input { get; set; } = new();
        [BindProperty] public LinkGuestInputModel LinkGuest { get; set; } = new();
        [BindProperty] public AddParticipantInputModel AddParticipant { get; set; } = new();
        public List<ResultTypeRow> ResultTypes { get; private set; } = new();
        public List<PlayerRow> Players { get; private set; } = new();
        public List<GuestParticipantRow> GuestParticipants { get; private set; } = new();
        public List<SelectListItem> LinkablePlayers { get; private set; } = new();
        public List<SelectListItem> AddablePlayers { get; private set; } = new();

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
            public decimal? PreMatchRatingMu { get; init; }
            public decimal? RatingChangeMu { get; init; }
            public decimal? PostMatchRatingMu => PreMatchRatingMu.HasValue && RatingChangeMu.HasValue
                ? PreMatchRatingMu.Value + RatingChangeMu.Value
                : null;
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

        public sealed class LinkGuestInputModel
        {
            public long MatchPlayerId { get; set; }
            public long PlayerId { get; set; }
        }

        public sealed class GuestParticipantRow
        {
            public long MatchPlayerId { get; init; }
            public string GuestName { get; init; } = string.Empty;
        }

        public sealed class AddParticipantInputModel
        {
            public long? PlayerId { get; set; }
            public string? GuestName { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(long id)
        {
            var match = await _db.BoardGameMatches
                .Include(m => m.FkBgdBoardGameNavigation)
                    .ThenInclude(bg => bg.FkBgdBoardGameVictoryConditionTypeNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (match == null) return NotFound();
            if (!await CanAccessMatchAsync(match)) return Forbid();
            StatusMessage = TempData["StatusMessage"] as string;

            var game = match.FkBgdBoardGameNavigation;
            MatchGameId = game?.Id ?? 0;
            ShowPoints = game?.FkBgdBoardGameVictoryConditionTypeNavigation?.Points ?? false;
            IsTeamVictoryGame = game?.FkBgdBoardGameVictoryConditionTypeNavigation?.TypeDesc == "Team Victory";
            MatchComplete = match.MatchComplete;

            if (game != null)
            {
                GameBannerUrl = $"/media/boardgame/front/{game.Gid:D}";
                GameBoxArt = GameBannerUrl;

                var methods = await _db.BoardGameEloMethods.AsNoTracking()
                    .Include(m => m.FkBgdEloMethodNavigation)
                    .Where(m => m.FkBgdBoardGame == game.Id && !m.Inactive)
                    .Select(m => m.FkBgdEloMethodNavigation!.MethodName)
                    .ToListAsync();

                if (methods.Any())
                {
                    RatingMethodName = string.Join(" + ", methods);
                    RatingMethodReason = IsTeamVictoryGame
                        ? "Team victory games use the configured team-aware rating method so aligned players move together."
                        : ShowPoints
                            ? "Point-score games use the configured method with final scores and winners to size each rating move."
                            : "Placement games use the configured method with finishing order and winner flags.";
                }
            }

            var link = await _db.BoardGameNightBoardGameMatches
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.FkBgdBoardGameMatch == id && !x.Inactive);

            NightId = link?.FkBgdBoardGameNight ?? 0;
            MatchGameName = game?.BoardGameName ?? "Match";
            MatchTypeLabel = MatchDefaults.GetMatchTypeLabel(match.MatchType);
            MatchDate = match.MatchDate;

            ResultTypes = await _db.ResultTypes.AsNoTracking()
                .Select(r => new ResultTypeRow
                {
                    Id = r.Id,
                    Name = r.TypeDesc ?? string.Empty,
                    IsVictory = r.IsVictory == true,
                    IsDefeat = r.IsDefeat == true
                }).ToListAsync();

            var matchPlayers = await _db.BoardGameMatchPlayers
                .AsNoTracking()
                .Include(mp => mp.FkBgdPlayerNavigation)
                .Include(mp => mp.FkBgdBoardGameMarkerNavigation)
                    .ThenInclude(mk => mk!.FkBgdBoardGameMarkerTypeNavigation)
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
            var guests = new List<GuestParticipantRow>();

            foreach (var mp in matchPlayers.OrderBy(GetParticipantName))
            {
                existingByMp.TryGetValue(mp.Id, out var res);
                var markerType = mp.FkBgdBoardGameMarkerNavigation?.FkBgdBoardGameMarkerTypeNavigation;
                var participantName = GetParticipantName(mp);

                var row = new PlayerRow
                {
                    MatchPlayerId = mp.Id,
                    PlayerName = participantName,
                    ExistingResultTypeId = res?.FkBgdResultType,
                    ExistingScore = res?.FinalScore,
                    ExistingIsWinner = res?.Win ?? false,
                    ExistingFinalTeam = res?.FinalTeam,
                    MarkerTypeName = markerType?.TypeDesc,
                    MarkerTypeGid = markerType?.Gid,
                    PlayerGid = mp.FkBgdPlayerNavigation?.Gid,
                    PreMatchRatingMu = res?.PreMatchRatingMu,
                    RatingChangeMu = res?.RatingChangeMu
                };
                if (row.MarkerTypeGid.HasValue) row.MarkerImageDataUrl = $"/media/marker-type/{row.MarkerTypeGid.Value:D}";
                if (row.PlayerGid.HasValue) row.PlayerAvatarUrl = $"/media/player/{row.PlayerGid.Value:D}";
                rows.Add(row);

                if (!mp.FkBgdPlayer.HasValue)
                {
                    guests.Add(new GuestParticipantRow
                    {
                        MatchPlayerId = mp.Id,
                        GuestName = participantName
                    });
                }
            }

            Players = rows;
            GuestParticipants = guests;
            LinkablePlayers = await LoadLinkablePlayersAsync(match);
            AddablePlayers = await LoadAddablePlayersAsync(match, NightId, matchPlayers);
            Input = new InputModel
            {
                MatchId = id,
                NightId = NightId,
                FinishedDate = match.FinishedDate ?? match.MatchDate,
                PlayerResults = Players.Select(p => new PlayerResultInput
                {
                    MatchPlayerId = p.MatchPlayerId,
                    ResultTypeId = p.ExistingResultTypeId ?? ResultTypes.FirstOrDefault()?.Id,
                    Score = p.ExistingScore ?? (!ShowPoints && p.ExistingIsWinner ? 1 : null),
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
            if (!await CanAccessMatchAsync(match)) return Forbid();

            match.FinishedDate = Input.FinishedDate ?? match.MatchDate;
            match.TimeModified = DateTime.UtcNow;

            foreach (var row in Input.PlayerResults)
            {
                var existing = await _db.BoardGameMatchPlayerResults.FirstOrDefaultAsync(r => r.FkBgdBoardGameMatchPlayer == row.MatchPlayerId);
                if (existing != null)
                {
                    existing.FkBgdResultType = row.ResultTypeId ?? 1;
                    existing.FinalScore = row.Score;
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
            return RedirectToPage(new { id = Input.MatchId });
        }

        public async Task<IActionResult> OnPostCompleteAsync()
        {
            var match = await _db.BoardGameMatches
                .Include(m => m.FkBgdBoardGameNavigation)
                    .ThenInclude(bg => bg.BoardGameShelfSections)
                        .ThenInclude(bgss => bgss.FkBgdShelfSectionNavigation)
                            .ThenInclude(ss => ss.FkBgdShelfNavigation)
                .FirstOrDefaultAsync(m => m.Id == Input.MatchId);

            if (match == null || match.MatchComplete == true) return NotFound();
            if (!await CanAccessMatchAsync(match)) return Forbid();

            await OnPostAsync();
            var isCompetitive = MatchDefaults.IsCompetitiveMatchType(match.MatchType);
            if (isCompetitive)
            {
                await _ratingService.CalculateAndApplyResults(match.Id);
            }

            match.MatchComplete = true;
            match.TimeModified = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            if (isCompetitive)
            {
                await _achievementService.UnlockForMatchAsync(match.Id, Input.NightId > 0 ? Input.NightId : null, User.Identity?.Name ?? "system");
            }

            // Store Location in TempData for the "One-Time" popup
            var section = match.FkBgdBoardGameNavigation?.BoardGameShelfSections.FirstOrDefault(x => !x.Inactive);
            if (section?.FkBgdShelfSectionNavigation != null)
            {
                var shelf = section.FkBgdShelfSectionNavigation.FkBgdShelfNavigation?.ShelfName ?? "Unknown Shelf";
                TempData["FlashShelfLocation"] = $"{shelf} — {section.FkBgdShelfSectionNavigation.SectionName}";
            }

            return RedirectToPage(new { id = Input.MatchId });
        }

        public async Task<IActionResult> OnPostUnlockAsync()
        {
            if (!User.IsInRole("Admin")) return Forbid();

            var match = await _db.BoardGameMatches
                .Include(m => m.BoardGameMatchPlayers)
                    .ThenInclude(mp => mp.BoardGameMatchPlayerResults)
                .FirstOrDefaultAsync(m => m.Id == Input.MatchId);

            if (match == null) return NotFound();
            if (!await CanAccessMatchAsync(match)) return Forbid();

            foreach (var mp in match.BoardGameMatchPlayers)
            {
                if (!mp.FkBgdPlayer.HasValue)
                {
                    continue;
                }

                foreach (var res in mp.BoardGameMatchPlayerResults.Where(r => !r.Inactive))
                {
                    if (res.RatingChangeMu.HasValue)
                    {
                        var rating = await _db.PlayerBoardGameRatings
                            .FirstOrDefaultAsync(r => r.FkBgdPlayer == mp.FkBgdPlayer.Value && r.FkBgdBoardGame == match.FkBgdBoardGame);

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

        public async Task<IActionResult> OnPostLinkGuestAsync(long id)
        {
            var matchPlayer = await _db.BoardGameMatchPlayers
                .Include(mp => mp.FkBgdBoardGameMatchNavigation)
                .FirstOrDefaultAsync(mp => mp.Id == LinkGuest.MatchPlayerId && !mp.Inactive);

            if (matchPlayer == null || matchPlayer.FkBgdBoardGameMatch != id)
            {
                return NotFound();
            }

            var match = matchPlayer.FkBgdBoardGameMatchNavigation;
            if (!await CanAccessMatchAsync(match)) return Forbid();

            if (match.MatchComplete == true)
            {
                TempData["StatusMessage"] = "Unlock the match before linking guests.";
                return RedirectToPage(new { id });
            }

            if (matchPlayer.FkBgdPlayer.HasValue)
            {
                TempData["StatusMessage"] = "That participant is already linked to a player.";
                return RedirectToPage(new { id });
            }

            var player = await GetLinkablePlayerQuery(match)
                .FirstOrDefaultAsync(p => p.Id == LinkGuest.PlayerId);

            if (player == null)
            {
                TempData["StatusMessage"] = "Choose a player from the current match scope.";
                return RedirectToPage(new { id });
            }

            var duplicateParticipant = await _db.BoardGameMatchPlayers.AnyAsync(mp =>
                !mp.Inactive &&
                mp.Id != matchPlayer.Id &&
                mp.FkBgdBoardGameMatch == match.Id &&
                mp.FkBgdPlayer == player.Id);

            if (duplicateParticipant)
            {
                TempData["StatusMessage"] = "That player is already in this match.";
                return RedirectToPage(new { id });
            }

            matchPlayer.FkBgdPlayer = player.Id;
            matchPlayer.TimeModified = DateTime.UtcNow;
            matchPlayer.ModifiedBy = User.Identity?.Name ?? "system";
            await _db.SaveChangesAsync();

            TempData["StatusMessage"] = $"Linked {matchPlayer.GuestName ?? "guest"} to {GetPlayerName(player)}.";
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostAddParticipantAsync(long id)
        {
            var match = await _db.BoardGameMatches
                .Include(m => m.FkBgdBoardGameNavigation)
                    .ThenInclude(bg => bg.FkBgdBoardGameVictoryConditionTypeNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (match == null) return NotFound();
            if (!await CanAccessMatchAsync(match)) return Forbid();

            if (match.MatchComplete == true)
            {
                TempData["StatusMessage"] = "Unlock the match before adding participants.";
                return RedirectToPage(new { id });
            }

            AddParticipant.GuestName = AddParticipant.GuestName?.Trim();
            var addingPlayer = AddParticipant.PlayerId.HasValue;
            var addingGuest = !string.IsNullOrWhiteSpace(AddParticipant.GuestName);

            if (addingPlayer == addingGuest)
            {
                TempData["StatusMessage"] = "Choose one roster player or enter one guest name.";
                return RedirectToPage(new { id });
            }

            var now = DateTime.UtcNow;
            var actor = User.Identity?.Name ?? "system";
            BoardGameMatchPlayer matchPlayer;

            if (addingPlayer)
            {
                var playerId = AddParticipant.PlayerId!.Value;
                var nightId = await GetNightIdForMatchAsync(id);
                var player = await GetAddablePlayerQuery(match, nightId)
                    .FirstOrDefaultAsync(p => p.Id == playerId);

                if (player == null)
                {
                    TempData["StatusMessage"] = "Choose a player from this match scope who is not already in the match.";
                    return RedirectToPage(new { id });
                }

                var alreadyInMatch = await _db.BoardGameMatchPlayers.AnyAsync(mp =>
                    !mp.Inactive &&
                    mp.FkBgdBoardGameMatch == id &&
                    mp.FkBgdPlayer == playerId);

                if (alreadyInMatch)
                {
                    TempData["StatusMessage"] = "That player is already in this match.";
                    return RedirectToPage(new { id });
                }

                matchPlayer = new BoardGameMatchPlayer
                {
                    Gid = Guid.NewGuid(),
                    FkBgdBoardGameMatch = id,
                    FkBgdPlayer = playerId,
                    TimeCreated = now,
                    TimeModified = now,
                    CreatedBy = actor,
                    ModifiedBy = actor
                };
            }
            else
            {
                matchPlayer = new BoardGameMatchPlayer
                {
                    Gid = Guid.NewGuid(),
                    FkBgdBoardGameMatch = id,
                    GuestName = AddParticipant.GuestName,
                    TimeCreated = now,
                    TimeModified = now,
                    CreatedBy = actor,
                    ModifiedBy = actor
                };
            }

            _db.BoardGameMatchPlayers.Add(matchPlayer);
            await _db.SaveChangesAsync();

            _db.BoardGameMatchPlayerResults.Add(new BoardGameMatchPlayerResult
            {
                Gid = Guid.NewGuid(),
                FkBgdBoardGameMatchPlayer = matchPlayer.Id,
                FkBgdResultType = GetDefaultResultTypeId(match),
                TimeCreated = now,
                TimeModified = now,
                CreatedBy = actor,
                ModifiedBy = actor
            });

            await _db.SaveChangesAsync();
            TempData["StatusMessage"] = "Participant added to the match.";
            return RedirectToPage(new { id });
        }

        private async Task<bool> CanAccessMatchAsync(BoardGameMatch match)
        {
            if (User.IsInRole("Admin"))
            {
                return true;
            }

            if (match.FkBgdClub.HasValue)
            {
                var currentClub = await _currentClubService.GetCurrentClubAsync();
                return currentClub.CurrentClubId == match.FkBgdClub.Value;
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return false;
            }

            return await _db.BoardGameMatchPlayers
                .AsNoTracking()
                .AnyAsync(mp => !mp.Inactive &&
                    mp.FkBgdBoardGameMatch == match.Id &&
                    mp.FkBgdPlayer.HasValue &&
                    mp.FkBgdPlayerNavigation.FkdboAspNetUsers == userId);
        }

        private async Task<List<SelectListItem>> LoadLinkablePlayersAsync(BoardGameMatch match)
        {
            var players = await GetLinkablePlayerQuery(match)
                .OrderBy(p => p.FirstName)
                .ThenBy(p => p.LastName)
                .Select(p => new
                {
                    p.Id,
                    Name = ((p.FirstName ?? string.Empty) + " " + (p.LastName ?? string.Empty)).Trim()
                })
                .ToListAsync();

            return players
                .Select(p => new SelectListItem(string.IsNullOrWhiteSpace(p.Name) ? $"Player {p.Id}" : p.Name, p.Id.ToString()))
                .ToList();
        }

        private async Task<List<SelectListItem>> LoadAddablePlayersAsync(BoardGameMatch match, long nightId, List<BoardGameMatchPlayer> matchPlayers)
        {
            var existingPlayerIds = matchPlayers
                .Where(mp => mp.FkBgdPlayer.HasValue)
                .Select(mp => mp.FkBgdPlayer!.Value)
                .ToHashSet();

            var players = await GetAddablePlayerQuery(match, nightId)
                .Where(p => !existingPlayerIds.Contains(p.Id))
                .OrderBy(p => p.FirstName)
                .ThenBy(p => p.LastName)
                .Select(p => new
                {
                    p.Id,
                    Name = ((p.FirstName ?? string.Empty) + " " + (p.LastName ?? string.Empty)).Trim()
                })
                .ToListAsync();

            return players
                .Select(p => new SelectListItem(string.IsNullOrWhiteSpace(p.Name) ? $"Player {p.Id}" : p.Name, p.Id.ToString()))
                .ToList();
        }

        private IQueryable<Player> GetAddablePlayerQuery(BoardGameMatch match, long nightId)
        {
            var query = _db.Players.Where(p => !p.Inactive);

            if (nightId > 0)
            {
                return query.Where(p => p.BoardGameNightPlayers.Any(np =>
                    !np.Inactive &&
                    np.FkBgdBoardGameNight == nightId));
            }

            if (match.FkBgdClub.HasValue)
            {
                var clubId = match.FkBgdClub.Value;
                return query.Where(p => p.PlayerClubs.Any(pc => !pc.Inactive && pc.FkBgdClub == clubId));
            }

            if (User.IsInRole("Admin"))
            {
                return query;
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return query.Where(p => !string.IsNullOrWhiteSpace(userId) && p.FkdboAspNetUsers == userId);
        }

        private IQueryable<Player> GetLinkablePlayerQuery(BoardGameMatch match)
        {
            var query = _db.Players.Where(p => !p.Inactive);

            if (User.IsInRole("Admin"))
            {
                return query;
            }

            if (match.FkBgdClub.HasValue)
            {
                var clubId = match.FkBgdClub.Value;
                return query.Where(p => p.PlayerClubs.Any(pc => !pc.Inactive && pc.FkBgdClub == clubId));
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return query.Where(p => !string.IsNullOrWhiteSpace(userId) && p.FkdboAspNetUsers == userId);
        }

        private static string GetPlayerName(Player player)
        {
            var name = $"{player.FirstName} {player.LastName}".Trim();
            return string.IsNullOrWhiteSpace(name) ? $"Player {player.Id}" : name;
        }

        private async Task<long> GetNightIdForMatchAsync(long matchId)
        {
            return await _db.BoardGameNightBoardGameMatches
                .AsNoTracking()
                .Where(x => x.FkBgdBoardGameMatch == matchId && !x.Inactive)
                .Select(x => x.FkBgdBoardGameNight)
                .FirstOrDefaultAsync();
        }

        private static long GetDefaultResultTypeId(BoardGameMatch match)
        {
            return match.FkBgdBoardGameNavigation?.FkBgdBoardGameVictoryConditionTypeNavigation?.TypeDesc == "Team Victory"
                ? 4
                : 2;
        }

        private static string GetParticipantName(BoardGameMatchPlayer matchPlayer)
        {
            var registeredName = $"{matchPlayer.FkBgdPlayerNavigation?.FirstName} {matchPlayer.FkBgdPlayerNavigation?.LastName}".Trim();
            if (!string.IsNullOrWhiteSpace(registeredName))
            {
                return registeredName;
            }

            if (!string.IsNullOrWhiteSpace(matchPlayer.GuestName))
            {
                return matchPlayer.GuestName;
            }

            if (!string.IsNullOrWhiteSpace(matchPlayer.InvitedEmail))
            {
                return matchPlayer.InvitedEmail;
            }

            return "Guest";
        }
    }
}

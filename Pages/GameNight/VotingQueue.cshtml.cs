using System.Security.Claims;
using Board_Game_Software.Models;
using Board_Game_Software.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Board_Game_Software.Pages.GameNight
{
    public class VotingQueueModel : PageModel
    {
        private readonly BoardGameDbContext _db;
        private readonly BoardGamePlayabilityService _playabilityService;
        private readonly ICurrentClubService _currentClubService;

        public VotingQueueModel(
            BoardGameDbContext db,
            BoardGamePlayabilityService playabilityService,
            ICurrentClubService currentClubService)
        {
            _db = db;
            _playabilityService = playabilityService;
            _currentClubService = currentClubService;
        }

        public BoardGameNight Night { get; private set; } = null!;
        public long? CurrentPlayerId { get; private set; }
        public List<VoteGameRow> Games { get; private set; } = new();

        [TempData]
        public string? Message { get; set; }

        public sealed class VoteGameRow
        {
            public long GameId { get; init; }
            public Guid GameGid { get; init; }
            public string Name { get; init; } = string.Empty;
            public string CoverUrl { get; init; } = "/images/default-cover.png";
            public byte? MinPlayers { get; init; }
            public byte? MaxPlayers { get; init; }
            public byte? MaxMinutes { get; init; }
            public bool UsesExpansionPlayerCount { get; init; }
            public string ShelfLocation { get; init; } = string.Empty;
            public int VoteCount { get; init; }
            public bool CurrentPlayerVoted { get; init; }
            public bool ValidPlayerCount { get; init; }
            public string VoterNames { get; init; } = string.Empty;
        }

        public sealed class VoteState
        {
            public bool Voted { get; init; }
            public int Count { get; init; }
            public string VoterNames { get; init; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync(long id)
        {
            var loaded = await LoadAsync(id);
            return loaded ? Page() : NotFound();
        }

        public async Task<IActionResult> OnPostVoteAsync(long id, long boardGameId)
        {
            if (!await LoadCurrentPlayerAsync(id)) return Forbid();
            if (!await CanUseGameForNightAsync(id, boardGameId)) return Forbid();
            var currentPlayerId = CurrentPlayerId!.Value;

            var existing = await _db.BoardGameVotes
                .FirstOrDefaultAsync(v => v.FkBgdBoardGameNight == id
                    && v.FkBgdBoardGame == boardGameId
                    && v.FkBgdPlayer == currentPlayerId);

            var actor = User.Identity?.Name ?? "system";
            if (existing == null)
            {
                _db.BoardGameVotes.Add(new BoardGameVote
                {
                    Gid = Guid.NewGuid(),
                    FkBgdBoardGameNight = id,
                    FkBgdBoardGame = boardGameId,
                    FkBgdPlayer = currentPlayerId,
                    CreatedBy = actor,
                    ModifiedBy = actor,
                    TimeCreated = DateTime.UtcNow,
                    TimeModified = DateTime.UtcNow
                });
            }
            else
            {
                existing.Inactive = false;
                existing.ModifiedBy = actor;
                existing.TimeModified = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            if (WantsJson())
            {
                var state = await GetVoteStateAsync(id, boardGameId, currentPlayerId);
                return new JsonResult(state);
            }

            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostUnvoteAsync(long id, long boardGameId)
        {
            if (!await LoadCurrentPlayerAsync(id)) return Forbid();
            if (!await CanUseGameForNightAsync(id, boardGameId)) return Forbid();
            var currentPlayerId = CurrentPlayerId!.Value;

            var existing = await _db.BoardGameVotes
                .FirstOrDefaultAsync(v => v.FkBgdBoardGameNight == id
                    && v.FkBgdBoardGame == boardGameId
                    && v.FkBgdPlayer == currentPlayerId
                    && !v.Inactive);

            if (existing != null)
            {
                existing.Inactive = true;
                existing.ModifiedBy = User.Identity?.Name ?? "system";
                existing.TimeModified = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            if (WantsJson())
            {
                var state = await GetVoteStateAsync(id, boardGameId, currentPlayerId);
                return new JsonResult(state);
            }

            return RedirectToPage(new { id });
        }

        private bool WantsJson()
        {
            return string.Equals(Request.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase)
                || Request.Headers.Accept.Any(value => value?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true);
        }

        private async Task<VoteState> GetVoteStateAsync(long nightId, long boardGameId, long currentPlayerId)
        {
            var rows = await _db.BoardGameVotes.AsNoTracking()
                .Where(v => v.FkBgdBoardGameNight == nightId
                    && v.FkBgdBoardGame == boardGameId
                    && !v.Inactive)
                .Select(v => new
                {
                    v.FkBgdPlayer,
                    PlayerName = (v.FkBgdPlayerNavigation.FirstName + " " + v.FkBgdPlayerNavigation.LastName).Trim()
                })
                .ToListAsync();

            return new VoteState
            {
                Voted = rows.Any(v => v.FkBgdPlayer == currentPlayerId),
                Count = rows.Count,
                VoterNames = string.Join(", ", rows.Select(v => v.PlayerName).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().Take(5))
            };
        }

        private async Task<bool> LoadAsync(long id)
        {
            Night = await _db.BoardGameNights.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id) ?? null!;
            if (Night == null) return false;
            if (!await CanAccessNightAsync(Night)) return false;

            await LoadCurrentPlayerAsync(id);

            var playerIds = await _db.BoardGameNightPlayers.AsNoTracking()
                .Where(np => np.FkBgdBoardGameNight == id && !np.Inactive)
                .Select(np => np.FkBgdPlayer)
                .ToListAsync();
            var playerCount = playerIds.Count;

            var voteRows = await _db.BoardGameVotes.AsNoTracking()
                .Where(v => v.FkBgdBoardGameNight == id && !v.Inactive)
                .Select(v => new
                {
                    v.FkBgdBoardGame,
                    v.FkBgdPlayer,
                    PlayerName = (v.FkBgdPlayerNavigation.FirstName + " " + v.FkBgdPlayerNavigation.LastName).Trim()
                })
                .ToListAsync();

            var votesByGame = voteRows
                .GroupBy(v => v.FkBgdBoardGame)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        Count = g.Count(),
                        CurrentVoted = CurrentPlayerId.HasValue && g.Any(v => v.FkBgdPlayer == CurrentPlayerId.Value),
                        Names = string.Join(", ", g.Select(v => v.PlayerName).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().Take(5))
                    });

            var playableGames = await _playabilityService.GetPlayableBaseGamesAsync(Night.FkBgdClub);
            var playableIds = playableGames.Select(g => g.Id).ToList();
            var shelfLocations = await _db.BoardGames.AsNoTracking()
                .Where(bg => playableIds.Contains(bg.Id))
                .Select(bg => new
                {
                    bg.Id,
                    ShelfLocation = bg.BoardGameShelfSections
                        .Where(link => !link.Inactive)
                        .Select(link => link.FkBgdShelfSectionNavigation.FkBgdShelfNavigation.ShelfName + " / " + link.FkBgdShelfSectionNavigation.SectionName)
                        .FirstOrDefault()
                })
                .ToDictionaryAsync(x => x.Id, x => x.ShelfLocation ?? string.Empty);

            Games = playableGames
                .Select(bg =>
                {
                    votesByGame.TryGetValue(bg.Id, out var votes);
                    shelfLocations.TryGetValue(bg.Id, out var shelfLocation);

                    return new VoteGameRow
                    {
                        GameId = bg.Id,
                        GameGid = bg.Gid,
                        Name = bg.Name,
                        CoverUrl = $"/media/boardgame/front/{bg.Gid}",
                        MinPlayers = bg.MinPlayers,
                        MaxPlayers = bg.MaxPlayers,
                        MaxMinutes = bg.MaxMinutes,
                        UsesExpansionPlayerCount = bg.UsesExpansionPlayerCount,
                        ShelfLocation = shelfLocation ?? string.Empty,
                        VoteCount = votes?.Count ?? 0,
                        CurrentPlayerVoted = votes?.CurrentVoted ?? false,
                        ValidPlayerCount = (!bg.MinPlayers.HasValue || bg.MinPlayers.Value <= playerCount)
                            && (!bg.MaxPlayers.HasValue || bg.MaxPlayers.Value >= playerCount),
                        VoterNames = votes?.Names ?? string.Empty
                    };
                })
                .OrderByDescending(g => g.ValidPlayerCount)
                .ThenByDescending(g => g.VoteCount)
                .ThenBy(g => g.Name)
                .ToList();

            return true;
        }
        private async Task<bool> LoadCurrentPlayerAsync(long nightId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return false;

            CurrentPlayerId = await _db.Players.AsNoTracking()
                .Where(p => p.FkdboAspNetUsers == userId)
                .Select(p => (long?)p.Id)
                .FirstOrDefaultAsync();

            if (!CurrentPlayerId.HasValue) return false;

            return await _db.BoardGameNightPlayers.AsNoTracking()
                .AnyAsync(np => np.FkBgdBoardGameNight == nightId
                    && np.FkBgdPlayer == CurrentPlayerId.Value
                    && !np.Inactive);
        }

        private async Task<bool> CanAccessNightAsync(BoardGameNight night)
        {
            if (User.IsInRole("Admin")) return true;

            var currentClub = await _currentClubService.GetCurrentClubAsync();
            return night.FkBgdClub.HasValue && night.FkBgdClub == currentClub.CurrentClubId;
        }

        private async Task<bool> CanUseGameForNightAsync(long nightId, long boardGameId)
        {
            var nightClubId = await _db.BoardGameNights.AsNoTracking()
                .Where(n => n.Id == nightId)
                .Select(n => n.FkBgdClub)
                .FirstOrDefaultAsync();

            return await _db.BoardGames.AsNoTracking()
                .AnyAsync(g => g.Id == boardGameId
                    && !g.Inactive
                    && (!nightClubId.HasValue || g.FkBgdClub == nightClubId.Value));
        }
    }
}

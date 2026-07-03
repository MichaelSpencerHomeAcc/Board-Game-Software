using Board_Game_Software.Data;
using Board_Game_Software.Models;
using Board_Game_Software.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Board_Game_Software.Pages.DataSetup.Players
{
    public class ManageTopTenModel : PageModel
    {
        private readonly BoardGameDbContext _context;
        private readonly ICurrentClubService _currentClubService;

        public ManageTopTenModel(BoardGameDbContext context, ICurrentClubService currentClubService)
        {
            _context = context;
            _currentClubService = currentClubService;
        }

        public Player Player { get; set; } = null!;
        public List<PlayerBoardGame> CurrentTopTen { get; set; } = new();

        // entryId -> image url (NOT base64)
        public Dictionary<long, string> GameImages { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(long id)
        {
            var player = await _context.Players.FindAsync(id);
            if (player == null) return NotFound();
            if (!await CanManagePlayerAsync(id)) return Forbid();

            Player = player;
            CurrentTopTen = await GetVisibleTopTenEntriesAsync(id);

            GameImages.Clear();

            foreach (var item in CurrentTopTen)
            {
                if (item.BoardGame != null)
                {
                    // Use your media endpoint (fast + cache headers)
                    GameImages[item.Id] = $"/media/boardgame/front/{item.BoardGame.Gid}";
                }
            }

            return Page();
        }

        // Return up to 500 games
        public async Task<JsonResult> OnGetSearchGamesAsync(long id, string term)
        {
            var currentClub = await _currentClubService.GetCurrentClubAsync();
            if (!await CanManagePlayerAsync(id))
            {
                return new JsonResult(new { games = Array.Empty<object>() });
            }

            var playerClubIds = await GetPlayerClubIdsAsync(id);
            var query = _context.BoardGames
                .AsNoTracking()
                .Include(bg => bg.FkBgdClubNavigation)
                .Where(bg => !bg.Inactive);

            if (playerClubIds.Count > 0)
            {
                query = query.Where(bg => bg.FkBgdClub.HasValue && playerClubIds.Contains(bg.FkBgdClub.Value));
            }
            else if (User.IsInRole("Admin") && currentClub.IsPlatformAdminMode)
            {
                query = query.Where(bg => bg.FkBgdClub == null);
            }
            else
            {
                query = query.Where(bg => false);
            }

            if (!string.IsNullOrWhiteSpace(term))
                query = query.Where(bg => bg.BoardGameName.Contains(term));

            var candidateGames = await query
                .OrderBy(bg => bg.BoardGameName)
                .Take(500)
                .ToListAsync();

            var gamesList = DeduplicateGames(candidateGames, currentClub.CurrentClubId)
                .Take(500)
                .Select(bg => new {
                    id = bg.Id,
                    gid = bg.Gid, // needed for thumbnail URL
                    boardGameName = bg.BoardGameName,
                    releaseYear = bg.ReleaseDate.HasValue ? bg.ReleaseDate.Value.Year.ToString() : "N/A",
                    clubName = bg.FkBgdClubNavigation?.ClubName ?? "Template"
                })
                .ToList();

            return new JsonResult(new { games = gamesList });
        }

        public async Task<IActionResult> OnPostAddGameAsync(long id, long boardGameId)
        {
            if (!await CanManagePlayerAsync(id)) return Forbid();

            if (!await CanUseGameForPlayerAsync(id, boardGameId))
            {
                return Forbid();
            }

            if (await PlayerAlreadyHasGameFamilyAsync(id, boardGameId))
            {
                return RedirectToPage(new { id });
            }

            var currentCount = await _context.PlayerBoardGames.CountAsync(x => x.FkBgdPlayer == id && !x.Inactive);
            if (currentCount >= 10) return RedirectToPage(new { id });

            var userName = User.Identity?.Name ?? "System";

            var newEntry = new PlayerBoardGame
            {
                FkBgdPlayer = id,
                FkBgdBoardGame = boardGameId,
                Rank = (short)(currentCount + 1),
                Gid = Guid.NewGuid(),
                Inactive = false,
                CreatedBy = userName,
                ModifiedBy = userName,
                TimeCreated = DateTime.Now,
                TimeModified = DateTime.Now
            };

            _context.PlayerBoardGames.Add(newEntry);
            await _context.SaveChangesAsync();

            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostRemoveAsync(long id, long entryId)
        {
            if (!await CanManagePlayerAsync(id)) return Forbid();

            var entry = await _context.PlayerBoardGames
                .Include(pbg => pbg.BoardGame)
                .FirstOrDefaultAsync(pbg => pbg.Id == entryId && pbg.FkBgdPlayer == id);
            if (entry != null)
            {
                entry.Inactive = true;
                entry.ModifiedBy = User.Identity?.Name ?? "System";
                entry.TimeModified = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            return RedirectToPage(new { id });
        }

        public async Task<JsonResult> OnPostUpdateOrderAsync([FromBody] List<RankUpdate> updates)
        {
            if (updates == null) return new JsonResult(new { success = false });

            var userName = User.Identity?.Name ?? "System";
            foreach (var update in updates)
            {
                var entry = await _context.PlayerBoardGames
                    .FirstOrDefaultAsync(pbg => pbg.Id == update.Id);
                if (entry != null)
                {
                    if (!await CanManagePlayerAsync(entry.FkBgdPlayer ?? 0))
                    {
                        return new JsonResult(new { success = false });
                    }

                    entry.Rank = update.Rank;
                    entry.ModifiedBy = userName;
                    entry.TimeModified = DateTime.Now;
                }
            }

            await _context.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        public class RankUpdate
        {
            public long Id { get; set; }
            public short Rank { get; set; }
        }

        private async Task<bool> CanUseGameForPlayerAsync(long playerId, long boardGameId)
        {
            var currentClub = await _currentClubService.GetCurrentClubAsync();
            var playerClubIds = await GetPlayerClubIdsAsync(playerId);

            return await _context.BoardGames.AsNoTracking()
                .AnyAsync(bg => bg.Id == boardGameId
                    && !bg.Inactive
                    && ((bg.FkBgdClub.HasValue && playerClubIds.Contains(bg.FkBgdClub.Value))
                        || (playerClubIds.Count == 0 && User.IsInRole("Admin") && currentClub.IsPlatformAdminMode && bg.FkBgdClub == null)));
        }

        private async Task<List<PlayerBoardGame>> GetVisibleTopTenEntriesAsync(long playerId)
        {
            var currentClub = await _currentClubService.GetCurrentClubAsync();
            var playerClubIds = await GetPlayerClubIdsAsync(playerId);

            var entries = await _context.PlayerBoardGames
                .Include(pbg => pbg.BoardGame)
                .ThenInclude(bg => bg!.FkBgdClubNavigation)
                .Where(pbg => pbg.FkBgdPlayer == playerId
                    && !pbg.Inactive
                    && pbg.BoardGame != null
                    && ((pbg.BoardGame.FkBgdClub.HasValue && playerClubIds.Contains(pbg.BoardGame.FkBgdClub.Value))
                        || (playerClubIds.Count == 0 && User.IsInRole("Admin") && currentClub.IsPlatformAdminMode && pbg.BoardGame.FkBgdClub == null)))
                .OrderBy(pbg => pbg.Rank)
                .ToListAsync();

            var seenTemplateIds = new HashSet<long>();
            var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var visibleEntries = new List<PlayerBoardGame>();

            foreach (var entry in entries)
            {
                if (entry.BoardGame == null) continue;

                var templateKey = GetTemplateFamilyKey(entry.BoardGame);
                var nameKey = NormalizeGameName(entry.BoardGame.BoardGameName);

                if (templateKey.HasValue && seenTemplateIds.Contains(templateKey.Value)) continue;
                if (!string.IsNullOrWhiteSpace(nameKey) && seenNames.Contains(nameKey)) continue;

                if (templateKey.HasValue) seenTemplateIds.Add(templateKey.Value);
                if (!string.IsNullOrWhiteSpace(nameKey)) seenNames.Add(nameKey);

                visibleEntries.Add(entry);
            }

            return visibleEntries;
        }

        private IEnumerable<BoardGame> DeduplicateGames(List<BoardGame> games, long? preferredClubId)
        {
            var seenTemplateIds = new HashSet<long>();
            var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var game in games
                .OrderByDescending(bg => preferredClubId.HasValue && bg.FkBgdClub == preferredClubId.Value)
                .ThenByDescending(bg => bg.FkBgdClub.HasValue)
                .ThenBy(bg => bg.BoardGameName))
            {
                var templateKey = GetTemplateFamilyKey(game);
                var nameKey = NormalizeGameName(game.BoardGameName);

                if (templateKey.HasValue && seenTemplateIds.Contains(templateKey.Value)) continue;
                if (!string.IsNullOrWhiteSpace(nameKey) && seenNames.Contains(nameKey)) continue;

                if (templateKey.HasValue) seenTemplateIds.Add(templateKey.Value);
                if (!string.IsNullOrWhiteSpace(nameKey)) seenNames.Add(nameKey);

                yield return game;
            }
        }

        private async Task<bool> PlayerAlreadyHasGameFamilyAsync(long playerId, long boardGameId)
        {
            var boardGame = await _context.BoardGames.AsNoTracking()
                .FirstOrDefaultAsync(bg => bg.Id == boardGameId && !bg.Inactive);
            if (boardGame == null) return false;

            var templateKey = GetTemplateFamilyKey(boardGame);
            var nameKey = NormalizeGameName(boardGame.BoardGameName);

            var entries = await _context.PlayerBoardGames
                .AsNoTracking()
                .Include(pbg => pbg.BoardGame)
                .Where(pbg => pbg.FkBgdPlayer == playerId && !pbg.Inactive && pbg.BoardGame != null)
                .ToListAsync();

            return entries.Any(entry =>
            {
                if (entry.BoardGame == null) return false;

                var entryTemplateKey = GetTemplateFamilyKey(entry.BoardGame);
                if (templateKey.HasValue && entryTemplateKey.HasValue && templateKey.Value == entryTemplateKey.Value)
                {
                    return true;
                }

                return string.Equals(nameKey, NormalizeGameName(entry.BoardGame.BoardGameName), StringComparison.OrdinalIgnoreCase);
            });
        }

        private async Task<HashSet<long>> GetPlayerClubIdsAsync(long playerId)
        {
            var clubIds = await _context.PlayerClubs
                .AsNoTracking()
                .Where(pc => !pc.Inactive && pc.FkBgdPlayer == playerId)
                .Select(pc => pc.FkBgdClub)
                .ToListAsync();

            var legacyClubId = await _context.Players
                .AsNoTracking()
                .Where(p => p.Id == playerId && p.FkBgdClub.HasValue)
                .Select(p => p.FkBgdClub)
                .FirstOrDefaultAsync();

            if (legacyClubId.HasValue)
            {
                clubIds.Add(legacyClubId.Value);
            }

            return clubIds.ToHashSet();
        }

        private static long? GetTemplateFamilyKey(BoardGame boardGame)
        {
            return boardGame.FkBgdTemplateBoardGame ?? (boardGame.FkBgdClub == null ? boardGame.Id : null);
        }

        private static string NormalizeGameName(string? boardGameName)
        {
            return (boardGameName ?? string.Empty).Trim().ToUpperInvariant();
        }

        private async Task<bool> CanManagePlayerAsync(long playerId)
        {
            if (playerId <= 0) return false;

            var currentClub = await _currentClubService.GetCurrentClubAsync();
            if (User.IsInRole("Admin") && currentClub.IsPlatformAdminMode) return true;

            if (!currentClub.CurrentClubId.HasValue) return false;
            var currentClubId = currentClub.CurrentClubId.Value;

            if (User.IsInRole("Admin"))
            {
                return await PlayerBelongsToClubAsync(playerId, currentClubId);
            }

            if (!currentClub.CanManageCurrentClub) return false;

            return await PlayerBelongsToClubAsync(playerId, currentClubId);
        }

        private async Task<bool> PlayerBelongsToClubAsync(long playerId, long clubId)
        {
            return await _context.PlayerClubs
                    .AsNoTracking()
                    .AnyAsync(pc => !pc.Inactive && pc.FkBgdPlayer == playerId && pc.FkBgdClub == clubId)
                || await _context.Players
                    .AsNoTracking()
                    .AnyAsync(p => p.Id == playerId && p.FkBgdClub == clubId);
        }
    }
}

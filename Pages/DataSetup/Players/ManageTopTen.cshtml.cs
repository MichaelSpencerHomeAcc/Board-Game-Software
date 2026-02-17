using Board_Game_Software.Data;
using Board_Game_Software.Models;
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

        public ManageTopTenModel(BoardGameDbContext context)
        {
            _context = context;
        }

        public Player Player { get; set; } = null!;
        public List<PlayerBoardGame> CurrentTopTen { get; set; } = new();

        // entryId -> image url (NOT base64)
        public Dictionary<long, string> GameImages { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(long id)
        {
            Player = await _context.Players.FindAsync(id);
            if (Player == null) return NotFound();

            CurrentTopTen = await _context.PlayerBoardGames
                .Include(pbg => pbg.BoardGame)
                .Where(pbg => pbg.FkBgdPlayer == id && !pbg.Inactive)
                .OrderBy(pbg => pbg.Rank)
                .ToListAsync();

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
        public async Task<JsonResult> OnGetSearchGamesAsync(string term)
        {
            var query = _context.BoardGames.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(term))
                query = query.Where(bg => bg.BoardGameName.Contains(term));

            var gamesList = await query
                .OrderBy(bg => bg.BoardGameName)
                .Take(500)
                .Select(bg => new {
                    id = bg.Id,
                    gid = bg.Gid, // needed for thumbnail URL
                    boardGameName = bg.BoardGameName,
                    releaseYear = bg.ReleaseDate.HasValue ? bg.ReleaseDate.Value.Year.ToString() : "N/A"
                })
                .ToListAsync();

            return new JsonResult(new { games = gamesList });
        }

        public async Task<IActionResult> OnPostAddGameAsync(long id, long boardGameId)
        {
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
            var entry = await _context.PlayerBoardGames.FindAsync(entryId);
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
                var entry = await _context.PlayerBoardGames.FindAsync(update.Id);
                if (entry != null)
                {
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
    }
}

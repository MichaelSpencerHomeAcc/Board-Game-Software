using Board_Game_Software.Data;
using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Board_Game_Software.Pages.DataSetup.Players
{
    public class ManageTopTenModel : PageModel
    {
        private readonly BoardGameDbContext _context;
        private readonly IMongoCollection<BoardGameImages> _boardGameImages;

        public ManageTopTenModel(BoardGameDbContext context, IMongoClient mongoClient, IConfiguration configuration)
        {
            _context = context;
            var databaseName = configuration["MongoDbSettings:Database"];
            var database = mongoClient.GetDatabase(databaseName);
            _boardGameImages = database.GetCollection<BoardGameImages>("BoardGameImages");
        }

        public Player Player { get; set; } = null!;
        public List<PlayerBoardGame> CurrentTopTen { get; set; } = new();
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

            if (CurrentTopTen.Any())
            {
                var frontImageType = await _context.BoardGameImageTypes
                    .FirstOrDefaultAsync(bgit => bgit.TypeDesc == "Board Game Front");

                if (frontImageType != null)
                {
                    // Create a concrete list of strings to avoid Guid? issues in the Mongo query
                    var gidStrings = CurrentTopTen
                        .Where(x => x.BoardGame != null)
                        .Select(x => x.BoardGame.Gid.ToString())
                        .ToList();

                    var images = await _boardGameImages.Find(img =>
                        gidStrings.Contains(img.GID.ToString()) &&
                        img.ImageTypeGID == frontImageType.Gid)
                        .ToListAsync();

                    foreach (var item in CurrentTopTen)
                    {
                        var itemGidString = item.BoardGame.Gid.ToString();
                        var img = images.FirstOrDefault(x => x.GID.ToString() == itemGidString);

                        if (img?.ImageBytes != null)
                        {
                            GameImages[item.Id] = $"data:{img.ContentType};base64,{Convert.ToBase64String(img.ImageBytes)}";
                        }
                    }
                }
            }
            return Page();
        }

        // Return up to 500 games to avoid the need for broken pagination buttons
        public async Task<JsonResult> OnGetSearchGamesAsync(string term)
        {
            var query = _context.BoardGames.AsQueryable();

            if (!string.IsNullOrWhiteSpace(term))
                query = query.Where(bg => bg.BoardGameName.Contains(term));

            var gamesList = await query
                .OrderBy(bg => bg.BoardGameName)
                .Take(500)
                .Select(bg => new {
                    id = bg.Id,
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

        public class RankUpdate { public long Id { get; set; } public short Rank { get; set; } }
    }
}
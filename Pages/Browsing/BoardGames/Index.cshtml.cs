using Board_Game_Software.Data;
using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Board_Game_Software.Pages.Browsing.BoardGames
{
    public class IndexModel : PageModel
    {
        private readonly BoardGameDbContext _context;
        private readonly IMongoCollection<BoardGameImages> _boardGameImages;

        public IndexModel(BoardGameDbContext context, IMongoClient mongoClient, IConfiguration configuration)
        {
            _context = context;

            var databaseName = configuration["MongoDbSettings:Database"];
            var database = mongoClient.GetDatabase(databaseName);
            _boardGameImages = database.GetCollection<BoardGameImages>("BoardGameImages");
        }

        public List<BoardGameViewModel> BoardGames { get; set; } = new();

        public string SearchTerm { get; set; } = string.Empty;

        public async Task OnGetAsync(string? search)
        {
            SearchTerm = search ?? string.Empty;

            var query = _context.BoardGames
                .Include(bg => bg.FkBgdBoardGameTypeNavigation)
                .Where(bg => !bg.Inactive);

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                bool isNumber = int.TryParse(SearchTerm, out int playerCount);

                if (isNumber)
                {
                    query = query.Where(bg =>
                        (bg.PlayerCountMin <= playerCount && playerCount <= bg.PlayerCountMax)
                        || bg.BoardGameName.Contains(SearchTerm)
                        || bg.FkBgdBoardGameTypeNavigation.TypeDesc.Contains(SearchTerm)
                    );
                }
                else
                {
                    query = query.Where(bg =>
                        bg.BoardGameName.Contains(SearchTerm)
                        || bg.FkBgdBoardGameTypeNavigation.TypeDesc.Contains(SearchTerm));
                }
            }

            var games = await query
                .OrderBy(bg => bg.BoardGameName)
                .Take(50)
                .ToListAsync();

            var frontImageType = await _context.BoardGameImageTypes.FirstOrDefaultAsync(t => t.TypeDesc == "Board Game Front");

            BoardGames = new List<BoardGameViewModel>();

            if (frontImageType != null && games.Any())
            {
                // Convert GIDs to strings for Mongo query
                var gids = games.Select(g => g.Gid.ToString()).ToList();
                var imageTypeGidString = frontImageType.Gid.ToString();

                var filter = Builders<BoardGameImages>.Filter.And(
                    Builders<BoardGameImages>.Filter.Eq(x => x.SQLTable, "bgd.BoardGame"),
                    Builders<BoardGameImages>.Filter.In("GID", gids),
                    Builders<BoardGameImages>.Filter.Eq("ImageTypeGID", imageTypeGidString)
                );

                var imageDocs = await _boardGameImages.Find(filter).ToListAsync();

                var imagesDict = imageDocs
                    .Where(img => img.ImageBytes != null && img.GID.HasValue)
                    .ToDictionary(img => img.GID.Value.ToString(), img => $"data:{img.ContentType};base64,{Convert.ToBase64String(img.ImageBytes)}");

                foreach (var game in games)
                {
                    var gidStr = game.Gid.ToString();

                    BoardGames.Add(new BoardGameViewModel
                    {
                        BoardGame = game,
                        Base64Image = imagesDict.ContainsKey(gidStr) ? imagesDict[gidStr] : null
                    });
                }
            }
            else
            {
                // No images or no games
                foreach (var game in games)
                {
                    BoardGames.Add(new BoardGameViewModel
                    {
                        BoardGame = game,
                        Base64Image = null
                    });
                }
            }
        }

        public async Task<JsonResult> OnGetSearchAsync(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return new JsonResult(new List<object>());
            }

            bool isNumber = int.TryParse(term, out int playerCount);

            var suggestionsQuery = _context.BoardGames
                .Include(bg => bg.FkBgdBoardGameTypeNavigation)
                .Where(bg => !bg.Inactive);

            if (isNumber)
            {
                suggestionsQuery = suggestionsQuery.Where(bg =>
                    (bg.PlayerCountMin <= playerCount && playerCount <= bg.PlayerCountMax)
                    || bg.BoardGameName.Contains(term)
                    || bg.FkBgdBoardGameTypeNavigation.TypeDesc.Contains(term)
                );
            }
            else
            {
                suggestionsQuery = suggestionsQuery.Where(bg =>
                    bg.BoardGameName.Contains(term) ||
                    bg.FkBgdBoardGameTypeNavigation.TypeDesc.Contains(term));
            }

            var suggestions = await suggestionsQuery
                .OrderBy(bg => bg.BoardGameName)
                .Select(bg => new
                {
                    name = bg.BoardGameName,
                    type = bg.FkBgdBoardGameTypeNavigation.TypeDesc
                })
                .Take(10)
                .ToListAsync();

            return new JsonResult(suggestions);
        }
    }
}

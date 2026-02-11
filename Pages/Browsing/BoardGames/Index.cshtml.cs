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

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; } = string.Empty;

        public async Task OnGetAsync(string? search)
        {
            SearchTerm = search ?? string.Empty;

            // 1. Build Query (Includes Publisher for the new UI)
            var query = _context.BoardGames
                .Include(bg => bg.FkBgdPublisherNavigation)
                .Include(bg => bg.FkBgdBoardGameTypeNavigation)
                .Where(bg => !bg.Inactive);

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var cleanSearch = SearchTerm.Replace("min", "", StringComparison.OrdinalIgnoreCase).Trim();
                bool isNumber = int.TryParse(cleanSearch, out int numericValue);

                if (isNumber)
                {
                    query = query.Where(bg =>
                        (bg.PlayerCountMin <= numericValue && numericValue <= bg.PlayerCountMax)
                        || (bg.PlayingTimeMinInMinutes <= numericValue && numericValue <= bg.PlayingTimeMaxInMinutes)
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

            BoardGames = new List<BoardGameViewModel>();

            if (games.Any())
            {
                // 2. Fetch Images (Logic matched to BoardGameDetails)
                Dictionary<Guid, string> imagesDict = new();

                var frontImageType = await _context.BoardGameImageTypes
                    .FirstOrDefaultAsync(t => t.TypeDesc == "Board Game Front");

                if (frontImageType != null)
                {
                    // FIX: Explicit cast to (Guid?) is required for Mongo Filter
                    var guids = games.Select(g => (Guid?)g.Gid).ToList();
                    var typeId = frontImageType.Gid;

                    // Exact logic from Details page: Match GID AND ImageTypeGID
                    var filter = Builders<BoardGameImages>.Filter.And(
                        Builders<BoardGameImages>.Filter.In(x => x.GID, guids),
                        Builders<BoardGameImages>.Filter.Eq(x => x.ImageTypeGID, typeId)
                    );

                    var images = await _boardGameImages.Find(filter).ToListAsync();

                    foreach (var img in images)
                    {
                        if (img.GID.HasValue && img.ImageBytes != null)
                        {
                            if (!imagesDict.ContainsKey(img.GID.Value))
                            {
                                var contentType = !string.IsNullOrEmpty(img.ContentType) ? img.ContentType : "image/png";
                                imagesDict[img.GID.Value] = $"data:{contentType};base64,{Convert.ToBase64String(img.ImageBytes)}";
                            }
                        }
                    }
                }

                foreach (var game in games)
                {
                    BoardGames.Add(new BoardGameViewModel
                    {
                        BoardGame = game,
                        Base64Image = imagesDict.ContainsKey(game.Gid) ? imagesDict[game.Gid] : null
                    });
                }
            }
        }

        public async Task<JsonResult> OnGetSearchAsync(string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return new JsonResult(new List<object>());

            var cleanSearch = term.Replace("min", "", StringComparison.OrdinalIgnoreCase).Trim();
            bool isNumber = int.TryParse(cleanSearch, out int numericValue);

            var query = _context.BoardGames
                .Include(bg => bg.FkBgdBoardGameTypeNavigation)
                .Where(bg => !bg.Inactive);

            if (isNumber)
            {
                query = query.Where(bg =>
                    (bg.PlayerCountMin <= numericValue && numericValue <= bg.PlayerCountMax)
                    || (bg.PlayingTimeMinInMinutes <= numericValue && numericValue <= bg.PlayingTimeMaxInMinutes)
                    || bg.BoardGameName.Contains(term)
                    || bg.FkBgdBoardGameTypeNavigation.TypeDesc.Contains(term)
                );
            }
            else
            {
                query = query.Where(bg =>
                    bg.BoardGameName.Contains(term) ||
                    bg.FkBgdBoardGameTypeNavigation.TypeDesc.Contains(term));
            }

            var suggestions = await query
                .OrderBy(bg => bg.BoardGameName)
                .Select(bg => new { name = bg.BoardGameName, type = bg.FkBgdBoardGameTypeNavigation.TypeDesc })
                .Take(10)
                .ToListAsync();

            return new JsonResult(suggestions);
        }
    }

    public class BoardGameViewModel
    {
        public BoardGame BoardGame { get; set; }
        public string? Base64Image { get; set; }
    }
}
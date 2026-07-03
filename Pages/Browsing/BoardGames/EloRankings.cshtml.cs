using Board_Game_Software.Models;
using Board_Game_Software.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

namespace Board_Game_Software.Pages.Browsing.BoardGames;

public class EloRankingsModel : PageModel
{
    private readonly BoardGameDbContext _context;
    private readonly IMongoCollection<BoardGameImages> _imagesCollection;
    private readonly ICurrentClubService _currentClubService;

    public EloRankingsModel(
        BoardGameDbContext context,
        IMongoClient mongoClient,
        IConfiguration configuration,
        ICurrentClubService currentClubService)
    {
        _context = context;
        _currentClubService = currentClubService;
        var databaseName = configuration["MongoDbSettings:Database"];
        var database = mongoClient.GetDatabase(databaseName);
        _imagesCollection = database.GetCollection<BoardGameImages>("BoardGameImages");
    }

    public BoardGame BoardGame { get; set; } = default!;
    public string BoardGameFrontImageUrl { get; set; } = string.Empty;
    public IList<VwEloRanking> PlayerRankings { get; set; } = default!;
    public Dictionary<long, string> PlayerImages { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(long? id)
    {
        if (id == null) return NotFound();

        var currentClub = await _currentClubService.GetCurrentClubAsync();
        var boardGame = await _context.BoardGames.FirstOrDefaultAsync(m => m.Id == id);
        if (boardGame == null) return NotFound();
        if (!CanViewGame(boardGame, currentClub)) return Forbid();

        BoardGame = boardGame;

        PlayerRankings = await _context.VwEloRankings
            .Where(r => r.FkBgdBoardGame == id && !r.Inactive)
            .OrderBy(r => r.CalculatedRank)
            .ToListAsync();

        BoardGameFrontImageUrl = BoardGame.Gid == Guid.Empty
            ? string.Empty
            : $"/media/boardgame/front/{BoardGame.Gid:D}";

        if (PlayerRankings.Any())
        {
            var playerIds = PlayerRankings.Select(r => r.FkBgdPlayer).ToList();
            var playerInfo = await _context.Players
                .Where(p => playerIds.Contains(p.Id))
                .Select(p => new { p.Id, p.Gid })
                .ToListAsync();

            var actualGids = playerInfo.Select(p => (Guid?)p.Gid).ToList();

            var playerImageFilter = Builders<BoardGameImages>.Filter.And(
                Builders<BoardGameImages>.Filter.Eq(x => x.SQLTable, "bgd.Player"),
                Builders<BoardGameImages>.Filter.In(x => x.GID, actualGids)
            );

            var images = await _imagesCollection.Find(playerImageFilter).ToListAsync();
            foreach (var p in playerInfo)
            {
                var img = images.FirstOrDefault(x => x.GID == p.Gid);
                if (img?.ImageBytes != null)
                {
                    PlayerImages[p.Id] = $"data:{img.ContentType};base64,{Convert.ToBase64String(img.ImageBytes)}";
                }
            }
        }

        return Page();
    }

    private bool CanViewGame(BoardGame boardGame, CurrentClubContext currentClub)
    {
        if (User.IsInRole("Admin") && currentClub.IsPlatformAdminMode)
        {
            return boardGame.FkBgdClub == null;
        }

        return currentClub.CurrentClubId.HasValue && boardGame.FkBgdClub == currentClub.CurrentClubId.Value;
    }

    public string GetInitials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "?";
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1
            ? (parts[0][0].ToString() + parts[1][0].ToString()).ToUpper()
            : name.Substring(0, Math.Min(2, name.Length)).ToUpper();
    }

    public string GetAvatarColor(string? name)
    {
        if (string.IsNullOrEmpty(name)) return "#6c757d";
        int hash = name.GetHashCode();
        var colors = new[] { "#d32f2f", "#7b1fa2", "#303f9f", "#1976d2", "#00796b", "#388e3c", "#ffa000", "#e64a19" };
        return colors[Math.Abs(hash) % colors.Length];
    }
}

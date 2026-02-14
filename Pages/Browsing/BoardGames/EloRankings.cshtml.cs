using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

namespace Board_Game_Software.Pages.Browsing.BoardGames;

public class EloRankingsModel : PageModel
{
    private readonly BoardGameDbContext _context;
    private readonly IMongoCollection<BoardGameImages> _imagesCollection;

    public EloRankingsModel(BoardGameDbContext context, IMongoClient mongoClient, IConfiguration configuration)
    {
        _context = context;
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

        BoardGame = await _context.BoardGames.FirstOrDefaultAsync(m => m.Id == id);
        if (BoardGame == null) return NotFound();

        // Data is now pre-ranked and pre-sorted by the SQL View logic
        PlayerRankings = await _context.VwEloRankings
            .Where(r => r.FkBgdBoardGame == id && !r.Inactive)
            .OrderBy(r => r.CalculatedRank)
            .ToListAsync();

        // Load Game Banner
        var frontType = await _context.BoardGameImageTypes.FirstOrDefaultAsync(t => t.TypeDesc == "Board Game Front");
        if (frontType != null && BoardGame.Gid != Guid.Empty)
        {
            var bannerFilter = Builders<BoardGameImages>.Filter.And(
                Builders<BoardGameImages>.Filter.Eq(img => img.GID, (Guid?)BoardGame.Gid),
                Builders<BoardGameImages>.Filter.Eq(img => img.ImageTypeGID, (Guid?)frontType.Gid)
            );

            var gameImg = await _imagesCollection.Find(bannerFilter).FirstOrDefaultAsync();
            if (gameImg?.ImageBytes != null)
                BoardGameFrontImageUrl = $"data:{gameImg.ContentType};base64,{Convert.ToBase64String(gameImg.ImageBytes)}";
        }

        // Load Player Avatars
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
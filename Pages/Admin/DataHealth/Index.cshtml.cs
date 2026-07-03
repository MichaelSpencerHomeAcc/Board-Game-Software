using Board_Game_Software.Models;
using Board_Game_Software.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Board_Game_Software.Pages.Admin.DataHealth
{
    public class IndexModel : PageModel
    {
        private readonly BoardGameDbContext _db;

        public IndexModel(BoardGameDbContext db)
        {
            _db = db;
        }

        public List<IssueRow> MissingCovers { get; private set; } = new();
        public List<IssueRow> GamesWithoutShelves { get; private set; } = new();
        public List<IssueRow> PlayerCountConflicts { get; private set; } = new();
        public List<IssueRow> ExpansionLinkIssues { get; private set; } = new();
        public List<IssueRow> GamesWithoutRatingMethod { get; private set; } = new();
        public List<IssueRow> PlayersWithoutAccounts { get; private set; } = new();

        public sealed class IssueRow
        {
            public long? Id { get; init; }
            public string Name { get; init; } = string.Empty;
            public string Detail { get; init; } = string.Empty;
            public string LinkPage { get; init; } = string.Empty;
        }

        public async Task OnGetAsync()
        {
            if (!User.IsInRole("Admin")) return;

            var games = await _db.BoardGames.AsNoTracking()
                .Where(g => !g.Inactive)
                .Select(g => new
                {
                    g.Id,
                    g.Gid,
                    g.BoardGameName,
                    g.IsExpansion,
                    g.PlayerCountMin,
                    g.PlayerCountMax,
                    ShelfCount = g.BoardGameShelfSections.Count(s => !s.Inactive),
                    EloCount = g.BoardGameEloMethods.Count(m => !m.Inactive),
                    BaseLinks = g.BoardGameExpansionExpansionGames.Count(e => !e.Inactive),
                    ExpansionLinks = g.BoardGameExpansionBaseGames.Count(e => !e.Inactive)
                })
                .ToListAsync();

            var gameIds = games.Select(g => checked((int)g.Id)).ToList();
            var covered = await _db.StoredImages
                .AsNoTracking()
                .Where(image => image.OwnerType == ImageService.GameCoverOwnerType && gameIds.Contains(image.OwnerId))
                .Select(image => image.OwnerId)
                .Distinct()
                .ToListAsync();
            var coveredIds = covered.ToHashSet();

            MissingCovers = games
                .Where(g => !g.IsExpansion && !coveredIds.Contains(checked((int)g.Id)))
                .OrderBy(g => g.BoardGameName)
                .Take(25)
                .Select(g => GameIssue(g.Id, g.BoardGameName, "No front cover image found."))
                .ToList();

            GamesWithoutShelves = games
                .Where(g => !g.IsExpansion && g.ShelfCount == 0)
                .OrderBy(g => g.BoardGameName)
                .Take(25)
                .Select(g => GameIssue(g.Id, g.BoardGameName, "Not assigned to any shelf section."))
                .ToList();

            PlayerCountConflicts = games
                .Where(g => g.PlayerCountMin.HasValue && g.PlayerCountMax.HasValue && g.PlayerCountMin > g.PlayerCountMax)
                .OrderBy(g => g.BoardGameName)
                .Take(25)
                .Select(g => GameIssue(g.Id, g.BoardGameName, $"{g.PlayerCountMin}-{g.PlayerCountMax} players is inverted."))
                .ToList();

            ExpansionLinkIssues = games
                .Where(g => g.IsExpansion && g.BaseLinks == 0)
                .Concat(games.Where(g => !g.IsExpansion && g.BoardGameName.Contains("Expansion") && g.BaseLinks == 0 && g.ExpansionLinks == 0))
                .OrderBy(g => g.BoardGameName)
                .Take(25)
                .Select(g => GameIssue(g.Id, g.BoardGameName, "Looks like an expansion but is not linked to a base game."))
                .ToList();

            GamesWithoutRatingMethod = games
                .Where(g => !g.IsExpansion && g.EloCount == 0)
                .OrderBy(g => g.BoardGameName)
                .Take(25)
                .Select(g => GameIssue(g.Id, g.BoardGameName, "No rating method configured."))
                .ToList();

            PlayersWithoutAccounts = await _db.Players.AsNoTracking()
                .Where(p => !p.Inactive && string.IsNullOrWhiteSpace(p.FkdboAspNetUsers))
                .OrderBy(p => p.FirstName)
                .ThenBy(p => p.LastName)
                .Take(25)
                .Select(p => new IssueRow
                {
                    Id = p.Id,
                    Name = (p.FirstName + " " + p.LastName).Trim(),
                    Detail = "Player is not linked to a login.",
                    LinkPage = "/DataSetup/Players/Details"
                })
                .ToListAsync();
        }

        private static IssueRow GameIssue(long id, string name, string detail)
            => new()
            {
                Id = id,
                Name = name,
                Detail = detail,
                LinkPage = "/Browsing/BoardGames/BoardGameDetails"
            };
    }
}

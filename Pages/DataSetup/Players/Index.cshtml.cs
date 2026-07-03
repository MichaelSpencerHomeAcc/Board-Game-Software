using Board_Game_Software.Models;
using Board_Game_Software.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Board_Game_Software.Pages.DataSetup.Players
{
    public class IndexModel : PageModel
    {
        private readonly BoardGameDbContext _context;
        private readonly IMongoCollection<BoardGameImages> _boardGameImages;
        private readonly ICurrentClubService _currentClubService;

        public IndexModel(
            BoardGameDbContext context,
            IMongoClient mongoClient,
            IConfiguration configuration,
            ICurrentClubService currentClubService)
        {
            _context = context;
            _currentClubService = currentClubService;
            var databaseName = configuration["MongoDbSettings:Database"];
            var database = mongoClient.GetDatabase(databaseName);
            _boardGameImages = database.GetCollection<BoardGameImages>("BoardGameImages");
        }

        public sealed class PlayerRow
        {
            public long Id { get; init; }
            public Guid Gid { get; init; }
            public string FullName { get; init; } = string.Empty;

            public DateOnly? DateOfBirth { get; init; }

            public string? FKdboAspNetUsers { get; init; }

            public string? ClubName { get; init; }

            public string AvatarUrl => $"/media/player/{Gid}";
            public string FocusStyle { get; init; } = "50% 50%";
            public bool HasImage { get; init; }
        }

        public IList<PlayerRow> Players { get; private set; } = new List<PlayerRow>();
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; } = 25;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
        public CurrentClubContext CurrentClub { get; private set; } = CurrentClubContext.Empty;
        public bool CanManagePlayers => User.IsInRole("Admin") || CurrentClub.CanManageCurrentClub;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        public async Task OnGetAsync(string? search, int pageNumber = 1)
        {
            SearchTerm = search;
            PageNumber = Math.Max(1, pageNumber);
            CurrentClub = await _currentClubService.GetCurrentClubAsync();

            // 1) SQL: lightweight projection
            var query = _context.Players
                .AsNoTracking()
                .Include(p => p.FkBgdClubNavigation)
                .Include(p => p.PlayerClubs.Where(pc => !pc.Inactive))
                    .ThenInclude(pc => pc.FkBgdClubNavigation)
                .Where(p => !p.Inactive)
                .AsQueryable();

            if (!User.IsInRole("Admin"))
            {
                if (!CurrentClub.CurrentClubId.HasValue)
                {
                    Players = new List<PlayerRow>();
                    TotalCount = 0;
                    PageNumber = 1;
                    return;
                }

                var currentClubId = CurrentClub.CurrentClubId.Value;
                query = query.Where(p => p.PlayerClubs.Any(pc =>
                    !pc.Inactive &&
                    pc.FkBgdClub == currentClubId)
                    || p.FkBgdClub == currentClubId);
            }

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim();
                query = query.Where(p =>
                    (p.FirstName != null && p.FirstName.Contains(term)) ||
                    (p.MiddleName != null && p.MiddleName.Contains(term)) ||
                    (p.LastName != null && p.LastName.Contains(term)) ||
                    p.PlayerClubs.Any(pc => !pc.Inactive && pc.FkBgdClubNavigation.ClubName.Contains(term)) ||
                    (p.FkBgdClubNavigation != null && p.FkBgdClubNavigation.ClubName.Contains(term)));
            }

            TotalCount = await query.CountAsync();
            PageNumber = Math.Min(PageNumber, Math.Max(TotalPages, 1));

            var players = await query
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            if (players.Count == 0)
            {
                Players = new List<PlayerRow>();
                return;
            }

            // 2) Mongo: ONE query for all player images
            var gids = players.Select(p => (Guid?)p.Gid).ToArray();

            var imgDocs = await _boardGameImages
                .Find(Builders<BoardGameImages>.Filter.Eq(x => x.SQLTable, "bgd.Player") &
                      Builders<BoardGameImages>.Filter.In(x => x.GID, gids))
                .Project(x => new { x.GID, x.AvatarFocusX, x.AvatarFocusY, x.ImageBytes })
                .ToListAsync();

            // map: gid -> focus + hasImage
            var imgMap = imgDocs
                .Where(d => d.GID.HasValue)
                .ToDictionary(
                    d => d.GID!.Value,
                    d => new
                    {
                        Focus = $"{d.AvatarFocusX}% {d.AvatarFocusY}%",
                        HasImage = d.ImageBytes != null && d.ImageBytes.Length > 0
                    });

            // 3) combine
            Players = players.Select(p =>
            {
                var found = imgMap.TryGetValue(p.Gid, out var meta);

                return new PlayerRow
                {
                    Id = p.Id,
                    Gid = p.Gid,
                    FullName = ((p.FirstName ?? "") + " " + (p.MiddleName ?? "") + " " + (p.LastName ?? "")).Trim(),
                    DateOfBirth = p.DateOfBirth,
                    FKdboAspNetUsers = p.FkdboAspNetUsers,
                    ClubName = BuildClubName(p),
                    FocusStyle = found ? meta!.Focus : "50% 50%",
                    HasImage = found && meta!.HasImage
                };
            }).ToList();
        }

        private static string BuildClubName(Player player)
        {
            var clubNames = player.PlayerClubs
                .Where(pc => !pc.Inactive)
                .Select(pc => pc.FkBgdClubNavigation?.ClubName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name!)
                .ToList();

            if (clubNames.Count == 0 && !string.IsNullOrWhiteSpace(player.FkBgdClubNavigation?.ClubName))
            {
                clubNames.Add(player.FkBgdClubNavigation.ClubName);
            }

            return string.Join(", ", clubNames.Distinct().OrderBy(name => name));
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id)
        {
            if (!User.IsInRole("Admin")) return Forbid();

            var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == id);
            if (player == null) return NotFound();

            await _boardGameImages.DeleteManyAsync(img => img.GID == player.Gid && img.SQLTable == "bgd.Player");
            _context.Players.Remove(player);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}

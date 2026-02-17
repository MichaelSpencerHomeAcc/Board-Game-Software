using Board_Game_Software.Models;
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

        public IndexModel(BoardGameDbContext context, IMongoClient mongoClient, IConfiguration configuration)
        {
            _context = context;
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

            public string AvatarUrl => $"/media/player/{Gid}";
            public string FocusStyle { get; init; } = "50% 50%";
            public bool HasImage { get; init; }
        }

        public IList<PlayerRow> Players { get; private set; } = new List<PlayerRow>();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        public async Task OnGetAsync()
        {
            // 1) SQL: lightweight projection
            var query = _context.VwPlayers
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim();
                query = query.Where(p => p.FullName.Contains(term));
            }

            var players = await query
                .OrderBy(p => p.FullName)
                .Select(p => new
                {
                    p.Id,
                    p.Gid,
                    p.FullName,
                    p.DateOfBirth,       
                    p.FKdboAspNetUsers
                })
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
                    FullName = p.FullName ?? string.Empty,
                    DateOfBirth = p.DateOfBirth,
                    FKdboAspNetUsers = p.FKdboAspNetUsers,
                    FocusStyle = found ? meta!.Focus : "50% 50%",
                    HasImage = found && meta!.HasImage
                };
            }).ToList();
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

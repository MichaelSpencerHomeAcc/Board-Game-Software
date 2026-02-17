using Board_Game_Software.Data;
using Board_Game_Software.Models;
using Microsoft.AspNetCore.Identity;
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
    public class DetailsModel : PageModel
    {
        private readonly BoardGameDbContext _context;
        private readonly IMongoCollection<BoardGameImages> _imagesCollection;
        private readonly UserManager<IdentityUser> _userManager;

        public DetailsModel(
            BoardGameDbContext context,
            IMongoClient mongoClient,
            IConfiguration configuration,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;

            var dbName = configuration["MongoDbSettings:Database"];
            var database = mongoClient.GetDatabase(dbName);
            _imagesCollection = database.GetCollection<BoardGameImages>("BoardGameImages");
        }

        public Player Player { get; set; } = null!;

        // FAST: no base64 – just use media endpoint
        public string ProfileImageUrl { get; set; } = string.Empty;
        public bool HasProfileImage { get; set; }

        public bool CanEdit { get; set; }

        public List<PlayerBoardGame> TopTenGames { get; set; } = new();
        public Dictionary<long, string> GameImages { get; set; } = new(); // entryId -> image url

        // Focus / zoom (defaults)
        public int AvatarX { get; set; } = 50;
        public int AvatarY { get; set; } = 50;
        public int AvatarZoom { get; set; } = 100;
        public int PodiumX { get; set; } = 50;
        public int PodiumY { get; set; } = 50;
        public int PodiumZoom { get; set; } = 100;

        public async Task<IActionResult> OnGetAsync(long id)
        {
            Player = await _context.Players
                .AsNoTracking()
                .Include(p => p.PlayerBoardGames)
                    .ThenInclude(pbg => pbg.BoardGame)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Player == null) return NotFound();

            // SECURITY CHECK: Admin or owner
            var currentUserId = _userManager.GetUserId(User);
            CanEdit = User.IsInRole("Admin") || (Player.FkdboAspNetUsers == currentUserId);

            // Top 10 list
            TopTenGames = Player.PlayerBoardGames
                .Where(x => !x.Inactive)
                .OrderBy(x => x.Rank)
                .Take(10)
                .ToList();

            // 1) Mongo: load player image doc (for focus + "has image")
            await LoadPlayerImageMeta(Player.Gid);

            // 2) Game images: just point to your media endpoint (fast)
            GameImages.Clear();
            foreach (var entry in TopTenGames)
            {
                if (entry.BoardGame == null) continue;
                GameImages[entry.Id] = $"/media/boardgame/front/{entry.BoardGame.Gid}";
            }

            return Page();
        }

        private async Task LoadPlayerImageMeta(Guid playerGid)
        {
            ProfileImageUrl = $"/media/player/{playerGid}";
            HasProfileImage = false;

            var filter = Builders<BoardGameImages>.Filter.And(
                Builders<BoardGameImages>.Filter.Eq(x => x.SQLTable, "bgd.Player"),
                Builders<BoardGameImages>.Filter.Eq(x => x.GID, (Guid?)playerGid)
            );

            // Only pull the metadata we need (fast)
            var doc = await _imagesCollection
                .Find(filter)
                .Project(x => new
                {
                    x.ImageBytes,
                    x.AvatarFocusX,
                    x.AvatarFocusY,
                    x.AvatarZoom,
                    x.PodiumFocusX,
                    x.PodiumFocusY,
                    x.PodiumZoom
                })
                .FirstOrDefaultAsync();

            if (doc == null) return;

            HasProfileImage = doc.ImageBytes != null && doc.ImageBytes.Length > 0;

            // If your Mongo fields are non-nullable ints, these assignments are safe.
            // If any can be 0 / unset, clamp them to sensible ranges.
            AvatarX = ClampPct(doc.AvatarFocusX, 50);
            AvatarY = ClampPct(doc.AvatarFocusY, 50);
            AvatarZoom = ClampZoom(doc.AvatarZoom, 100);

            PodiumX = ClampPct(doc.PodiumFocusX, 50);
            PodiumY = ClampPct(doc.PodiumFocusY, 50);
            PodiumZoom = ClampZoom(doc.PodiumZoom, 100);
        }

        public async Task<IActionResult> OnPostUpdateFocusAsync(
            long id,
            int AvatarX, int AvatarY, int AvatarZoom,
            int PodiumX, int PodiumY, int PodiumZoom)
        {
            var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == id);
            if (player == null) return NotFound();

            // SECURITY RE-CHECK
            var currentUserId = _userManager.GetUserId(User);
            if (!User.IsInRole("Admin") && player.FkdboAspNetUsers != currentUserId)
                return Forbid();

            var filter = Builders<BoardGameImages>.Filter.And(
                Builders<BoardGameImages>.Filter.Eq(x => x.SQLTable, "bgd.Player"),
                Builders<BoardGameImages>.Filter.Eq(x => x.GID, (Guid?)player.Gid)
            );

            var update = Builders<BoardGameImages>.Update
                .Set(x => x.AvatarFocusX, ClampPct(AvatarX, 50))
                .Set(x => x.AvatarFocusY, ClampPct(AvatarY, 50))
                .Set(x => x.AvatarZoom, ClampZoom(AvatarZoom, 100))
                .Set(x => x.PodiumFocusX, ClampPct(PodiumX, 50))
                .Set(x => x.PodiumFocusY, ClampPct(PodiumY, 50))
                .Set(x => x.PodiumZoom, ClampZoom(PodiumZoom, 100));

            await _imagesCollection.UpdateOneAsync(filter, update);

            return RedirectToPage(new { id });
        }

        private static int ClampPct(int v, int fallback)
        {
            if (v <= 0) return fallback;
            if (v > 100) return 100;
            return v;
        }

        private static int ClampZoom(int v, int fallback)
        {
            if (v <= 0) return fallback;
            if (v < 100) return 100;
            if (v > 300) return 300;
            return v;
        }

        public string GetInitials(string? f, string? l)
            => $"{(f?.Length > 0 ? f[0] : ' ')}{(l?.Length > 0 ? l[0] : ' ')}"
                .ToUpper()
                .Trim();

        public string GetAvatarColor(string? name)
        {
            if (string.IsNullOrEmpty(name)) return "#6c757d";
            int hash = name.GetHashCode();
            var colors = new[] { "#d32f2f", "#7b1fa2", "#303f9f", "#1976d2", "#00796b", "#388e3c", "#ffa000", "#e64a19" };
            return colors[Math.Abs(hash) % colors.Length];
        }
    }
}

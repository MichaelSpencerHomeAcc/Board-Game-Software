using Board_Game_Software.Data;
using Board_Game_Software.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

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
            var databaseName = configuration["MongoDbSettings:Database"];
            var database = mongoClient.GetDatabase(databaseName);
            _imagesCollection = database.GetCollection<BoardGameImages>("BoardGameImages");
        }

        public Player Player { get; set; } = null!;
        public string? ProfileImageBase64 { get; set; }
        public long? CurrentUserClaimedPlayerId { get; set; }
        public List<PlayerBoardGame> TopTenGames { get; set; } = new();
        public Dictionary<long, string> GameImages { get; set; } = new();

        // Focal points and Zoom levels
        public int AvatarX { get; set; } = 50;
        public int AvatarY { get; set; } = 50;
        public int AvatarZoom { get; set; } = 100;
        public int PodiumX { get; set; } = 50;
        public int PodiumY { get; set; } = 50;
        public int PodiumZoom { get; set; } = 100;

        public async Task<IActionResult> OnGetAsync(long id)
        {
            Player = await _context.Players
                .Include(p => p.PlayerBoardGames)
                    .ThenInclude(pbg => pbg.BoardGame)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Player == null) return NotFound();

            TopTenGames = Player.PlayerBoardGames
                .Where(x => !x.Inactive)
                .OrderBy(x => x.Rank)
                .Take(10)
                .ToList();

            // Load Game Box Art for Top 10
            if (TopTenGames.Any())
            {
                var frontImageType = await _context.BoardGameImageTypes
                    .FirstOrDefaultAsync(bgit => bgit.TypeDesc == "Board Game Front");

                if (frontImageType != null)
                {
                    var gidStrings = TopTenGames.Where(x => x.BoardGame != null).Select(x => x.BoardGame.Gid.ToString()).ToList();
                    var images = await _imagesCollection.Find(img => gidStrings.Contains(img.GID.ToString()) && img.ImageTypeGID == frontImageType.Gid).ToListAsync();

                    foreach (var item in TopTenGames)
                    {
                        var img = images.FirstOrDefault(x => x.GID.ToString() == item.BoardGame.Gid.ToString());
                        if (img?.ImageBytes != null)
                        {
                            GameImages[item.Id] = $"data:{img.ContentType};base64,{Convert.ToBase64String(img.ImageBytes)}";
                        }
                    }
                }
            }

            await LoadProfileImage(Player.Gid);

            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var claimedPlayer = await _context.Players.FirstOrDefaultAsync(p => p.FkdboAspNetUsers == user.Id);
                CurrentUserClaimedPlayerId = claimedPlayer?.Id;
            }

            return Page();
        }

        private async Task LoadProfileImage(Guid gid)
        {
            var filter = Builders<BoardGameImages>.Filter.And(
                Builders<BoardGameImages>.Filter.Eq(x => x.SQLTable, "bgd.Player"),
                Builders<BoardGameImages>.Filter.Eq(x => x.GID, gid)
            );

            var imageDoc = await _imagesCollection.Find(filter).FirstOrDefaultAsync();
            if (imageDoc != null)
            {
                if (imageDoc.ImageBytes != null)
                {
                    ProfileImageBase64 = $"data:{imageDoc.ContentType};base64,{Convert.ToBase64String(imageDoc.ImageBytes)}";
                }

                AvatarX = imageDoc.AvatarFocusX;
                AvatarY = imageDoc.AvatarFocusY;
                AvatarZoom = imageDoc.AvatarZoom;
                PodiumX = imageDoc.PodiumFocusX;
                PodiumY = imageDoc.PodiumFocusY;
                PodiumZoom = imageDoc.PodiumZoom;
            }
        }

        public async Task<IActionResult> OnPostUpdateFocusAsync(long id, int AvatarX, int AvatarY, int AvatarZoom, int PodiumX, int PodiumY, int PodiumZoom)
        {
            var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == id);
            if (player == null) return NotFound();

            var filter = Builders<BoardGameImages>.Filter.And(
                Builders<BoardGameImages>.Filter.Eq(x => x.SQLTable, "bgd.Player"),
                Builders<BoardGameImages>.Filter.Eq(x => x.GID, player.Gid)
            );

            var update = Builders<BoardGameImages>.Update
                .Set(x => x.AvatarFocusX, AvatarX)
                .Set(x => x.AvatarFocusY, AvatarY)
                .Set(x => x.AvatarZoom, AvatarZoom)
                .Set(x => x.PodiumFocusX, PodiumX)
                .Set(x => x.PodiumFocusY, PodiumY)
                .Set(x => x.PodiumZoom, PodiumZoom);

            await _imagesCollection.UpdateOneAsync(filter, update);

            return RedirectToPage(new { id });
        }

        public string GetInitials(string? f, string? l) => $"{(f?.Length > 0 ? f[0] : ' ')}{(l?.Length > 0 ? l[0] : ' ')}".ToUpper().Trim();

        public string GetAvatarColor(string? name)
        {
            if (string.IsNullOrEmpty(name)) return "#6c757d";
            int hash = name.GetHashCode();
            var colors = new[] { "#d32f2f", "#7b1fa2", "#303f9f", "#1976d2", "#00796b", "#388e3c", "#ffa000", "#e64a19" };
            return colors[Math.Abs(hash) % colors.Length];
        }
    }
}
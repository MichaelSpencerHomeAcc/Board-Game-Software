using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

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

        public IList<VwPlayer> Players { get; set; } = default!;
        public Dictionary<long, string> PlayerImagesBase64 { get; set; } = new();
        public Dictionary<long, string> PlayerFocusStyles { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.VwPlayers.AsQueryable();

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                query = query.Where(p => p.FullName.Contains(SearchTerm));
            }

            // Visible to everyone
            Players = await query.OrderBy(p => p.FullName).ToListAsync();

            foreach (var player in Players)
            {
                var img = await _boardGameImages
                    .Find(x => x.SQLTable == "bgd.Player" && x.GID == player.Gid)
                    .FirstOrDefaultAsync();

                if (img != null)
                {
                    if (img.ImageBytes != null)
                    {
                        PlayerImagesBase64[player.Id] = $"data:{img.ContentType};base64,{Convert.ToBase64String(img.ImageBytes)}";
                    }
                    PlayerFocusStyles[player.Id] = $"{img.AvatarFocusX}% {img.AvatarFocusY}%";
                }
                else
                {
                    PlayerFocusStyles[player.Id] = "50% 50%";
                }
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id)
        {
            // SECURITY: Hard restriction - Only Admins can delete
            if (!User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == id);
            if (player == null) return NotFound();

            await _boardGameImages.DeleteManyAsync(img => img.GID == player.Gid && img.SQLTable == "bgd.Player");
            _context.Players.Remove(player);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}
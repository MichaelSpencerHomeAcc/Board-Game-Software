using Board_Game_Software.Data;
using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Microsoft.Extensions.Configuration;
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

        public IList<VwPlayer> Players { get; set; } = default!;

        // Holds the Base64 strings for the frontend
        public Dictionary<long, string> PlayerImagesBase64 { get; set; } = new();

        // --- NEW: Search Property ---
        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        public async Task OnGetAsync()
        {
            // 1. Start the query
            var query = _context.VwPlayers.AsQueryable();

            // 2. Apply Search Filter (if text exists)
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                // specific filtering on FullName
                query = query.Where(p => p.FullName.Contains(SearchTerm));
            }

            // 3. Execute Query
            Players = await query
                .OrderBy(p => p.FullName)
                .ToListAsync();

            // 4. Fetch Images for the *Filtered* list only
            foreach (var player in Players)
            {
                var img = await _boardGameImages
                    .Find(x => x.SQLTable == "bgd.Player" && x.GID == player.Gid)
                    .FirstOrDefaultAsync();

                if (img != null && img.ImageBytes != null)
                {
                    PlayerImagesBase64[player.Id] = $"data:{img.ContentType};base64,{Convert.ToBase64String(img.ImageBytes)}";
                }
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id)
        {
            var player = await _context.Players.FindAsync(id);
            if (player == null) return NotFound();

            await _boardGameImages.DeleteManyAsync(img => img.GID == player.Gid && img.SQLTable == "bgd.Player");

            _context.Players.Remove(player);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}
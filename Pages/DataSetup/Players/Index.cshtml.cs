using Board_Game_Software.Data;
using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver; // Don't forget this!
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Board_Game_Software.Pages.DataSetup.Players
{
    public class IndexModel : PageModel
    {
        private readonly BoardGameDbContext _context;
        private readonly IMongoCollection<BoardGameImages> _boardGameImages; // Added

        public IndexModel(BoardGameDbContext context, IMongoClient mongoClient, IConfiguration configuration)
        {
            _context = context;
            // Setup Mongo exactly like your Board Games Edit page
            var databaseName = configuration["MongoDbSettings:Database"];
            var database = mongoClient.GetDatabase(databaseName);
            _boardGameImages = database.GetCollection<BoardGameImages>("BoardGameImages");
        }

        public IList<VwPlayer> Players { get; set; } = default!;

        // This is the missing piece the HTML is looking for!
        public Dictionary<long, string> PlayerImagesBase64 { get; set; } = new();

        public async Task OnGetAsync()
        {
            Players = await _context.VwPlayers
                .OrderBy(p => p.FullName)
                .ToListAsync();

            foreach (var player in Players)
            {
                // MATCHING YOUR DETAILS FILTER: "bgd.Player" instead of "Players"
                var img = await _boardGameImages
                    .Find(x => x.SQLTable == "bgd.Player" && x.GID == player.Gid)
                    .FirstOrDefaultAsync();

                if (img != null && img.ImageBytes != null)
                {
                    // Using img.ContentType makes it work for png, jpg, etc.
                    PlayerImagesBase64[player.Id] = $"data:{img.ContentType};base64,{Convert.ToBase64String(img.ImageBytes)}";
                }
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id)
        {
            var player = await _context.Players.FindAsync(id);
            if (player == null) return NotFound();

            // Clean up using the correct table string
            await _boardGameImages.DeleteManyAsync(img => img.GID == player.Gid && img.SQLTable == "bgd.Player");

            _context.Players.Remove(player);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}
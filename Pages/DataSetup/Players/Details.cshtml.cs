using Board_Game_Software.Data;
using Board_Game_Software.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System;
using System.Security.Claims;
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
            var databaseName = configuration["MongoDbSettings:Database"];
            var database = mongoClient.GetDatabase(databaseName);
            _imagesCollection = database.GetCollection<BoardGameImages>("BoardGameImages");
        }

        public Player Player { get; set; } = null!;
        public string? ProfileImageBase64 { get; set; }
        public long? CurrentUserClaimedPlayerId { get; set; }

        public async Task<IActionResult> OnGetAsync(long id)
        {
            Player = await _context.Players.FindAsync(id);
            if (Player == null)
            {
                return NotFound();
            }

            await LoadProfileImage(Player.Gid);

            // Check if the current logged-in user has already claimed a player
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var claimedPlayer = await _context.Players
                    .FirstOrDefaultAsync(p => p.FkdboAspNetUsers == user.Id);

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

            if (imageDoc != null && imageDoc.ImageBytes != null)
            {
                // Assuming standard image types; you can make this dynamic if needed
                ProfileImageBase64 = $"data:image/png;base64,{Convert.ToBase64String(imageDoc.ImageBytes)}";
            }
        }

        public async Task<IActionResult> OnPostClaimAsync(long id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Forbid();

            // 1. Check if user already claimed a player
            var existingClaim = await _context.Players.FirstOrDefaultAsync(p => p.FkdboAspNetUsers == userId);
            if (existingClaim != null)
            {
                ModelState.AddModelError(string.Empty, "You have already claimed a player profile.");

                // Reload data to show page correctly with error
                Player = await _context.Players.FindAsync(id);
                if (Player != null) await LoadProfileImage(Player.Gid);
                CurrentUserClaimedPlayerId = existingClaim.Id;

                return Page();
            }

            // 2. Find and Claim
            Player = await _context.Players.FindAsync(id);
            if (Player == null) return NotFound();

            if (!string.IsNullOrEmpty(Player.FkdboAspNetUsers))
            {
                ModelState.AddModelError(string.Empty, "This player is already claimed by another user.");
                await LoadProfileImage(Player.Gid);
                return Page();
            }

            Player.FkdboAspNetUsers = userId;
            await _context.SaveChangesAsync();

            return RedirectToPage(new { id });
        }

        // --- UI Helper Methods ---

        public string GetInitials(string? firstName, string? lastName)
        {
            string first = !string.IsNullOrEmpty(firstName) ? firstName[0].ToString() : "";
            string last = !string.IsNullOrEmpty(lastName) ? lastName[0].ToString() : "";
            return (first + last).ToUpper();
        }

        public string GetAvatarColor(string? name)
        {
            if (string.IsNullOrEmpty(name)) return "#6c757d";
            int hash = 0;
            foreach (char c in name) hash = c + ((hash << 5) - hash);
            var colors = new[] { "#d32f2f", "#c2185b", "#7b1fa2", "#512da8", "#303f9f", "#1976d2", "#0288d1", "#0097a7", "#00796b", "#388e3c", "#689f38", "#fbc02d", "#ffa000", "#f57c00", "#e64a19", "#5d4037", "#616161", "#455a64" };
            return colors[Math.Abs(hash) % colors.Length];
        }
    }
}
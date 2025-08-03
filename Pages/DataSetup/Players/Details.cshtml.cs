using Board_Game_Software.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

public class DetailsModel : PageModel
{
    private readonly BoardGameDbContext _context;
    private readonly IMongoCollection<BoardGameImages> _imagesCollection;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;

    public Player Player { get; set; } = null!;
    public string? ProfileImageBase64 { get; set; }

    public long? CurrentUserClaimedPlayerId { get; set; }

    public DetailsModel(BoardGameDbContext context, IMongoClient mongoClient, IConfiguration configuration, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
        var databaseName = configuration["MongoDbSettings:Database"];
        var database = mongoClient.GetDatabase(databaseName);
        _imagesCollection = database.GetCollection<BoardGameImages>("BoardGameImages");
    }

    public async Task<IActionResult> OnGetAsync(long id)
    {
        Player = await _context.Players.FindAsync(id);
        if (Player == null)
        {
            return NotFound();
        }

        await LoadProfileImage(Player.Gid);

        // Get logged in user ID
        var user = await _userManager.GetUserAsync(User);
        if (user != null)
        {
            // Find if this user already claimed a Player
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
            ProfileImageBase64 = $"data:image/png;base64,{Convert.ToBase64String(imageDoc.ImageBytes)}";
        }
        else
        {
            ProfileImageBase64 = null;
        }
    }

    public async Task<IActionResult> OnPostClaimAsync(long id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Forbid();

        // Check if user already claimed a player
        var existingClaim = await _context.Players.FirstOrDefaultAsync(p => p.FkdboAspNetUsers == userId);
        if (existingClaim != null)
        {
            // You can add a ModelState error or just return a message that they must unclaim first
            ModelState.AddModelError(string.Empty, "You already claimed another player. Unclaim it first.");
            // Reload current player to redisplay details page properly
            Player = await _context.Players.FindAsync(id);
            await LoadProfileImage(Player.Gid);
            return Page();
        }

        // Find the player to claim
        Player = await _context.Players.FindAsync(id);
        if (Player == null)
            return NotFound();

        // Assign user to this player
        Player.FkdboAspNetUsers = userId;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, "Failed to claim player: " + ex.Message);
            return Page();
        }

        return RedirectToPage(new { id });
    }
}

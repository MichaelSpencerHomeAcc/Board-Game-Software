using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MongoDB.Driver;
using System;
using System.IO;
using System.Threading.Tasks;

public class EditModel : PageModel
{
    private readonly BoardGameDbContext _context;
    private readonly IMongoCollection<BoardGameImages> _imagesCollection;

    [BindProperty]
    public Player Player { get; set; }

    [BindProperty]
    public IFormFile? Upload { get; set; }

    public string? ProfileImageBase64 { get; set; }  // For displaying image

    public EditModel(BoardGameDbContext context, IMongoClient mongoClient, IConfiguration configuration)
    {
        _context = context;
        var databaseName = configuration["MongoDbSettings:Database"];
        var database = mongoClient.GetDatabase(databaseName);
        _imagesCollection = database.GetCollection<BoardGameImages>("BoardGameImages");
    }

    public async Task<IActionResult> OnGetAsync(long id)
    {
        Player = await _context.Players.FindAsync(id);

        if (Player == null)
            return NotFound();

        await LoadProfileImage(Player.Gid);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            foreach (var entry in ModelState)
            {
                var key = entry.Key;
                foreach (var error in entry.Value.Errors)
                {
                    Console.WriteLine($"Validation error on '{key}': {error.ErrorMessage}");
                }
            }

            Player = await _context.Players.FindAsync(Player.Id);
            if (Player == null)
                return NotFound();

            await LoadProfileImage(Player.Gid);

            return Page();
        }


        var playerInDb = await _context.Players.FindAsync(Player.Id);
        if (playerInDb == null)
            return NotFound();

        playerInDb.FirstName = Player.FirstName;
        playerInDb.LastName = Player.LastName;
        playerInDb.MiddleName = Player.MiddleName;
        playerInDb.DateOfBirth = Player.DateOfBirth;

        await _context.SaveChangesAsync();

        if (Upload != null && Upload.Length > 0)
        {
            try
            {
                using var ms = new MemoryStream();
                await Upload.CopyToAsync(ms);
                var imageBytes = ms.ToArray();

                var filter = Builders<BoardGameImages>.Filter.And(
                    Builders<BoardGameImages>.Filter.Eq(x => x.SQLTable, "bgd.Player"),
                    Builders<BoardGameImages>.Filter.Eq(x => x.GID, playerInDb.Gid)
                );

                var update = Builders<BoardGameImages>.Update
                    .Set(x => x.ImageBytes, imageBytes)
                    .Set(x => x.Description, "Profile Picture");

                var options = new UpdateOptions { IsUpsert = true };

                await _imagesCollection.UpdateOneAsync(filter, update, options);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Image upload failed: {ex.Message}");

                Player = await _context.Players.FindAsync(Player.Id);
                await LoadProfileImage(Player.Gid);
                return Page();
            }
        }

        return RedirectToPage("./Index");
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
}

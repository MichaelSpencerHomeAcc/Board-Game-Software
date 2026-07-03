using Board_Game_Software.Models;
using Board_Game_Software.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System;
using System.IO;
using System.Threading.Tasks;

public class EditModel : PageModel
{
    private readonly BoardGameDbContext _context;
    private readonly IMongoCollection<BoardGameImages> _imagesCollection;
    private readonly ICurrentClubService _currentClubService;

    [BindProperty]
    public Player Player { get; set; } = null!;

    [BindProperty]
    public IFormFile? Upload { get; set; }

    [BindProperty]
    public List<long> SelectedClubIds { get; set; } = new();

    public string? ProfileImageBase64 { get; set; }  // For displaying image
    public SelectList ClubOptions { get; set; } = default!;

    public EditModel(
        BoardGameDbContext context,
        IMongoClient mongoClient,
        IConfiguration configuration,
        ICurrentClubService currentClubService)
    {
        _context = context;
        _currentClubService = currentClubService;
        var databaseName = configuration["MongoDbSettings:Database"];
        var database = mongoClient.GetDatabase(databaseName);
        _imagesCollection = database.GetCollection<BoardGameImages>("BoardGameImages");
    }

    public async Task<IActionResult> OnGetAsync(long id)
    {
        var player = await _context.Players.FindAsync(id);
        if (player == null)
            return NotFound();

        if (!await CanManagePlayerAsync(id))
            return Forbid();

        Player = player;
        SelectedClubIds = await _context.PlayerClubs
            .AsNoTracking()
            .Where(pc => pc.FkBgdPlayer == Player.Id && !pc.Inactive)
            .Select(pc => pc.FkBgdClub)
            .ToListAsync();
        await LoadClubOptionsAsync();
        await LoadProfileImage(Player.Gid);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await CanManagePlayerAsync(Player.Id))
            return Forbid();

        SelectedClubIds = await KeepManageableClubIdsAsync(SelectedClubIds);

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

            var player = await _context.Players.FindAsync(Player.Id);
            if (player == null)
                return NotFound();

            Player = player;
            SelectedClubIds = await _context.PlayerClubs
                .AsNoTracking()
                .Where(pc => pc.FkBgdPlayer == Player.Id && !pc.Inactive)
                .Select(pc => pc.FkBgdClub)
                .ToListAsync();
            await LoadClubOptionsAsync();
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
        playerInDb.FkBgdClub = SelectedClubIds.FirstOrDefault() == 0 ? null : SelectedClubIds.First();
        playerInDb.ModifiedBy = User.Identity?.Name ?? "system";
        playerInDb.TimeModified = DateTime.UtcNow;

        await SyncPlayerClubsAsync(playerInDb.Id, SelectedClubIds, playerInDb.ModifiedBy, playerInDb.TimeModified);
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

                var player = await _context.Players.FindAsync(Player.Id);
                if (player == null)
                    return NotFound();

                Player = player;
                SelectedClubIds = await _context.PlayerClubs
                    .AsNoTracking()
                    .Where(pc => pc.FkBgdPlayer == Player.Id && !pc.Inactive)
                    .Select(pc => pc.FkBgdClub)
                    .ToListAsync();
                await LoadClubOptionsAsync();
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

    private async Task SyncPlayerClubsAsync(long playerId, IEnumerable<long> selectedClubIds, string actor, DateTime now)
    {
        var selected = selectedClubIds.Distinct().ToHashSet();
        var manageableClubIds = await GetManageableClubIdsAsync();
        var existing = await _context.PlayerClubs
            .Where(pc => pc.FkBgdPlayer == playerId && manageableClubIds.Contains(pc.FkBgdClub))
            .ToListAsync();

        foreach (var row in existing)
        {
            var shouldBeActive = selected.Contains(row.FkBgdClub);
            row.Inactive = !shouldBeActive;
            row.ModifiedBy = actor;
            row.TimeModified = now;
            selected.Remove(row.FkBgdClub);
        }

        var validNewClubIds = await _context.Clubs
            .Where(c => !c.Inactive && selected.Contains(c.Id) && manageableClubIds.Contains(c.Id))
            .Select(c => c.Id)
            .ToListAsync();

        foreach (var clubId in validNewClubIds)
        {
            _context.PlayerClubs.Add(new PlayerClub
            {
                FkBgdPlayer = playerId,
                FkBgdClub = clubId,
                JoinedAt = now,
                CreatedBy = actor,
                TimeCreated = now,
                ModifiedBy = actor,
                TimeModified = now
            });
        }
    }

    private async Task LoadClubOptionsAsync()
    {
        var manageableClubIds = await GetManageableClubIdsAsync();
        var clubs = await _context.Clubs
            .AsNoTracking()
            .Where(c => !c.Inactive && manageableClubIds.Contains(c.Id))
            .OrderBy(c => c.ClubName)
            .Select(c => new { c.Id, c.ClubName })
            .ToListAsync();

        ClubOptions = new SelectList(clubs, "Id", "ClubName");
    }

    private async Task<bool> CanManagePlayerAsync(long playerId)
    {
        var currentClub = await _currentClubService.GetCurrentClubAsync();
        if (User.IsInRole("Admin") && currentClub.IsPlatformAdminMode) return true;
        
        var manageableClubIds = await GetManageableClubIdsAsync();
        if (User.IsInRole("Admin") && currentClub.CurrentClubId.HasValue)
        {
            manageableClubIds = [currentClub.CurrentClubId.Value];
        }
        if (manageableClubIds.Count == 0) return false;

        return await _context.PlayerClubs
            .AsNoTracking()
            .AnyAsync(pc =>
                pc.FkBgdPlayer == playerId &&
                !pc.Inactive &&
                manageableClubIds.Contains(pc.FkBgdClub))
            || await _context.Players
                .AsNoTracking()
                .AnyAsync(p => p.Id == playerId
                    && p.FkBgdClub.HasValue
                    && manageableClubIds.Contains(p.FkBgdClub.Value));
    }

    private async Task<List<long>> GetManageableClubIdsAsync()
    {
        var currentClub = await _currentClubService.GetCurrentClubAsync();

        if (User.IsInRole("Admin"))
        {
            if (!currentClub.IsPlatformAdminMode && currentClub.CurrentClubId.HasValue)
            {
                return [currentClub.CurrentClubId.Value];
            }

            return await _context.Clubs
                .AsNoTracking()
                .Where(c => !c.Inactive)
                .Select(c => c.Id)
                .ToListAsync();
        }

        return currentClub.AvailableClubs
            .Where(c => c.Role is "Owner" or "Admin")
            .Select(c => c.ClubId)
            .ToList();
    }

    private async Task<List<long>> KeepManageableClubIdsAsync(IEnumerable<long> selectedClubIds)
    {
        var manageableClubIds = await GetManageableClubIdsAsync();
        return selectedClubIds
            .Distinct()
            .Where(manageableClubIds.Contains)
            .ToList();
    }
}

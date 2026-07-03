using Board_Game_Software.Models;
using Board_Game_Software.Services;
using Board_Game_Software.Settings; // Make sure this is here to find MongoDbSettings
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options; // Required for IOptions
using MongoDB.Driver;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Board_Game_Software.Pages.DataSetup.Players
{
    public class AddModel : PageModel
    {
        private readonly BoardGameDbContext _context;
        private readonly IMongoCollection<BoardGameImages> _boardGameImages;
        private readonly ICurrentClubService _currentClubService;

        // UPDATED CONSTRUCTOR: Uses IOptions to safely pull from your Program.cs setup
        public AddModel(
            BoardGameDbContext context,
            IMongoClient mongoClient,
            IOptions<MongoDbSettings> mongoSettings,
            ICurrentClubService currentClubService)
        {
            _context = context;
            _currentClubService = currentClubService;

            // Access the database name safely from the settings object
            var databaseName = mongoSettings.Value.Database;
            var database = mongoClient.GetDatabase(databaseName);

            _boardGameImages = database.GetCollection<BoardGameImages>("BoardGameImages");
        }

        [BindProperty]
        public Player Player { get; set; } = new();

        [BindProperty]
        public IFormFile? Upload { get; set; }

        [BindProperty]
        public List<long> SelectedClubIds { get; set; } = new();

        public SelectList ClubOptions { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync()
        {
            var canManage = await EnsureCanManagePlayersAsync();
            if (!canManage)
            {
                return Forbid();
            }

            await LoadClubOptionsAsync();

            var currentClub = await _currentClubService.GetCurrentClubAsync();
            if (currentClub.CurrentClubId.HasValue)
            {
                SelectedClubIds = [currentClub.CurrentClubId.Value];
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!await EnsureCanManagePlayersAsync())
            {
                return Forbid();
            }

            SelectedClubIds = await KeepManageableClubIdsAsync(SelectedClubIds);

            // Set metadata
            Player.CreatedBy = User.Identity?.Name ?? "system";
            Player.TimeCreated = DateTime.UtcNow;
            Player.ModifiedBy = Player.CreatedBy;
            Player.TimeModified = Player.TimeCreated;
            Player.Gid = Guid.NewGuid();
            Player.Inactive = false;
            Player.FkBgdClub = SelectedClubIds.FirstOrDefault() == 0 ? null : SelectedClubIds.First();

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                foreach (var error in errors)
                {
                    Console.WriteLine($"ModelState error: {error}");
                }

                ViewData["ModelErrors"] = errors;
                await LoadClubOptionsAsync();
                return Page();
            }

            // 1. Save Player to SQL
            _context.Players.Add(Player);
            await _context.SaveChangesAsync();

            await SyncPlayerClubsAsync(Player.Id, SelectedClubIds, Player.CreatedBy, Player.TimeCreated);

            // 2. Handle Image Upload to MongoDB
            if (Upload != null && Upload.Length > 0)
            {
                using var ms = new MemoryStream();
                await Upload.CopyToAsync(ms);
                var imageBytes = ms.ToArray();

                var newImage = new BoardGameImages
                {
                    GID = Player.Gid,
                    SQLTable = "bgd.Player", // Matches the Player table schema
                    ImageBytes = imageBytes,
                    ContentType = Upload.ContentType,
                    Description = "Profile Picture"
                };

                await _boardGameImages.InsertOneAsync(newImage);
            }

            return RedirectToPage("./Index");
        }

        private async Task SyncPlayerClubsAsync(long playerId, IEnumerable<long> selectedClubIds, string actor, DateTime now)
        {
            var clubIds = selectedClubIds.Distinct().ToHashSet();
            if (!clubIds.Any())
            {
                return;
            }

            var validClubIds = await _context.Clubs
                .Where(c => !c.Inactive && clubIds.Contains(c.Id))
                .Select(c => c.Id)
                .ToListAsync();

            foreach (var clubId in validClubIds)
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

            await _context.SaveChangesAsync();
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

        private async Task<bool> EnsureCanManagePlayersAsync()
        {
            if (User.IsInRole("Admin")) return true;

            var manageableClubIds = await GetManageableClubIdsAsync();
            return manageableClubIds.Count > 0;
        }

        private async Task<List<long>> GetManageableClubIdsAsync()
        {
            if (User.IsInRole("Admin"))
            {
                return await _context.Clubs
                    .AsNoTracking()
                    .Where(c => !c.Inactive)
                    .Select(c => c.Id)
                    .ToListAsync();
            }

            var currentClub = await _currentClubService.GetCurrentClubAsync();
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
}

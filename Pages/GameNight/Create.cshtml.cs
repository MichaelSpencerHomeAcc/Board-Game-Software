using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Board_Game_Software.Models;

namespace Board_Game_Software.Pages.GameNight
{
    public class CreateModel : PageModel
    {
        // TODO: Replace 'AppDbContext' with your actual DbContext type
        private readonly BoardGameDbContext _db;

        public CreateModel(BoardGameDbContext db)
        {
            _db = db;
        }

        // Lightweight row used to render the checkboxes
        public List<PlayerRow> AllPlayers { get; private set; } = new();

        [BindProperty]
        public CreateInput Input { get; set; } = new();

        public async Task OnGetAsync()
        {
            // Default to today (server local) for convenience
            Input.GameNightDate = DateOnly.FromDateTime(DateTime.Today);

            AllPlayers = await _db.Set<Player>()
                .Where(p => !p.Inactive)
                .Select(p => new PlayerRow
                {
                    PlayerId = p.Id,
                    Name = ((p.FirstName ?? "") + " " + (p.LastName ?? "")).Trim()
                })
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Input.SelectedPlayerIds ??= new List<long>();

            if (!ModelState.IsValid)
            {
                await ReloadPlayersAsync();
                return Page();
            }

            var now = DateTime.UtcNow;
            var userName = User?.Identity?.Name ?? "system";

            var night = new BoardGameNight
            {
                Gid = Guid.NewGuid(),
                Inactive = false,
                GameNightDate = Input.GameNightDate,
                Finished = false,
                TimeCreated = now,
                TimeModified = now,
                CreatedBy = userName,
                ModifiedBy = userName
            };

            _db.Add(night);
            await _db.SaveChangesAsync();

            if (Input.SelectedPlayerIds.Any())
            {
                var links = Input.SelectedPlayerIds.Distinct().Select(pid => new BoardGameNightPlayer
                {
                    Gid = Guid.NewGuid(),
                    Inactive = false,
                    TimeCreated = now,
                    TimeModified = now,
                    CreatedBy = userName,
                    ModifiedBy = userName,
                    FkBgdBoardGameNight = night.Id,
                    FkBgdPlayer = pid
                });

                _db.AddRange(links);
                await _db.SaveChangesAsync();
            }

            return RedirectToPage("/GameNight/Details", new { id = night.Id });
        }

        private async Task ReloadPlayersAsync()
        {
            AllPlayers = await _db.Set<Player>()
                .Where(p => !p.Inactive)
                .Select(p => new PlayerRow
                {
                    PlayerId = p.Id,
                    Name = ((p.FirstName ?? "") + " " + (p.LastName ?? "")).Trim(),
                    Preselected = Input.SelectedPlayerIds != null && Input.SelectedPlayerIds.Contains(p.Id)
                })
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public class CreateInput
        {
            [Required]
            public DateOnly GameNightDate { get; set; }

            public List<long> SelectedPlayerIds { get; set; } = new();
        }

        public class PlayerRow
        {
            public long PlayerId { get; set; }
            public string Name { get; set; } = string.Empty;
            public bool Preselected { get; set; }
        }
    }
}
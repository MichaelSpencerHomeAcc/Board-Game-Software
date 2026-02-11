using System.Globalization;
using System.Linq;
using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Board_Game_Software.Pages.GameNight
{
    public class AddPlayersModel : PageModel
    {
        private readonly BoardGameDbContext _db;

        public AddPlayersModel(BoardGameDbContext db) => _db = db;

        [BindProperty] public InputModel Input { get; set; } = new();
        public List<PlayerRow> AllPlayers { get; private set; } = new();

        public class InputModel
        {
            public long NightId { get; set; }
            public string? ReturnUrl { get; set; }
            public List<long> SelectedPlayerIds { get; set; } = new();
        }

        public class PlayerRow
        {
            public long PlayerId { get; set; }
            public string Name { get; set; } = string.Empty;
            public bool Preselected { get; set; }
            public string Initials { get; set; } = string.Empty; 
        }


        public async Task<IActionResult> OnGetAsync(long id, string? returnUrl)
        {
            Input.NightId = id;
            Input.ReturnUrl = returnUrl;

            var nightExists = await _db.BoardGameNights
                .AsNoTracking()
                .AnyAsync(n => n.Id == id && !n.Inactive);
            if (!nightExists) return NotFound();

            var existingIds = await _db.BoardGameNightPlayers
                .AsNoTracking()
                .Where(p => p.FkBgdBoardGameNight == id && !p.Inactive)
                .Select(p => p.FkBgdPlayer)
                .ToListAsync();

            AllPlayers = await _db.Set<Player>()
                .AsNoTracking()
                .Where(p => !p.Inactive && !existingIds.Contains(p.Id))
                .Select(p => new PlayerRow
                {
                    PlayerId = p.Id,
                    Name = ((p.FirstName ?? "") + " " + (p.LastName ?? "")).Trim(),
                    Initials = BuildInitials(p.FirstName, p.LastName),
                    Preselected = false
                })
                .OrderBy(p => p.Name)
                .ToListAsync();

            return Page();
        }


        public async Task<IActionResult> OnPostAsync()
        {
            var ids = Input.SelectedPlayerIds.Distinct().ToList();

            if (ids.Any())
            {
                var now = DateTime.UtcNow;
                var user = User?.Identity?.Name ?? "system";

                var links = ids.Select(pid => new BoardGameNightPlayer
                {
                    Gid = Guid.NewGuid(),
                    Inactive = false,
                    TimeCreated = now,
                    TimeModified = now,
                    CreatedBy = user,
                    ModifiedBy = user,
                    FkBgdBoardGameNight = Input.NightId,
                    FkBgdPlayer = pid
                });

                _db.AddRange(links);
                await _db.SaveChangesAsync();
            }

            if (!string.IsNullOrEmpty(Input.ReturnUrl) && Url.IsLocalUrl(Input.ReturnUrl))
                return Redirect(Input.ReturnUrl);

            return RedirectToPage("/GameNight/Details", new { id = Input.NightId });
        }

        private static string BuildInitials(string? first, string? last)
        {
            var f = (first ?? "").Trim();
            var l = (last ?? "").Trim();
            if (string.IsNullOrEmpty(l))
                return f.Length > 0 ? char.ToUpper(f[0], CultureInfo.CurrentCulture).ToString() : "?";

            var a = f.Length > 0 ? char.ToUpper(f[0], CultureInfo.CurrentCulture) : '?';
            var b = char.ToUpper(l[0], CultureInfo.CurrentCulture);
            return $"{a}{b}";
        }
    }
}

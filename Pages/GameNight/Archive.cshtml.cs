using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Board_Game_Software.Pages.GameNight
{
    public class ArchiveModel : PageModel
    {
        private readonly BoardGameDbContext _db;

        public ArchiveModel(BoardGameDbContext db)
        {
            _db = db;
        }

        public List<VwBoardGameNight> ArchivedNights { get; set; } = new();

        public async Task OnGetAsync()
        {
            var cutoffDate = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-3);

            // Fetch only finished games older than 3 months
            ArchivedNights = await _db.VwBoardGameNights
                .AsNoTracking()
                .Where(n => n.Finished && n.GameNightDate < cutoffDate)
                .OrderByDescending(n => n.GameNightDate)
                .ToListAsync();
        }
    }
}
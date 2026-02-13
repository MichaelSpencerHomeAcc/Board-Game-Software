using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Board_Game_Software.Pages.GameNight
{
    public class IndexModel : PageModel
    {
        private readonly BoardGameDbContext _db;

        public IndexModel(BoardGameDbContext db)
        {
            _db = db;
        }

        public List<VwBoardGameNight> Nights { get; set; } = new();

        public async Task OnGetAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            // Calculate the date 3 months ago
            var cutoffDate = today.AddMonths(-3);

            var query = _db.VwBoardGameNights.AsNoTracking();

            // Filter out Finished games that are older than 3 months
            // Logic: Keep it if it's NOT finished OR if it's newer than the cutoff
            var items = await query
                .Where(n => !n.Finished || n.GameNightDate >= cutoffDate)
                .ToListAsync();

            // Sorting logic remains the same to keep Upcoming at the top
            Nights = items
                .OrderBy(n => n.Inactive)
                .ThenByDescending(n => !n.Finished && n.GameNightDate >= today)
                .ThenByDescending(n => !n.Finished && n.GameNightDate < today)
                .ThenBy(n => n.Finished)
                .ThenByDescending(n => n.GameNightDate)
                .ToList();
        }
    }
}
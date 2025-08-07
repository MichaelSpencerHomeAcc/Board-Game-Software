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
        private readonly BoardGameDbContext _db; // Replace with your actual DbContext type

        public IndexModel(BoardGameDbContext db)
        {
            _db = db;
        }

        public List<VwBoardGameNight> Nights { get; set; } = new();

        public async Task OnGetAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var items = await _db.VwBoardGameNights
                .AsNoTracking()
                .ToListAsync();

            Nights = items
                .OrderByDescending(n => !n.Finished && n.GameNightDate >= today) // upcoming first
                .ThenByDescending(n => !n.Finished && n.GameNightDate < today)   // past unfinished
                .ThenBy(n => n.Finished)                                        // finished last
                .ThenBy(n => n.GameNightDate)                                   // sort within bucket
                .ToList();
        }
    }
}

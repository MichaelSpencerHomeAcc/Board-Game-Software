using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Board_Game_Software.Models;

namespace Board_Game_Software.Controllers
{
    public class BoardGameNightController : Controller
    {
        private readonly BoardGameDbContext _db;

        public BoardGameNightController(BoardGameDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var items = await _db.VwBoardGameNights
                .AsNoTracking()
                .ToListAsync();

            // Upcoming (not finished, date >= today), then Past Unfinished, then Finished (most recent first in each bucket)
            var model = items
                .OrderByDescending(n => !n.Finished && n.GameNightDate >= today) // upcoming first
                .ThenByDescending(n => !n.Finished && n.GameNightDate < today)   // then past unfinished
                .ThenBy(n => n.Finished)                                        // finally finished
                .ThenBy(n => n.GameNightDate)                                   // within bucket, sort by date
                .ToList();

            return View(model);
        }
    }
}

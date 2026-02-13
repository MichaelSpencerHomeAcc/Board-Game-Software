using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Board_Game_Software.Models;

namespace Board_Game_Software.Pages
{
    public class IndexModel : PageModel
    {
        private readonly BoardGameDbContext _context;

        public IndexModel(BoardGameDbContext context)
        {
            _context = context;
        }

        public int TotalGames { get; set; }
        public int TotalPlayers { get; set; }
        public int TotalGameNights { get; set; }

        public async Task OnGetAsync()
        {
            // Fetch real counts from the database
            TotalGames = await _context.BoardGames.CountAsync(g => !g.Inactive);
            TotalPlayers = await _context.Players.CountAsync(p => !p.Inactive);
            TotalGameNights = await _context.BoardGameNights.CountAsync(n => !n.Inactive);
        }
    }
}
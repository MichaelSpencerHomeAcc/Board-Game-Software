using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Board_Game_Software.Data;
using Board_Game_Software.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Board_Game_Software.Pages.DataSetup.Players
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly BoardGameDbContext _context;

        public IndexModel(BoardGameDbContext context)
        {
            _context = context;
        }
        public IList<VwPlayer> Players { get; set; } = default!;

        public async Task OnGetAsync()
        {
            Players = await _context.VwPlayers
                .OrderBy(p => p.FullName)
                .ToListAsync();
        }
    }
}
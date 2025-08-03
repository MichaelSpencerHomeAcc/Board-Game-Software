using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Board_Game_Software.Pages.DataSetup.BoardGameMarkerTypes
{
    public class AddModel : PageModel
    {
        private readonly BoardGameDbContext _context;

        [BindProperty]
        public BoardGameMarkerType MarkerType { get; set; } = new();

        public SelectList AlignmentTypes { get; set; } = null!;

        public AddModel(BoardGameDbContext context)
        {
            _context = context;
        }

        public async Task OnGetAsync()
        {
            await PopulateAlignmentTypesSelectList(null);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await PopulateAlignmentTypesSelectList(MarkerType.FkBgdMarkerAlignmentType);
                return Page();
            }

            MarkerType.CreatedBy = User.Identity?.Name ?? "system";
            MarkerType.TimeCreated = DateTime.UtcNow;
            MarkerType.ModifiedBy = MarkerType.CreatedBy;
            MarkerType.TimeModified = MarkerType.TimeCreated;
            MarkerType.Gid = Guid.NewGuid();
            MarkerType.Inactive = false;

            _context.BoardGameMarkerTypes.Add(MarkerType);
            await _context.SaveChangesAsync();

            return RedirectToPage("Index");
        }

        private async Task PopulateAlignmentTypesSelectList(long? selectedId)
        {
            var alignmentTypes = await _context.MarkerAlignmentTypes
                .Where(a => !a.Inactive)
                .OrderBy(a => a.TypeDesc)
                .ToListAsync();

            AlignmentTypes = new SelectList(alignmentTypes, "Id", "TypeDesc", selectedId);
        }
    }
}

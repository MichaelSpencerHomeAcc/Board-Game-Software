using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Board_Game_Software.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Board_Game_Software.Pages.DataSetup.BoardGameMarkerTypes
{
    public class AddAdditionalTypeModel : PageModel
    {
        private readonly BoardGameDbContext _context;

        public AddAdditionalTypeModel(BoardGameDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string TypeDesc { get; set; }

        public List<string> ExistingTypes { get; set; } = new List<string>();

        public string Message { get; set; }

        public async Task OnGetAsync()
        {
            ExistingTypes = await _context.MarkerAdditionalTypes
                .Where(t => !t.Inactive)
                .OrderBy(t => t.TypeDesc)
                .Select(t => t.TypeDesc.Trim())
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Reload existing types for redisplay
            ExistingTypes = await _context.MarkerAdditionalTypes
                .Where(t => !t.Inactive)
                .OrderBy(t => t.TypeDesc)
                .Select(t => t.TypeDesc.Trim())
                .ToListAsync();

            if (string.IsNullOrWhiteSpace(TypeDesc))
            {
                ModelState.AddModelError("TypeDesc", "Additional Type Name is required.");
                TypeDesc = string.Empty;
                ModelState.Remove("TypeDesc");
                return Page();
            }

            var trimmedInput = TypeDesc.Trim();

            bool exists = ExistingTypes.Any(t => string.Equals(t, trimmedInput, StringComparison.OrdinalIgnoreCase));

            if (exists)
            {
                ModelState.AddModelError("TypeDesc", "This Additional Type already exists.");
                TypeDesc = string.Empty;
                ModelState.Remove("TypeDesc");
                return Page();
            }

            var newType = new MarkerAdditionalType
            {
                TypeDesc = trimmedInput,
                Inactive = false,
                CreatedBy = User.Identity?.Name ?? "system",
                TimeCreated = DateTime.UtcNow,
                ModifiedBy = User.Identity?.Name ?? "system",
                TimeModified = DateTime.UtcNow
            };

            _context.MarkerAdditionalTypes.Add(newType);
            await _context.SaveChangesAsync();

            Message = $"Successfully added '{newType.TypeDesc}'";
            TypeDesc = string.Empty;
            ModelState.Remove("TypeDesc");

            // Refresh list to include the new one
            ExistingTypes = await _context.MarkerAdditionalTypes
                .Where(t => !t.Inactive)
                .OrderBy(t => t.TypeDesc)
                .Select(t => t.TypeDesc.Trim())
                .ToListAsync();

            return Page();
        }

    }
}

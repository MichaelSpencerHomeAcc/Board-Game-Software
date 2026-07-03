using Board_Game_Software.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Board_Game_Software.Pages.Clubs;

[Authorize]
public class SwitchModel : PageModel
{
    private readonly ICurrentClubService _currentClubService;

    public SwitchModel(ICurrentClubService currentClubService)
    {
        _currentClubService = currentClubService;
    }

    [BindProperty]
    public long? ClubId { get; set; }

    [BindProperty]
    public string? ReturnUrl { get; set; }

    public IActionResult OnGet()
    {
        return RedirectToPage("/Index");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ClubId.HasValue)
        {
            if (!User.IsInRole("Admin"))
            {
                return Forbid();
            }

            _currentClubService.SetPlatformAdminMode();
            return RedirectBack();
        }

        var changed = await _currentClubService.SetCurrentClubAsync(ClubId.Value);
        if (!changed)
        {
            return Forbid();
        }

        return RedirectBack();
    }

    private IActionResult RedirectBack()
    {
        if (!string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
        {
            return LocalRedirect(ReturnUrl);
        }

        return RedirectToPage("/Index");
    }
}

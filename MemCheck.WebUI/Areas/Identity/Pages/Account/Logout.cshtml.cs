using MemCheck.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace MemCheck.WebUI.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class LogoutModel : PageModel
{
    private readonly SignInManager<MemCheckUser> _signInManager;

    public LogoutModel(SignInManager<MemCheckUser> signInManager)
    {
        _signInManager = signInManager;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        await _signInManager.SignOutAsync();
        return LocalRedirect("/");
    }

    public async Task<IActionResult> OnPost(string returnAddress = "/")
    {
        await _signInManager.SignOutAsync();
        return returnAddress != null ? LocalRedirect(returnAddress) : RedirectToPage();
    }
}

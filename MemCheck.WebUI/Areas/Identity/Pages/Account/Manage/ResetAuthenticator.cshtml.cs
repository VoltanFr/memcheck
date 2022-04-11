using MemCheck.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace MemCheck.WebUI.Areas.Identity.Pages.Account.Manage
{
    public class ResetAuthenticatorModel : PageModel
    {
        private readonly UserManager<MemCheckUser> _userManager;
        private readonly SignInManager<MemCheckUser> _signInManager;

        public ResetAuthenticatorModel(UserManager<MemCheckUser> userManager, SignInManager<MemCheckUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [TempData]
        public string StatusMessage { get; set; } = null!;

        public async Task<IActionResult> OnGet()
        {
            var user = await _userManager.GetUserAsync(User);
            return user == null ? NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.") : Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await _userManager.SetTwoFactorEnabledAsync(user, false);
            await _userManager.ResetAuthenticatorKeyAsync(user);

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your authenticator app key has been reset, you will need to configure your authenticator app using the new key.";

            return RedirectToPage("./EnableAuthenticator");
        }
    }
}
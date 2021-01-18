using MemCheck.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace MemCheck.WebUI.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterConfirmationModel : PageModel
    {
        private readonly UserManager<MemCheckUser> _userManager;

        public RegisterConfirmationModel(UserManager<MemCheckUser> userManager)
        {
            _userManager = userManager;
        }

        public string UserName { get; set; } = null!;

        public bool DisplayConfirmAccountLink { get; set; }

        public string EmailConfirmationUrl { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(string userName)
        {
            if (userName == null)
            {
                return RedirectToPage("/Index");
            }

            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
            {
                return NotFound($"Unable to load user with name '{userName}'.");
            }

            UserName = userName;

            return Page();
        }
    }
}

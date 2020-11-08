using Microsoft.AspNetCore.Authorization;
using System.Text;
using System.Threading.Tasks;
using MemCheck.Domain;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Identity;

namespace MemCheck.WebUI.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterConfirmationModel : PageModel
    {
        private readonly UserManager<MemCheckUser> _userManager;
        private readonly IEmailSender _sender;

        public RegisterConfirmationModel(UserManager<MemCheckUser> userManager, IEmailSender sender)
        {
            _userManager = userManager;
            _sender = sender;
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

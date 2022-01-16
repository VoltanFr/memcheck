using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using System.Threading.Tasks;

namespace MemCheck.WebUI.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterConfirmationModel : PageModel
    {
        public RegisterConfirmationModel(SendGridEmailSender sendGridEmailSender)
        {
            SendGridEmailSender = sendGridEmailSender.Sender;
        }
        [BindProperty] public string SendGridEmailSender { get; set; } = null!;
        [BindProperty(SupportsGet = true)] public string UserAddress { get; set; } = null!;
        public async Task<IActionResult> OnGetAsync(string userAddress)
        {
            await Task.CompletedTask;

            if (userAddress == null)
                return RedirectToPage("./Login");

            UserAddress = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(userAddress));

            return Page();
        }
    }
}

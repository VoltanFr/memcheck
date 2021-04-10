using MemCheck.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace MemCheck.WebUI.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ResendEmailConfirmModel : PageModel
    {
        private readonly UserManager<MemCheckUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly IStringLocalizer<ResendEmailConfirmModel> localizer;
        private readonly ILogger<RegisterModel> logger;

        public ResendEmailConfirmModel(UserManager<MemCheckUser> userManager, IEmailSender emailSender, IStringLocalizer<ResendEmailConfirmModel> localizer, ILogger<RegisterModel> logger)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            this.localizer = localizer;
            this.logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = null!;

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = null!;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!_userManager.Options.SignIn.RequireConfirmedAccount)
                return RedirectToPage("./Login");

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);

                if (user == null)
                {
                    // Don't reveal that the user does not exist
                    return RedirectToPage("./Login");
                }

                if (await _userManager.IsEmailConfirmedAsync(user))
                {
                    // Don't reveal that the user is confirmed
                    return RedirectToPage("./Login");
                }

                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { area = "Identity", userId = user.Id, code },
                    protocol: Request.Scheme);

                var hello = localizer["Hello"];
                var url = HtmlEncoder.Default.Encode(callbackUrl);
                var linkText = localizer["PleaseConfirmYourAccount"];
                var body = $"<p>{hello} {user.UserName}</p><p><a href='{url}'>{linkText}</a>.";

                await _emailSender.SendEmailAsync(Input.Email, localizer["ConfirmYourEmail"], body);

                logger.LogInformation("EMail confirmation message resent.");

                return RedirectToPage("RegisterConfirmation", new { userName = user.UserName });
            }

            return Page();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MemCheck.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace MemCheck.WebUI.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ConfirmEmailModel : PageModel
    {
        private readonly UserManager<MemCheckUser> _userManager;
        private readonly IStringLocalizer<ConfirmEmailModel> localizer;
        private readonly IEmailSender emailSender;

        private async Task SendWelcomeMailAsync(MemCheckUser user)
        {
            var hello = localizer["Hello"];
            var welcome = localizer["Welcome"];
            var docLine = localizer["DocLine"];
            var p = Url.Page(pageName: "/Doc/MdRenderer", pageHandler: null, values: null, protocol: Request.Scheme);
            var docUrl = HtmlEncoder.Default.Encode(p);
            var docLinkText = localizer["DocLinkText"];
            var appLine = localizer["AppLine"];
            var appUrl = HtmlEncoder.Default.Encode(Url.Page(pageName: "/Index", pageHandler: null, values: null, protocol: Request.Scheme));
            var appLinkText = localizer["AppLinkText"];
            var thank = localizer["Thank"];
            var body = $"<p>{hello} {user.UserName}</p><p>{welcome}</p><p>{docLine} <a href='{docUrl}'>{docLinkText}</a>.</p><p>{appLine} <a href='{appUrl}'>{appLinkText}</a>.</p><p>{thank}.</p>";
            await emailSender.SendEmailAsync(user.Email, localizer["WelcomeMailSubject"], body);
        }

        public ConfirmEmailModel(UserManager<MemCheckUser> userManager, IStringLocalizer<ConfirmEmailModel> localizer, IEmailSender emailSender)
        {
            _userManager = userManager;
            this.localizer = localizer;
            this.emailSender = emailSender;
        }

        [TempData]
        public string StatusMessage { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(string userId, string code)
        {
            if (userId == null || code == null)
                return RedirectToPage("./Login");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound($"Unable to load user with ID '{userId}'.");

            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await _userManager.ConfirmEmailAsync(user, code);
            StatusMessage = result.Succeeded ? localizer["ThankYou"] : localizer["Error"];

            await SendWelcomeMailAsync(user);

            return Page();
        }
    }
}

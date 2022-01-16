﻿using MemCheck.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace MemCheck.WebUI.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ResendEmailConfirmModel : PageModel
    {
        #region Fields
        private readonly UserManager<MemCheckUser> userManager;
        private readonly IEmailSender emailSender;
        private readonly IStringLocalizer<ResendEmailConfirmModel> localizer;
        private readonly ILogger<RegisterModel> logger;
        #endregion
        public ResendEmailConfirmModel(UserManager<MemCheckUser> userManager, IEmailSender emailSender, IStringLocalizer<ResendEmailConfirmModel> localizer, ILogger<RegisterModel> logger)
        {
            this.userManager = userManager;
            this.emailSender = emailSender;
            this.localizer = localizer;
            this.logger = logger;
        }

        [BindProperty] public InputModel Input { get; set; } = null!;

        public class InputModel
        {
            [Required, EmailAddress] public string Email { get; set; } = null!;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!userManager.Options.SignIn.RequireConfirmedAccount)
                return RedirectToPage("./Login");

            if (ModelState.IsValid)
            {
                var normalizedInputEmail = userManager.NormalizeEmail(Input.Email);
                var users = userManager.Users.Where(u => u.NormalizedEmail == normalizedInputEmail && !u.EmailConfirmed && u.DeletionDate == null);
                //Not sure of perf when there are a lot of users
                //If no user exists or user is confirmed, we don't reveal that information because it could be used for attacks

                foreach (var user in users)
                {
                    var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page("/Account/ConfirmEmail", pageHandler: null, values: new { area = "Identity", userId = user.Id, code }, protocol: Request.Scheme)!;

                    var url = HtmlEncoder.Default.Encode(callbackUrl);

                    var mailBody = new StringBuilder();
                    mailBody.Append($"<p>{localizer["Hello"].Value} {user.UserName}</p>");
                    mailBody.Append($"<p>{localizer["BeforeHyperLink"].Value}</p>");
                    mailBody.Append($"<p><a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>{localizer["HyperLinkText"].Value}</a></p>");
                    mailBody.Append($"<p>{localizer["AfterHyperLink"].Value}</p>");
                    mailBody.Append($"<p>{localizer["Final"].Value}</p>");

                    await emailSender.SendEmailAsync(Input.Email, localizer["MailSubject"], mailBody.ToString());

                    logger.LogInformation($"EMail confirmation message resent for user '{user.UserName}'.");
                }
            }

            return RedirectToPage("RegisterConfirmation");
        }
    }
}

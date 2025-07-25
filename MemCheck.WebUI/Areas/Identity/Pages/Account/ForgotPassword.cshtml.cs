using MemCheck.AzureComponents;
using MemCheck.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace MemCheck.WebUI.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class ForgotPasswordModel : PageModel
{
    #region Fields
    private readonly UserManager<MemCheckUser> userManager;
    private readonly IMemCheckMailSender emailSender;
    private readonly IStringLocalizer<ForgotPasswordModel> localizer;
    #endregion

    public ForgotPasswordModel(UserManager<MemCheckUser> userManager, IMemCheckMailSender emailSender, IStringLocalizer<ForgotPasswordModel> localizer)
    {
        this.userManager = userManager;
        this.emailSender = emailSender;
        this.localizer = localizer;
    }

    [BindProperty] public InputModel Input { get; set; } = null!;

    public class InputModel
    {
        [Required] public string UserName { get; set; } = null!;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (ModelState.IsValid)
        {
            var user = await userManager.FindByNameAsync(Input.UserName);
            if (user == null || !await userManager.IsEmailConfirmedAsync(user) || user.DeletionDate != null)
                // Don't reveal that the user does not exist or is not confirmed
                return RedirectToPage("./ForgotPasswordConfirmation");

            // For more information on how to enable account confirmation and password reset please 
            // visit https://go.microsoft.com/fwlink/?LinkID=532713
            var code = await userManager.GeneratePasswordResetTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page("/Account/ResetPassword", pageHandler: null, values: new { area = "Identity", code }, protocol: Request.Scheme)!;

            var body = $"<p>{localizer["Hello"]} {user.UserName}.</p><p>{localizer["MailPhrasePart1"]} <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>{localizer["MailLinkText"]}</a> {localizer["MailPhrasePart2"]}.</p>";

            await emailSender.SendAsync(user.GetEmail(), localizer["MailSubject"], body);

            return RedirectToPage("./ForgotPasswordConfirmation");
        }

        return Page();
    }
}

using MemCheck.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace MemCheck.WebUI.Areas.Identity.Pages.Account.Manage;

public partial class EmailModel : PageModel
{
    private readonly UserManager<MemCheckUser> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly IStringLocalizer<EmailModel> localizer;

    public EmailModel(
        UserManager<MemCheckUser> userManager,
        IEmailSender emailSender,
        IStringLocalizer<EmailModel> localizer)
    {
        _userManager = userManager;
        _emailSender = emailSender;
        this.localizer = localizer;
    }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public bool IsEmailConfirmed { get; set; }

    [TempData]
    public string StatusMessage { get; set; } = null!;

    [BindProperty]
    public InputModel Input { get; set; } = null!;

    public class InputModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "New email")]
        public string NewEmail { get; set; } = null!;
    }

    private async Task LoadAsync(MemCheckUser user)
    {
        var email = await _userManager.GetEmailAsync(user);
        Email = email ?? "";

        Input = new InputModel
        {
            NewEmail = email ?? "",
        };

        IsEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        await LoadAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostChangeEmailAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

        if (!ModelState.IsValid)
        {
            await LoadAsync(user);
            return Page();
        }

        var email = await _userManager.GetEmailAsync(user);
        if (Input.NewEmail != email)
        {
            var userId = await _userManager.GetUserIdAsync(user);
            var code = await _userManager.GenerateChangeEmailTokenAsync(user, Input.NewEmail);
            var callbackUrl = Url.Page("/Account/ConfirmEmailChange", pageHandler: null, values: new { userId, email = Input.NewEmail, code }, protocol: Request.Scheme)!;

            var mailBody = new StringBuilder();
            mailBody.Append(CultureInfo.InvariantCulture, $"<p>{localizer["Hello"].Value} {user.UserName}</p>");
            mailBody.Append(CultureInfo.InvariantCulture, $"<p>{localizer["PleaseConfirmYourMemcheckAccountBy"].Value} <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>{localizer["ClickingHere"].Value}</a>.</p>");
            mailBody.Append(CultureInfo.InvariantCulture, $"<p>{localizer["ThankYou"].Value}</p>");

            await _emailSender.SendEmailAsync(Input.NewEmail, localizer["ConfirmYourEmail"].Value, mailBody.ToString());

            StatusMessage = "Confirmation link to change email sent. Please check your email.";
            return RedirectToPage();
        }

        StatusMessage = "Your email is unchanged.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSendVerificationEmailAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

        if (!ModelState.IsValid)
        {
            await LoadAsync(user);
            return Page();
        }

        var userId = await _userManager.GetUserIdAsync(user);
        var email = user.GetEmail();
        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
        var callbackUrl = Url.Page("/Account/ConfirmEmail", pageHandler: null, values: new { area = "Identity", userId, code }, protocol: Request.Scheme)!;

        var mailBody = new StringBuilder();
        mailBody.Append(CultureInfo.InvariantCulture, $"<p>{localizer["Hello"].Value} {user.UserName}</p>");
        mailBody.Append(CultureInfo.InvariantCulture, $"<p>{localizer["PleaseConfirmYourMemcheckAccountBy"].Value} <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>{localizer["ClickingHere"].Value}</a>.</p>");
        mailBody.Append(CultureInfo.InvariantCulture, $"<p>{localizer["ThankYou"].Value}</p>");

        await _emailSender.SendEmailAsync(email, localizer["ConfirmYourEmail"].Value, mailBody.ToString());

        StatusMessage = "Verification email sent. Please check your email.";
        return RedirectToPage();
    }
}

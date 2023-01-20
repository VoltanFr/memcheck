using MemCheck.Domain;
using Microsoft.AspNetCore.Authorization;
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

namespace MemCheck.WebUI.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class RegisterModel : PageModel
{
    private readonly SignInManager<MemCheckUser> _signInManager;
    private readonly UserManager<MemCheckUser> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly IStringLocalizer<RegisterModel> localizer;

    public RegisterModel(UserManager<MemCheckUser> userManager, SignInManager<MemCheckUser> signInManager, IEmailSender emailSender, IStringLocalizer<RegisterModel> localizer)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailSender = emailSender;
        this.localizer = localizer;
    }

    [BindProperty]
    public InputModel Input { get; set; } = null!;

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        public string UserName { get; set; } = null!;

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = null!;
    }

    public async Task OnGetAsync()
    {
        await Task.CompletedTask;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (ModelState.IsValid)
        {
            var user = new MemCheckUser { UserName = Input.UserName, Email = Input.Email };
            var result = await _userManager.CreateAsync(user, Input.Password);
            if (result.Succeeded)
            {
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { area = "Identity", userId = user.Id, code },
                    protocol: Request.Scheme)!;

                var mailBody = new StringBuilder();
                mailBody.Append(CultureInfo.InvariantCulture, $"<p>{localizer["Hello"].Value} {user.UserName}</p>");
                mailBody.Append(CultureInfo.InvariantCulture, $"<p>{localizer["BeforeHyperLink"].Value}</p>");
                mailBody.Append(CultureInfo.InvariantCulture, $"<p><a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>{localizer["HyperLinkText"].Value}</a></p>");
                mailBody.Append(CultureInfo.InvariantCulture, $"<p>{localizer["AfterHyperLink"].Value}</p>");
                mailBody.Append(CultureInfo.InvariantCulture, $"<p>{localizer["Final"].Value}</p>");

                await _emailSender.SendEmailAsync(Input.Email, localizer["MailSubject"].Value, mailBody.ToString());

                if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    return RedirectToPage("RegisterConfirmation", new { UserAddress = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(Input.Email)) });
                else
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect("/");
                }
            }
            foreach (var error in result.Errors)
            {
                var errorMesgLocalized = localizer.GetString(error.Code);
                var errorMesg = errorMesgLocalized.ResourceNotFound
                    ? localizer.GetString(nameof(IdentityErrorDescriber.DefaultError)) + '(' + error.Code + ')'
                    : errorMesgLocalized.Value;
                ModelState.AddModelError(string.Empty, string.Format(CultureInfo.InvariantCulture, errorMesg, error.Description));
            }
        }

        // If we got this far, something failed, redisplay form
        return Page();
    }
}

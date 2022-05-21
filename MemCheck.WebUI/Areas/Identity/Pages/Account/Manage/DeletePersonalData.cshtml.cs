using MemCheck.Application;
using MemCheck.Application.QueryValidation;
using MemCheck.Application.Users;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace MemCheck.WebUI.Areas.Identity.Pages.Account.Manage;

public class DeletePersonalDataModel : PageModel
{
    #region Fields
    private readonly UserManager<MemCheckUser> _userManager;
    private readonly SignInManager<MemCheckUser> _signInManager;
    private readonly CallContext callContext;
    #endregion

    public DeletePersonalDataModel(MemCheckDbContext dbContext, UserManager<MemCheckUser> userManager, SignInManager<MemCheckUser> signInManager, TelemetryClient telemetryClient)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        callContext = new CallContext(dbContext, new MemCheckTelemetryClient(telemetryClient), new FakeStringLocalizer(), new ProdRoleChecker(userManager));
    }

    [BindProperty] public InputModel Input { get; set; } = null!;

    public class InputModel
    {
        [Required, DataType(DataType.Password)] public string Password { get; set; } = null!;
    }

    public bool RequirePassword { get; set; }

    public async Task<IActionResult> OnGet()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }
        RequirePassword = await _userManager.HasPasswordAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

        RequirePassword = await _userManager.HasPasswordAsync(user);
        if (RequirePassword)
            if (!await _userManager.CheckPasswordAsync(user, Input.Password))
            {
                ModelState.AddModelError(string.Empty, "Incorrect password.");
                return Page();
            }

        var deleter = new DeleteUserAccount(callContext, _userManager);
        await deleter.RunAsync(new DeleteUserAccount.Request(user.Id, user.Id));
        await _signInManager.SignOutAsync();

        return Redirect("~/");
    }
}

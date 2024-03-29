﻿using MemCheck.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace MemCheck.WebUI.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class LoginModel : PageModel
{
    #region Fields
    private readonly IStringLocalizer<LoginModel> localizer;
    private readonly SignInManager<MemCheckUser> _signInManager;
    #endregion

    public LoginModel(SignInManager<MemCheckUser> signInManager, IStringLocalizer<LoginModel> localizer)
    {
        this.localizer = localizer;
        _signInManager = signInManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = null!;

    [TempData]
    public string ErrorMessage { get; set; } = null!;

    public class InputModel
    {
        [Required]
        public string UserName { get; set; } = null!;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }

    public async Task OnGetAsync()
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
            ModelState.AddModelError(string.Empty, ErrorMessage);

        // Clear the existing external cookie to ensure a clean login process
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (ModelState.IsValid)
        {
            // This doesn't count login failures towards account lockout
            // To enable password failures to trigger account lockout, set lockoutOnFailure: true
            var result = await _signInManager.PasswordSignInAsync(Input.UserName, Input.Password, Input.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
                return LocalRedirect("/");

            if (result.RequiresTwoFactor)
                return RedirectToPage("./LoginWith2fa", new { Input.RememberMe });

            if (result.IsLockedOut)
                return RedirectToPage("./Lockout");

            ModelState.AddModelError(string.Empty, localizer["InvalidLoginAttempt"].Value);
            return Page();
        }

        // If we got this far, something failed, redisplay form
        return Page();
    }
}

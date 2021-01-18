using MemCheck.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace MemCheck.WebUI.Areas.Identity.Pages.Account.Manage
{
    public partial class IndexModel : PageModel
    {
        #region Fields
        private readonly UserManager<MemCheckUser> _userManager;
        private readonly SignInManager<MemCheckUser> _signInManager;
        private readonly IStringLocalizer<IndexModel> localizer;
        #endregion

        public IndexModel(
            UserManager<MemCheckUser> userManager,
            SignInManager<MemCheckUser> signInManager,
            IStringLocalizer<IndexModel> localizer)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            this.localizer = localizer;
        }



        public string Username { get; set; } = null!;
        public string UserEmail { get; set; } = null!;

        [TempData] public string StatusMessage { get; set; } = null!;

        [BindProperty] public string UILanguage { get; set; } = null!;

        [BindProperty] public bool SubscribeToCardOnEdit { get; set; } = false;

        [BindProperty] public bool SendNotificationsByEmail { get; set; } = false;

        [BindProperty, Range(1, 30, ErrorMessage = "Valeur incorrecte, doit être entre 1 et 30 jours")] public int MinimumCountOfDaysBetweenNotifs { get; set; } = 0; //I didn't manage to localize the error message

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

            UserEmail = user.Email;
            Username = await _userManager.GetUserNameAsync(user);
            UILanguage = user.UILanguage ?? "<Not stored>";
            MinimumCountOfDaysBetweenNotifs = user.MinimumCountOfDaysBetweenNotifs;
            SendNotificationsByEmail = user.MinimumCountOfDaysBetweenNotifs > 0;
            SubscribeToCardOnEdit = user.SubscribeToCardOnEdit;

            return Page();
        }
        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            user.MinimumCountOfDaysBetweenNotifs = SendNotificationsByEmail ? MinimumCountOfDaysBetweenNotifs : 0;
            user.SubscribeToCardOnEdit = SubscribeToCardOnEdit;

            await _userManager.UpdateAsync(user);

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = localizer["ProfileUpdated"].Value;
            return RedirectToPage();
        }
    }
}

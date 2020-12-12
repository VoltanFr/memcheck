using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MemCheck.WebUI.Areas.Identity.Pages.Account.Manage
{
    public sealed class EditSearchSubscriptionModel : PageModel
    {
        [BindProperty(SupportsGet = true)] public string Id { get; set; } = "";
        [BindProperty(SupportsGet = true)] public string ReturnUrl { get; set; } = "";
    }
}

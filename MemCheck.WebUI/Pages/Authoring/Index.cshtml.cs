using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MemCheck.WebUI.Pages.Authoring
{
    public sealed class IndexModel : PageModel
    {
        [BindProperty(SupportsGet = true)] public string CardId { get; set; } = "";
        [BindProperty(SupportsGet = true)] public string ReturnUrl { get; set; } = null!;
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MemCheck.WebUI.Pages.Media
{
    public sealed class DeleteModel : PageModel
    {
        [BindProperty(SupportsGet = true)] public string ImageId { get; set; } = "";
        [BindProperty(SupportsGet = true)] public string ReturnAddress { get; set; } = null!;
    }
}

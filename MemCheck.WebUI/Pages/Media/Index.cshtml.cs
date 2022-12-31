using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MemCheck.WebUI.Pages.Media;

public sealed class IndexModel : PageModel
{
    [BindProperty(SupportsGet = true)] public string ImageId { get; set; } = "";
}

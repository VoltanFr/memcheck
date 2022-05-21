using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MemCheck.WebUI.Pages.Tags;

public sealed class AuthoringViewModel : PageModel
{
    [BindProperty(SupportsGet = true)] public string TagId { get; set; } = "";
    [BindProperty(SupportsGet = true)] public string ReturnAddress { get; set; } = "";
}

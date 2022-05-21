using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MemCheck.WebUI.Pages.Learn;

public sealed class LearnViewModel : PageModel
{
    [BindProperty(SupportsGet = true)] public string LearnMode { get; set; } = "Unknown";
    [BindProperty(SupportsGet = true)] public string TagId { get; set; } = "";
}

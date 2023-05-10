using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MemCheck.WebUI.Pages.Authoring;

public sealed class DiscussionModel : PageModel
{
    [BindProperty(SupportsGet = true)] public string CardId { get; set; } = "";
}

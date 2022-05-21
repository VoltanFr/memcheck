using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MemCheck.WebUI.Pages.Search;

public class NewSearchModel : PageModel
{
    [BindProperty(SupportsGet = true)] public string DeckId { get; set; } = null!;
    [BindProperty(SupportsGet = true)] public string HeapId { get; set; } = null!;
    [BindProperty(SupportsGet = true)] public string TagFilter { get; set; } = null!;
}

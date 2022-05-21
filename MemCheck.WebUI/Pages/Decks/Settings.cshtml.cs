using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MemCheck.WebUI.Pages.Decks;

public sealed class SettingsModel : PageModel
{
    [BindProperty(SupportsGet = true)] public string DeckId { get; set; } = "";
}

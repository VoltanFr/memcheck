using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MemCheck.Application;
using MemCheck.Application.Heaping;
using MemCheck.Database;
using MemCheck.WebUI.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;

namespace MemCheck.WebUI.Pages.Tags
{
    public sealed class AuthoringViewModel : PageModel
    {
        [BindProperty(SupportsGet = true)] public string TagId { get; set; } = "";
        [BindProperty(SupportsGet = true)] public string ReturnUrl { get; set; } = "";
    }
}

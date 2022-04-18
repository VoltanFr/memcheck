using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System;

namespace MemCheck.WebUI.Controllers
{
    [Route("[controller]")]
    public class DocController : Controller
    {
        #region Private methods
        private string? GetRefererRoute()
        {
            var previousPageUrl = Request.Headers["Referer"].ToString();
            var hostIndex = previousPageUrl.IndexOf(Request.Host.Value, StringComparison.OrdinalIgnoreCase);
            if (hostIndex == -1)
                return null;
            var result = previousPageUrl[(hostIndex + Request.Host.Value.Length)..];
            var parameterIndex = result.IndexOf('?', StringComparison.Ordinal);
            return parameterIndex == -1 ? result : result[..parameterIndex];
        }
        #endregion
        [HttpGet("")]
        public IActionResult Default()
        {
            //We provide contextual doc for the previously active page if any, otherwise we return the doc root page in the GUI language
            var refererRoute = GetRefererRoute();
            var cultureName = HttpContext.Features.Get<IRequestCultureFeature>()!.RequestCulture.Culture.Name[..2];
            return RedirectToPage("/Doc/MdRenderer", new { refererRoute, cultureName });
        }
    }
}

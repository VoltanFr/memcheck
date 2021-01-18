﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace MemCheck.WebUI.Controllers
{
    [Route("[controller]")]
    public class DocController : Controller
    {
        #region Private methods
        private string? GetRefererRoute()
        {
            var previousPageUrl = Request.Headers["Referer"].ToString();
            var hostIndex = previousPageUrl.IndexOf(Request.Host.Value);
            if (hostIndex == -1)
                return null;
            var result = previousPageUrl.Substring(hostIndex + Request.Host.Value.Length);
            var parameterIndex = result.IndexOf('?');
            if (parameterIndex == -1)
                return result;
            return result.Substring(0, parameterIndex);
        }
        #endregion
        [HttpGet("")]
        public IActionResult Default()
        {
            //We provide contextual doc for the previously active page if any, otherwise we return the doc root page in the GUI language
            var refererRoute = GetRefererRoute();
            var cultureName = Request.HttpContext.Features.Get<IRequestCultureFeature>().RequestCulture.Culture.Name;
            return RedirectToPage("/Doc/MdRenderer", new { refererRoute, cultureName });
        }
    }
}

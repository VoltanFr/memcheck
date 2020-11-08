using MemCheck.Application;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.WebUI.Controllers
{
    [Route("[controller]")]
    public class UILanguagesController : Controller
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        private readonly UserManager<MemCheckUser> userManager;
        #endregion
        #region Private methods
        private IEnumerable<string> GetSupportedUILanguages()
        {
            var requestCulture = HttpContext.Features.Get<IRequestCultureFeature>();
            var provider = requestCulture.Provider as RequestCultureProvider;
            if (provider == null) throw new InvalidProgramException("requestCulture.Provider as RequestCultureProvider returns null");
            return provider.Options.SupportedUICultures.Select<System.Globalization.CultureInfo, string>(c => c.Name);
        }
        #endregion
        public UILanguagesController(MemCheckDbContext dbContext, UserManager<MemCheckUser> userManager) : base()
        {
            this.dbContext = dbContext;
            this.userManager = userManager;
        }
        [HttpGet("GetAvailableLanguages")] public IActionResult GetAvailableLanguages() => base.Ok(GetSupportedUILanguages());
        [HttpGet("GetActiveLanguage")]
        public IActionResult GetActiveLanguage()
        {
            var requestCulture = HttpContext.Features.Get<IRequestCultureFeature>();
            return Ok(requestCulture.RequestCulture.UICulture.Name);
        }
        [HttpPost("SetCulture/{culture}")]
        public async Task<IActionResult> SetCultureAsync(string culture)
        {
            var requestCulture = HttpContext.Features.Get<IRequestCultureFeature>();
            if (requestCulture.RequestCulture.UICulture.Name == culture)
                return Ok(false);

            Response.Cookies.Append(CookieRequestCultureProvider.DefaultCookieName, CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)), new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) });

            var user = await userManager.GetUserAsync(HttpContext.User);
            if (user != null)
                new SetUserUILanguage(dbContext, GetSupportedUILanguages()).Run(user, culture);

            return Ok(true);
        }
    }
}



//@inject IOptions < RequestLocalizationOptions > LocOptions

//@{
//    var requestCulture = Context.Features.Get<IRequestCultureFeature>();
//    var cultureItems = LocOptions.Value.SupportedUICultures
//        .Select(c => new SelectListItem { Value = c.Name, Text = c.DisplayName })
//        .ToList();
//    var returnUrl = string.IsNullOrEmpty(Context.Request.Path) ? "~/" : $"~{Context.Request.Path.Value}";
//}

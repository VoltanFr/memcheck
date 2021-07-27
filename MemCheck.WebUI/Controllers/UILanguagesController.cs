using MemCheck.Application.Languages;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
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
        private readonly SignInManager<MemCheckUser> signInManager;
        #endregion
        #region Private methods
        private static IEnumerable<string> GetSupportedUILanguages()
        {
            return MemCheckRequestCultureProvider.SupportedCultures.Select(c => c.Name);
        }
        #endregion
        public UILanguagesController(MemCheckDbContext dbContext, UserManager<MemCheckUser> userManager, SignInManager<MemCheckUser> signInManager) : base()
        {
            this.dbContext = dbContext;
            this.userManager = userManager;
            this.signInManager = signInManager;
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
            //For each request, the culture is determined by MemCheckRequestCultureProvider.DetermineProviderCultureResult.
            //Here we set the culture in the cookie and in the use account, as necessary
            //This method returns true if it changes the culture

            var cookieOk = false;

            if (Request.Cookies.TryGetValue(CookieRequestCultureProvider.DefaultCookieName, out var cultureCookie))
            {
                var cultureFromCookie = CookieRequestCultureProvider.ParseCookieValue(cultureCookie).UICultures.FirstOrDefault().Value;
                cookieOk = cultureFromCookie == culture;
            }

            if (!cookieOk)
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                if (user != null)
                {
                    await new SetUserUILanguage(dbContext).RunAsync(new SetUserUILanguage.Request(user.Id, culture));
                    await signInManager.RefreshSignInAsync(user);   //So that the culture claim is renewed
                }
                MemCheckRequestCultureProvider.AddCultureCookie(HttpContext.Features, Response, culture, false);
            }

            var requestCulture = HttpContext.Features.Get<IRequestCultureFeature>();
            return Ok(requestCulture != null && requestCulture.RequestCulture.UICulture.Name != culture);
        }
    }
}
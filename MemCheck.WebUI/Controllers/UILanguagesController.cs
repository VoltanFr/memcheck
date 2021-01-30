using MemCheck.Application;
using MemCheck.Application.Languages;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
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
            var requestCulture = HttpContext.Features.Get<IRequestCultureFeature>();
            if (requestCulture.RequestCulture.UICulture.Name == culture)
                return Ok(false);

            var user = await userManager.GetUserAsync(HttpContext.User);
            if (user != null)
            {
                new SetUserUILanguage(dbContext, GetSupportedUILanguages()).Run(user, culture);
                await signInManager.RefreshSignInAsync(user);   //So that the culture claim is renewed
            }

            return Ok(true);
        }
    }
}

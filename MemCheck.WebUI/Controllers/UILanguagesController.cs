using MemCheck.Application;
using MemCheck.Application.Languages;
using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.WebUI.Controllers
{
    [Route("[controller]")]
    public class UILanguagesController : MemCheckController
    {
        #region Fields
        private readonly CallContext callContext;
        private readonly UserManager<MemCheckUser> userManager;
        private readonly SignInManager<MemCheckUser> signInManager;
        private readonly ILogger<UILanguagesController> logger;
        #endregion
        public UILanguagesController(MemCheckDbContext dbContext, UserManager<MemCheckUser> userManager, SignInManager<MemCheckUser> signInManager, TelemetryClient telemetryClient, IStringLocalizer<TagsController> localizer, ILogger<UILanguagesController> logger) : base(localizer)
        {
            callContext = new CallContext(dbContext, new MemCheckTelemetryClient(telemetryClient), this, new ProdRoleChecker(userManager));
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.logger = logger;
        }
        [HttpGet("GetAvailableLanguages")]
        public IActionResult GetAvailableLanguages()
        {
            var result = MemCheckSupportedCultures.All.Select(c => MemCheckSupportedCultures.IdFromCulture(c));
            logger.LogInformation($"UILanguagesController.GetAvailableLanguages returning '{string.Concat(result)}'");
            return base.Ok(result);
        }
        [HttpGet("GetActiveLanguage")]
        public IActionResult GetActiveLanguage()
        {
            var requestCulture = HttpContext.Features.Get<IRequestCultureFeature>()!;
            var result = MemCheckSupportedCultures.IdFromCulture(requestCulture.RequestCulture.Culture);
            return Ok(result);
        }
        [HttpPost("SetCulture/{cultureId}")]
        public async Task<IActionResult> SetCultureAsync(string cultureId)
        {
            var newCulture = MemCheckSupportedCultures.CultureFromId(cultureId);

            if (newCulture == null)
            {
                logger.LogError($"UILanguagesController.SetCulture({cultureId}) returning BadRequest");
                return BadRequest();
            }

            var user = await userManager.GetUserAsync(HttpContext.User);
            if (user != null)
            {
                await new SetUserUILanguage(callContext).RunAsync(new SetUserUILanguage.Request(user.Id, newCulture));
                await signInManager.RefreshSignInAsync(user);   //So that the culture claim is renewed
            }

            return Ok();
        }
    }
}
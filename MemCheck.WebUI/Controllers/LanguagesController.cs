using MemCheck.Application;
using MemCheck.Application.Languages;
using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Threading.Tasks;

namespace MemCheck.WebUI.Controllers
{
    [Route("[controller]"), Authorize(Roles = IRoleChecker.AdminRoleName)]
    public class LanguagesController : MemCheckController
    {
        #region Fields
        private readonly CallContext callContext;
        private readonly UserManager<MemCheckUser> userManager;
        #endregion
        public LanguagesController(MemCheckDbContext dbContext, IStringLocalizer<LanguagesController> localizer, UserManager<MemCheckUser> userManager, TelemetryClient telemetryClient) : base(localizer)
        {
            callContext = new CallContext(dbContext, new MemCheckTelemetryClient(telemetryClient), this);
            this.userManager = userManager;
        }
        [HttpGet("GetAllLanguages")] public async Task<IActionResult> GetAllLanguagesControllerAsync() => Ok(await new GetAllLanguages(callContext).RunAsync());
        #region Create
        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] CreateRequest language)
        {
            CheckBodyParameter(language);
            var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
            return Ok(await new CreateLanguage(callContext, new ProdRoleChecker(userManager)).RunAsync(new CreateLanguage.Request(userId, language.Name), this));
        }
        public sealed class CreateRequest
        {
            public string Name { get; set; } = null!;
        }
        #endregion
    }
}

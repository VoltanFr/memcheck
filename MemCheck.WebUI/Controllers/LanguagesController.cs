using MemCheck.Application.Languages;
using MemCheck.Database;
using MemCheck.Domain;
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
        private readonly MemCheckDbContext dbContext;
        private readonly UserManager<MemCheckUser> userManager;
        #endregion
        public LanguagesController(MemCheckDbContext dbContext, IStringLocalizer<LanguagesController> localizer, UserManager<MemCheckUser> userManager) : base(localizer)
        {
            this.dbContext = dbContext;
            this.userManager = userManager;
        }
        [HttpGet("GetAllLanguages")] public async Task<IActionResult> GetAllLanguagesControllerAsync() => Ok(await new GetAllLanguages(dbContext).RunAsync());
        #region Create
        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] CreateRequest language)
        {
            CheckBodyParameter(language);
            var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
            return Ok(await new CreateLanguage(dbContext, new ProdRoleChecker(userManager)).RunAsync(new CreateLanguage.Request(userId, language.Name), this));
        }
        public sealed class CreateRequest
        {
            public string Name { get; set; } = null!;
        }
        #endregion
    }
}

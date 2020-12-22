using MemCheck.Application;
using MemCheck.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Threading.Tasks;

namespace MemCheck.WebUI.Controllers
{
    [Route("[controller]"), Authorize(Roles = "Admin")]
    public class LanguagesController : MemCheckController
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public LanguagesController(MemCheckDbContext dbContext, IStringLocalizer<LanguagesController> localizer) : base(localizer)
        {
            this.dbContext = dbContext;
        }
        [HttpGet("GetAllLanguages")] public IActionResult GetAllLanguagesController() => Ok(new GetAllLanguages(dbContext).Run());
        [HttpPost("Create")] public async Task<IActionResult> Create([FromBody] CreateLanguage.Request language) => Ok(await new CreateLanguage(dbContext).RunAsync(language));
    }
}

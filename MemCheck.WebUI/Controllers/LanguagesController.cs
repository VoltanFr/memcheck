using MemCheck.Application;
using MemCheck.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MemCheck.WebUI.Controllers
{
    [Route("[controller]")]
    public class LanguagesController : Controller
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public LanguagesController(MemCheckDbContext dbContext) : base()
        {
            this.dbContext = dbContext;
        }
        [HttpGet("GetAllLanguages")] public IActionResult GetAllLanguagesController() => Ok(new GetAllLanguages(dbContext).Run());
        [HttpPost("Create")] public async Task<IActionResult> Create([FromBody] CreateLanguage.Request language) => Ok(await new CreateLanguage(dbContext).RunAsync(language));
    }
}

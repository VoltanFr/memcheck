using MemCheck.Application;
using MemCheck.Database;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.WebUI.Controllers
{
    [Route("[controller]")]
    public class AdminController : Controller
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public AdminController(MemCheckDbContext dbContext) : base()
        {
            this.dbContext = dbContext;
        }
        [HttpGet("cards")] public IActionResult GetCards() => Ok(new GetAllCardsInDb(dbContext).Run());
    }
}

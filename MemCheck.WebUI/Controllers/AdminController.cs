using MemCheck.Application;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemCheck.WebUI.Controllers
{
    [Route("[controller]"), Authorize(Roles = "Admin")]
    public class AdminController : Controller, ILocalized
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        private readonly IStringLocalizer<TagsController> localizer;
        private readonly IEmailSender emailSender;
        private readonly UserManager<MemCheckUser> userManager;
        #endregion
        public AdminController(MemCheckDbContext dbContext, UserManager<MemCheckUser> userManager, IStringLocalizer<TagsController> localizer, IEmailSender emailSender) : base()
        {
            this.dbContext = dbContext;
            this.localizer = localizer;
            this.emailSender = emailSender;
            this.userManager = userManager;
        }
        public IStringLocalizer Localizer => localizer;
        #region GetUsers
        [HttpPost("GetUsers")]
        public async Task<IActionResult> GetUsers([FromBody] GetUsersRequest request)
        {
            if (request.Filter == null)
                return BadRequest(localizer["FilterSet"].Value);

            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                if (user == null)
                    return BadRequest(localizer["NeedLogin"].Value);
                var appRequest = new GetAllUsers.Request(user, request.PageSize, request.PageNo, request.Filter);
                var result = await new GetAllUsers(dbContext, userManager).RunAsync(appRequest);
                return Ok(new GetUsersViewModel(result));
            }
            catch (Exception e)
            {
                return ControllerError.BadRequest(e, this);
            }
        }
        #region Request and result classes
        public sealed class GetUsersRequest
        {
            public int PageSize { get; set; }
            public int PageNo { get; set; }
            public string Filter { get; set; } = null!;
        }
        public sealed class GetUsersViewModel
        {
            public GetUsersViewModel(GetAllUsers.ResultModel applicationResult)
            {
                TotalCount = applicationResult.TotalCount;
                PageCount = applicationResult.PageCount;
                Users = applicationResult.Users.Select(user => new GetUsersUserViewModel(user));
            }
            public int TotalCount { get; }
            public int PageCount { get; }
            public IEnumerable<GetUsersUserViewModel> Users { get; }
        }
        public sealed class GetUsersUserViewModel
        {
            public GetUsersUserViewModel(GetAllUsers.ResultUserModel user)
            {
                UserName = user.UserName;
                Roles = user.Roles;
                Email = user.Email;
            }
            public string UserName { get; }
            public string Roles { get; }
            public string Email { get; }
        }
        #endregion
        #endregion
        #region LaunchNotifier
        [HttpPost("LaunchNotifier")]
        public async Task<IActionResult> LaunchNotifier()
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            try
            {
                await emailSender.SendEmailAsync(user.Email, "Notifier starting", $"At {DateTime.Now}");

                var chrono = Stopwatch.StartNew();
                var notifier = new Notifier(dbContext);
                var notifierResult = await notifier.GetNotificationsAsync(user.Id);

                var mailBody = new StringBuilder();
                mailBody.Append("<h1>Summary</h1>");
                mailBody.Append($"<p>{notifierResult.RegisteredCardCount} registered cards</p>");
                mailBody.Append($"<p>Finished at {DateTime.Now}</p>");
                mailBody.Append($"</p>Took {chrono.Elapsed}</p>");

                mailBody.Append("<h1>Cards</h1>");
                mailBody.Append("<ul>");
                foreach (var card in notifierResult.CardVersions)
                    mailBody.Append($"<li>https://memcheckfr.azurewebsites.net/Authoring?CardId={card.CardId}</li>");
                mailBody.Append("/<ul>");

                await emailSender.SendEmailAsync(user.Email, "Notifier ended on success", mailBody.ToString());
                return Ok();
            }
            catch (Exception e)
            {
                await emailSender.SendEmailAsync(user.Email, "Notifier ended on exception", $"<h1>{e.GetType().Name}</h1><p>{e.Message}</p>");
                return ControllerError.BadRequest(e, this);
            }
        }
        #endregion
    }
}

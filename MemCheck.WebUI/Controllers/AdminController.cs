using MemCheck.Application;
using MemCheck.Application.Notifying;
using MemCheck.Application.QueryValidation;
using MemCheck.Application.Users;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemCheck.WebUI.Controllers
{
    [Route("[controller]"), Authorize(Roles = IRoleChecker.AdminRoleName)]
    public class AdminController : MemCheckController
    {
        #region Private classes MemCheckMailSender & MemCheckLinkGenerator
        private sealed class MemCheckMailSender : IMemCheckMailSender
        {
            #region Fields
            private readonly IEmailSender emailSender;
            #endregion
            public MemCheckMailSender(IEmailSender emailSender)
            {
                this.emailSender = emailSender;
            }
            public async Task SendAsync(string to, string subject, string body)
            {
                await emailSender.SendEmailAsync(to, subject, body);
            }
        }
        private sealed class MemCheckLinkGenerator : IMemCheckLinkGenerator
        {
            #region Fields
            private readonly LinkGenerator linkGenerator;
            private readonly HttpContext httpContext;
            #endregion
            public MemCheckLinkGenerator(LinkGenerator linkGenerator, HttpContext httpContext)
            {
                this.linkGenerator = linkGenerator;
                this.httpContext = httpContext;
            }
            public string GetAbsoluteUri(string relativeUri)
            {
                return linkGenerator.GetUriByPage(httpContext, page: relativeUri)!;
            }
        }
        #endregion
        #region Fields
        private readonly CallContext callContext;
        private readonly IEmailSender emailSender;
        private readonly LinkGenerator linkGenerator;
        private readonly UserManager<MemCheckUser> userManager;
        #endregion
        #region Private methods
        private string GetAuthoringPageLink()
        {
            return linkGenerator.GetUriByPage(HttpContext, page: "/Authoring/Index")!;
        }
        private string GetComparePageLink()
        {
            return linkGenerator.GetUriByPage(HttpContext, page: "/Authoring/Compare")!;
        }
        private string GetHistoryPageLink()
        {
            return linkGenerator.GetUriByPage(HttpContext, page: "/Authoring/History")!;
        }
        #endregion
        public AdminController(MemCheckDbContext dbContext, UserManager<MemCheckUser> userManager, IStringLocalizer<AdminController> localizer, IEmailSender emailSender, LinkGenerator linkGenerator, TelemetryClient telemetryClient) : base(localizer)
        {
            callContext = new CallContext(dbContext, new MemCheckTelemetryClient(telemetryClient), this, new ProdRoleChecker(userManager));
            this.emailSender = emailSender;
            this.linkGenerator = linkGenerator;
            this.userManager = userManager;
        }
        #region GetUsers
        [HttpPost("GetUsers")]
        public async Task<IActionResult> GetUsers([FromBody] GetUsersRequest request)
        {
            CheckBodyParameter(request);
            var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
            var appRequest = new GetAllUsers.Request(userId, request.PageSize, request.PageNo, request.Filter);
            var result = await new GetAllUsers(callContext).RunAsync(appRequest);
            return Ok(new GetUsersViewModel(result));
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
                UserId = user.UserId.ToString();
                Roles = user.Roles;
                Email = user.Email;
                NotifInterval = user.NotifInterval;
                LastNotifUtcDate = user.LastNotifUtcDate;
                LastSeenUtcDate = user.LastSeenUtcDate;
            }
            public string UserName { get; }
            public string UserId { get; }
            public string Roles { get; }
            public string Email { get; }
            public int NotifInterval { get; }
            public DateTime LastNotifUtcDate { get; }
            public DateTime LastSeenUtcDate { get; }
        }
        #endregion
        #endregion
        #region LaunchNotifier
        [HttpPost("LaunchNotifier")]
        public async Task<IActionResult> LaunchNotifier()
        {
            var launchingUser = await userManager.GetUserAsync(HttpContext.User);
            try
            {
                var mailer = new NotificationMailer(callContext, launchingUser.Email, new MemCheckMailSender(emailSender), new MemCheckLinkGenerator(linkGenerator, HttpContext));
                await mailer.RunAsync();
                return ControllerResultWithToast.Success("Notifications sent", this);
            }
            catch (Exception e)
            {
                await emailSender.SendEmailAsync(launchingUser.Email, "Notifier ended on exception", $"<h1>{e.GetType().Name}</h1><p>{e.Message}</p><p><pre>{e.StackTrace}</pre></p>");
                throw;
            }
        }
        #endregion
    }
}

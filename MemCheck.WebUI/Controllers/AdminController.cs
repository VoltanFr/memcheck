using MemCheck.Application;
using MemCheck.Application.QueryValidation;
using MemCheck.Application.Users;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.WebUI.Controllers;

[Route("[controller]"), Authorize(Roles = IRoleChecker.AdminRoleName)]
public class AdminController : MemCheckController
{
    #region Private classes MemCheckMailSender & MemCheckLinkGenerator
    //private sealed class MemCheckMailSender : IMemCheckMailSender
    //{
    //    #region Fields
    //    private readonly IEmailSender emailSender;
    //    #endregion
    //    public MemCheckMailSender(IEmailSender emailSender)
    //    {
    //        this.emailSender = emailSender;
    //    }
    //    public async Task SendAsync(string to, string subject, string body)
    //    {
    //        await emailSender.SendEmailAsync(to, subject, body);
    //    }
    //}
    //private sealed class MemCheckLinkGenerator : IMemCheckLinkGenerator
    //{
    //    #region Fields
    //    private readonly LinkGenerator linkGenerator;
    //    private readonly HttpContext httpContext;
    //    #endregion
    //    public MemCheckLinkGenerator(LinkGenerator linkGenerator, HttpContext httpContext)
    //    {
    //        this.linkGenerator = linkGenerator;
    //        this.httpContext = httpContext;
    //    }
    //    public string GetAbsoluteAddress(string relativeUri)
    //    {
    //        return linkGenerator.GetUriByPage(httpContext, page: relativeUri)!;
    //    }
    //}
    #endregion
    #region Fields
    private readonly CallContext callContext;
    private readonly IAzureEmailSender azureEmailSender;
#pragma warning disable IDE0052 // Remove unread private members
    private readonly LinkGenerator linkGenerator;
#pragma warning restore IDE0052 // Remove unread private members
    private readonly MemCheckUserManager userManager;
    #endregion
    public AdminController(MemCheckDbContext dbContext, MemCheckUserManager userManager, IStringLocalizer<AdminController> localizer, IAzureEmailSender azureEmailSender, LinkGenerator linkGenerator, TelemetryClient telemetryClient) : base(localizer)
    {
        callContext = new CallContext(dbContext, new MemCheckTelemetryClient(telemetryClient), this, new ProdRoleChecker(userManager));
        this.azureEmailSender = azureEmailSender;
        this.linkGenerator = linkGenerator;
        this.userManager = userManager;
    }
    #region GetUsers
    [HttpPost("GetUsers")]
    public async Task<IActionResult> GetUsers([FromBody] GetUsersRequest request)
    {
        CheckBodyParameter(request);
        var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
        var appRequest = new GetAllUsersStats.Request(userId, request.PageSize, request.PageNo, request.Filter);
        var result = await new GetAllUsersStats(callContext).RunAsync(appRequest);
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
        public GetUsersViewModel(GetAllUsersStats.ResultModel applicationResult)
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
        public GetUsersUserViewModel(GetAllUsersStats.ResultUserModel user)
        {
            UserName = user.UserName;
            UserId = user.UserId.ToString();
            Roles = user.Roles;
            Email = user.Email;
            NotifInterval = user.NotifInterval;
            LastNotifUtcDate = user.LastNotifUtcDate;
            LastSeenUtcDate = user.LastSeenUtcDate;
            RegistrationUtcDate = user.RegistrationUtcDate;
        }
        public string UserName { get; }
        public string UserId { get; }
        public string Roles { get; }
        public string Email { get; }
        public int NotifInterval { get; }
        public DateTime LastNotifUtcDate { get; }
        public DateTime LastSeenUtcDate { get; }
        public DateTime RegistrationUtcDate { get; }
    }
    #endregion
    #endregion
    #region LaunchNotifier
    [HttpPost("LaunchNotifier")]
    public async Task<IActionResult> LaunchNotifier()
    {
        var launchingUser = await userManager.GetExistingUserAsync(HttpContext.User);
        azureEmailSender.SendEmail(launchingUser.GetEmail(), "Notifier started", $"<h1>Notifier started by {launchingUser.UserName}</h1><p>Notifications will be sent to all users.</p>");
        return ControllerResultWithToast.Success("Notifications hijacked", this);


        //try
        //{
        //    var mailer = new NotificationMailer(callContext, azureEmailSender, new MemCheckLinkGenerator(linkGenerator, HttpContext));
        //    await mailer.RunAndCreateReportMailMainPartAsync();
        //    return ControllerResultWithToast.Success("Notifications sent", this);
        //}
        //catch (Exception e)
        //{
        //    await emailSender.SendEmailAsync(launchingUser.GetEmail(), "Notifier ended on exception", $"<h1>{e.GetType().Name}</h1><p>{e.Message}</p><p><pre>{e.StackTrace}</pre></p>");
        //    throw;
        //}
    }
    #endregion
}

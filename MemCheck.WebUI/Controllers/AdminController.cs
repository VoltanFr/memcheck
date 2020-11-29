using MemCheck.Application;
using MemCheck.Application.Notifying;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
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
        private readonly string authoringPageLink;
        #endregion
        public AdminController(MemCheckDbContext dbContext, UserManager<MemCheckUser> userManager, IStringLocalizer<TagsController> localizer, IEmailSender emailSender, IHttpContextAccessor contextAccessor, LinkGenerator linkGenerator) : base()
        {
            this.dbContext = dbContext;
            this.localizer = localizer;
            this.emailSender = emailSender;
            this.userManager = userManager;
            authoringPageLink = linkGenerator.GetUriByPage(contextAccessor.HttpContext, page: "/Authoring/Index");
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
                NotifInterval = user.NotifInterval;
                LastNotifUtcDate = user.LastNotifUtcDate;
            }
            public string UserName { get; }
            public string Roles { get; }
            public string Email { get; }
            public int NotifInterval { get; }
            public DateTime LastNotifUtcDate { get; }
        }
        #endregion
        #endregion
        #region LaunchNotifier
        private string GetMailBodyForUser(Notifier.UserNotifications userNotifications)
        {
            var mailBody = new StringBuilder();
            mailBody.Append("<html>");
            mailBody.Append("<body>");
            mailBody.Append($"<p>Hello {userNotifications.UserName}</p>");
            mailBody.Append("<h1>Summary</h1>");
            mailBody.Append("<p>");
            mailBody.Append($"{userNotifications.SubscribedCardCount} registered cards<br/>");
            mailBody.Append($"Search finished at {DateTime.UtcNow}<br/>");
            mailBody.Append("</p>");

            if (!userNotifications.CardVersions.Any())
                mailBody.Append("<h1>No card with new version</h1>");
            else
            {
                mailBody.Append($"<h1>{userNotifications.CardVersions.Length} Cards with new versions</h1>");
                mailBody.Append("<ul>");
                foreach (var card in userNotifications.CardVersions.OrderBy(cardVersion => cardVersion.VersionUtcDate))
                {
                    mailBody.Append("<li>");
                    mailBody.Append($"<a href={authoringPageLink}?CardId={card.CardId}>{card.FrontSide}</a><br/>");
                    mailBody.Append($"By {card.VersionCreator}<br/>");
                    mailBody.Append($"On {card.VersionUtcDate} (UTC)<br/>");
                    mailBody.Append($"Version description: '{card.VersionDescription}'");
                    mailBody.Append("</li>");
                }
                mailBody.Append("</ul>");
            }

            if (!userNotifications.DeletedCards.Any())
                mailBody.Append("<h1>No deleted card</h1>");
            else
            {
                mailBody.Append($"<h1>{userNotifications.DeletedCards.Length} Deleted cards</h1>");
                mailBody.Append("<ul>");
                foreach (var card in userNotifications.DeletedCards.OrderBy(card => card.DeletionUtcDate))
                {
                    mailBody.Append("<li>");
                    mailBody.Append($"{card.FrontSide}<br/>");
                    mailBody.Append($"By {card.DeletionAuthor}<br/>");
                    mailBody.Append($"On {card.DeletionUtcDate} (UTC)<br/>");
                    mailBody.Append($"Deletion description: '{card.DeletionDescription}'");
                    mailBody.Append("</li>");
                }
                mailBody.Append("</ul>");
            }

            mailBody.Append("</body>");
            mailBody.Append("</html>");

            return mailBody.ToString();
        }
        [HttpPost("LaunchNotifier")]
        public async Task<IActionResult> LaunchNotifier()
        {
            var launchingUser = await userManager.GetUserAsync(HttpContext.User);
            try
            {
                var mailSendingsToWaitFor = new List<Task>();
                var chrono = Stopwatch.StartNew();
                var notifierResult = await new Notifier(dbContext).GetNotificationsAndUpdateLastNotifDatesAsync();
                var sentEmailCount = 0;

                foreach (var userNotifications in notifierResult.UserNotifications)
                    if (userNotifications.CardVersions.Any() || userNotifications.DeletedCards.Any())
                    {
                        var mailBody = GetMailBodyForUser(userNotifications);
                        mailSendingsToWaitFor.Add(emailSender.SendEmailAsync(userNotifications.UserEmail, "MemCheck notifications", mailBody));
                        sentEmailCount++;
                    }


                var adminMailBody = $"<html><body><p>Sent {sentEmailCount} emails.</p><p>Finished at {DateTime.Now}<br/>Notifier execution took {chrono.Elapsed}</p></body></html>";
                mailSendingsToWaitFor.Add(emailSender.SendEmailAsync(launchingUser.Email, "Notifier ended on success", adminMailBody));
                Task.WaitAll(mailSendingsToWaitFor.ToArray());
                return Ok();
            }
            catch (Exception e)
            {
                await emailSender.SendEmailAsync(launchingUser.Email, "Notifier ended on exception", $"<h1>{e.GetType().Name}</h1><p>{e.Message}</p>");
                return ControllerError.BadRequest(e, this);
            }
        }
        #endregion
    }
}

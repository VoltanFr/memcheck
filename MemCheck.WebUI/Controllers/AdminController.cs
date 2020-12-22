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
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemCheck.WebUI.Controllers
{
    [Route("[controller]"), Authorize(Roles = "Admin")]
    public class AdminController : MemCheckController
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        private readonly IEmailSender emailSender;
        private readonly UserManager<MemCheckUser> userManager;
        private readonly string authoringPageLink;
        #endregion
        public AdminController(MemCheckDbContext dbContext, UserManager<MemCheckUser> userManager, IStringLocalizer<TagsController> localizer, IEmailSender emailSender, IHttpContextAccessor contextAccessor, LinkGenerator linkGenerator) : base(localizer)
        {
            this.dbContext = dbContext;
            this.emailSender = emailSender;
            this.userManager = userManager;
            authoringPageLink = linkGenerator.GetUriByPage(contextAccessor.HttpContext, page: "/Authoring/Index");
        }
        #region GetUsers
        [HttpPost("GetUsers")]
        public async Task<IActionResult> GetUsers([FromBody] GetUsersRequest request)
        {
            CheckBodyParameter(request);
            var user = await userManager.GetUserAsync(HttpContext.User);
            var appRequest = new GetAllUsers.Request(user, request.PageSize, request.PageNo, request.Filter);
            var result = await new GetAllUsers(dbContext, userManager).RunAsync(appRequest);
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
        private void AddCardVersions(ImmutableArray<CardVersion> cardVersions, StringBuilder mailBody)
        {
            if (!cardVersions.Any())
                mailBody.Append("<h1>No card with new version</h1>");
            else
            {
                mailBody.Append($"<h1>{cardVersions.Length} Cards with new versions</h1>");
                mailBody.Append("<ul>");
                foreach (var card in cardVersions.OrderBy(cardVersion => cardVersion.VersionUtcDate))
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
        }
        private void AddCardDeletions(ImmutableArray<CardDeletion> deletedCards, StringBuilder mailBody)
        {
            if (!deletedCards.Any())
                mailBody.Append("<h1>No deleted card</h1>");
            else
            {
                mailBody.Append($"<h1>{deletedCards.Length} Deleted cards</h1>");
                mailBody.Append("<ul>");
                foreach (var card in deletedCards.OrderBy(card => card.DeletionUtcDate))
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
        }
        private void AddSearchNotifications(UserSearchNotifierResult searchNotifications, StringBuilder mailBody)
        {
            mailBody.Append($"<h1>{searchNotifications.SubscriptionName} search</h1>");

            if (searchNotifications.TotalNewlyFoundCardCount == 0)
                mailBody.Append("<h2>No newly found card</h2>");
            else
            {
                mailBody.Append($"<h2>{searchNotifications.TotalNewlyFoundCardCount} newly found cards</h2>");
                if (searchNotifications.TotalNewlyFoundCardCount > searchNotifications.NewlyFoundCards.Length)
                    mailBody.Append($"<p>Showing {searchNotifications.NewlyFoundCards.Length} first cards.</p>");
                mailBody.Append("<ul>");
                foreach (var card in searchNotifications.NewlyFoundCards.OrderBy(card => card.VersionUtcDate))
                {
                    mailBody.Append("<li>");
                    mailBody.Append($"<a href={authoringPageLink}?CardId={card.CardId}>{card.FrontSide}</a><br/>");
                    mailBody.Append($"Changed by '{card.VersionCreator}'<br/>");
                    mailBody.Append($"On {card.VersionUtcDate} (UTC)<br/>");
                    mailBody.Append($"Version description: '{card.VersionDescription}'");
                    mailBody.Append("</li>");
                }
                mailBody.Append("</ul>");
            }

            if (searchNotifications.CountOfCardsNotFoundAnymore_StillExists_UserAllowedToView > 0
                || searchNotifications.CountOfCardsNotFoundAnymore_Deleted_UserAllowedToView > 0
                || searchNotifications.CountOfCardsNotFoundAnymore_StillExists_UserNotAllowedToView > 0
                || searchNotifications.CountOfCardsNotFoundAnymore_Deleted_UserNotAllowedToView > 0
                )
            {
                mailBody.Append("<h2>Cards no more reported by this search</h2>");

                if (searchNotifications.CountOfCardsNotFoundAnymore_StillExists_UserAllowedToView > 0)
                {
                    mailBody.Append("<ul>");
                    foreach (var card in searchNotifications.CardsNotFoundAnymore_StillExists_UserAllowedToView.OrderBy(card => card.VersionUtcDate))
                    {
                        mailBody.Append("<li>");
                        mailBody.Append($"<a href={authoringPageLink}?CardId={card.CardId}>{card.FrontSide}</a><br/>");
                        mailBody.Append($"Changed by user '{card.VersionCreator}' and not reported anymore by this search<br/>");
                        mailBody.Append($"On {card.VersionUtcDate} (UTC)<br/>");
                        mailBody.Append($"Version description: '{card.VersionDescription}'");
                        mailBody.Append("</li>");
                    }
                    if (searchNotifications.CountOfCardsNotFoundAnymore_StillExists_UserAllowedToView > searchNotifications.CardsNotFoundAnymore_StillExists_UserAllowedToView.Length)
                    {
                        mailBody.Append("<li>");
                        mailBody.Append($"And {searchNotifications.CountOfCardsNotFoundAnymore_StillExists_UserAllowedToView - searchNotifications.CardsNotFoundAnymore_StillExists_UserAllowedToView.Length} more");
                        mailBody.Append("</li>");
                    }
                    mailBody.Append("</ul>");
                }

                if (searchNotifications.CountOfCardsNotFoundAnymore_Deleted_UserAllowedToView > 0)
                {
                    mailBody.Append("<ul>");
                    foreach (var card in searchNotifications.CardsNotFoundAnymore_Deleted_UserAllowedToView)
                    {
                        mailBody.Append("<li>");
                        mailBody.Append($"{card.FrontSide}<br/>");
                        mailBody.Append($"Deleted by user '{card.DeletionAuthor}'<br/>");
                        mailBody.Append($"On {card.DeletionUtcDate} (UTC)<br/>");
                        mailBody.Append($"With description: '{card.DeletionDescription}'");
                        mailBody.Append("</li>");
                    }
                    if (searchNotifications.CountOfCardsNotFoundAnymore_Deleted_UserAllowedToView > searchNotifications.CardsNotFoundAnymore_Deleted_UserAllowedToView.Length)
                    {
                        mailBody.Append("<li>");
                        mailBody.Append($"And {searchNotifications.CountOfCardsNotFoundAnymore_Deleted_UserAllowedToView - searchNotifications.CardsNotFoundAnymore_Deleted_UserAllowedToView.Length} more");
                        mailBody.Append("</li>");
                    }
                    mailBody.Append("</ul>");
                }

                if (searchNotifications.CountOfCardsNotFoundAnymore_StillExists_UserNotAllowedToView > 0)
                {
                    mailBody.Append("<ul>");
                    mailBody.Append("<li>");
                    mailBody.Append($"{searchNotifications.CountOfCardsNotFoundAnymore_StillExists_UserNotAllowedToView} cards have been modified and made private, preventing you from seeing them");
                    mailBody.Append("</li>");
                    mailBody.Append("</ul>");
                }

                if (searchNotifications.CountOfCardsNotFoundAnymore_Deleted_UserNotAllowedToView > 0)
                {
                    mailBody.Append("<ul>");
                    mailBody.Append("<li>");
                    mailBody.Append($"{searchNotifications.CountOfCardsNotFoundAnymore_Deleted_UserNotAllowedToView} cards have been made private and deleted, can not show any detail");
                    mailBody.Append("</li>");
                    mailBody.Append("</ul>");
                }
            }
            else
                mailBody.Append("<h2>No card is not reported by this search anymore</h2>");
        }
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

            AddCardVersions(userNotifications.CardVersions, mailBody);
            AddCardDeletions(userNotifications.DeletedCards, mailBody);
            foreach (var searchNotifications in userNotifications.SearchNotificactions)
                AddSearchNotifications(searchNotifications, mailBody);

            mailBody.Append("</body>");
            mailBody.Append("</html>");

            return mailBody.ToString();
        }
        private string GetAdminMailBody(int sentEmailCount, List<string> performanceIndicators)
        {
            var mailBody = new StringBuilder();
            mailBody.Append("<html>");
            mailBody.Append("<body>");


            mailBody.Append($"<p>Sent {sentEmailCount} emails.</p>");
            mailBody.Append($"<p>Perf indicators...</p>");
            mailBody.Append("<ul>");
            foreach (var performanceIndicator in performanceIndicators)
                mailBody.Append($"<li>{performanceIndicator}</li>");
            mailBody.Append("</ul>");
            mailBody.Append($"<p>Finished at {DateTime.UtcNow} (UTC)</p>");

            mailBody.Append("</body>");
            mailBody.Append("</html>");
            return mailBody.ToString();
        }
        private bool MustSendForNotifications(Notifier.UserNotifications userNotifications)
        {
            if (userNotifications.CardVersions.Any())
                return true;
            if (userNotifications.DeletedCards.Any())
                return true;
            return userNotifications.SearchNotificactions.Any(searchNotificaction =>
                searchNotificaction.TotalNewlyFoundCardCount > 0
                || searchNotificaction.CountOfCardsNotFoundAnymore_StillExists_UserAllowedToView > 0
                || searchNotificaction.CountOfCardsNotFoundAnymore_Deleted_UserAllowedToView > 0
                || searchNotificaction.CountOfCardsNotFoundAnymore_StillExists_UserNotAllowedToView > 0
                || searchNotificaction.CountOfCardsNotFoundAnymore_Deleted_UserNotAllowedToView > 0
                );
        }
        [HttpPost("LaunchNotifier")]
        public async Task<IActionResult> LaunchNotifier()
        {
            var launchingUser = await userManager.GetUserAsync(HttpContext.User);
            try
            {
                var mailSendingsToWaitFor = new List<Task>();
                var performanceIndicators = new List<string>();
                var notifierResult = await new Notifier(dbContext, performanceIndicators).GetNotificationsAndUpdateLastNotifDatesAsync();
                var sentEmailCount = 0;

                foreach (var userNotifications in notifierResult.UserNotifications)
                    if (MustSendForNotifications(userNotifications))
                    {
                        var mailBody = GetMailBodyForUser(userNotifications);
                        mailSendingsToWaitFor.Add(emailSender.SendEmailAsync(userNotifications.UserEmail, "MemCheck notifications", mailBody));
                        sentEmailCount++;
                    }


                mailSendingsToWaitFor.Add(emailSender.SendEmailAsync(launchingUser.Email, "Notifier ended on success", GetAdminMailBody(sentEmailCount, performanceIndicators)));
                Task.WaitAll(mailSendingsToWaitFor.ToArray());
                return Ok();
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

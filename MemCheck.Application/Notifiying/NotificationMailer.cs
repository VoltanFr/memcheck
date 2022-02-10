using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using Microsoft.ApplicationInsights;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemCheck.Application.Notifying
{
    public interface IMemCheckLinkGenerator
    {
        string GetAbsoluteUri(string relativeUri);  //For example GetAbsoluteUri("/Learn/Index") returns "https://memcheckfr.azurewebsites.net/Learn/Index"
    }
    public interface IMemCheckMailSender
    {
        Task SendAsync(string to, string subject, string body);
    }
    public sealed class NotificationMailer
    {
        #region Fields
        private readonly CallContext callContext;
        private readonly string senderEmailAddress;
        private readonly IMemCheckMailSender emailSender;
        private readonly IMemCheckLinkGenerator linkGenerator;
        #endregion
        #region Private methods
        private string GetAuthoringPageLink()
        {
            return linkGenerator.GetAbsoluteUri("/Authoring/Index")!;
        }
        private string GetComparePageLink()
        {
            return linkGenerator.GetAbsoluteUri("/Authoring/Compare")!;
        }
        private string GetHistoryPageLink()
        {
            return linkGenerator.GetAbsoluteUri("/Authoring/History")!;
        }
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
                    mailBody.Append($"<a href={GetAuthoringPageLink()}?CardId={card.CardId}>{card.FrontSide}</a><br/>");
                    mailBody.Append($"By {card.VersionCreator}<br/>");
                    mailBody.Append($"On {card.VersionUtcDate} (UTC)<br/>");
                    mailBody.Append($"Version description: '{card.VersionDescription}'<br/>");
                    mailBody.Append($"<a href={GetHistoryPageLink()}?CardId={card.CardId}>History</a> - ");
                    if (card.VersionIdOnLastNotification != null)
                        mailBody.Append($"<a href={GetComparePageLink()}?CardId={card.CardId}&VersionId={card.VersionIdOnLastNotification}>View changes since your last notification</a>");
                    else
                        mailBody.Append("Did not exist on your previous notifications");
                    mailBody.Append("</li>");
                }
                mailBody.Append("</ul>");
            }
        }
        private static void AddCardDeletions(ImmutableArray<CardDeletion> deletedCards, StringBuilder mailBody)
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
                    mailBody.Append($"<a href={GetAuthoringPageLink()}?CardId={card.CardId}>{card.FrontSide}</a><br/>");
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
                        mailBody.Append($"<a href={GetAuthoringPageLink()}?CardId={card.CardId}>{card.FrontSide}</a><br/>");
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
        private static string GetAdminMailBody(int sentEmailCount, List<string> performanceIndicators)
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
        private static bool MustSendForNotifications(Notifier.UserNotifications userNotifications)
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
        #endregion
        public NotificationMailer(CallContext callContext, string senderEmailAddress, IMemCheckMailSender emailSender, IMemCheckLinkGenerator linkGenerator)
        {
            this.callContext = callContext;
            this.senderEmailAddress = senderEmailAddress;
            this.emailSender = emailSender;
            this.linkGenerator = linkGenerator;
        }
        public async Task RunAsync()
        {
            var mailSendingsToWaitFor = new List<Task>();
            var performanceIndicators = new List<string>();
            var notifierResult = await new Notifier(callContext, performanceIndicators).RunAsync(new Notifier.Request());
            var sentEmailCount = 0;

            foreach (var userNotifications in notifierResult.UserNotifications)
                if (MustSendForNotifications(userNotifications))
                {
                    var mailBody = GetMailBodyForUser(userNotifications);
                    mailSendingsToWaitFor.Add(emailSender.SendAsync(userNotifications.UserEmail, "MemCheck notifications", mailBody));
                    sentEmailCount++;
                }

            mailSendingsToWaitFor.Add(emailSender.SendAsync(senderEmailAddress, "Notifier ended on success", GetAdminMailBody(sentEmailCount, performanceIndicators)));
            Task.WaitAll(mailSendingsToWaitFor.ToArray());
        }
    }
}
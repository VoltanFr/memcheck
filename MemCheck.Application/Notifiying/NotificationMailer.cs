using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemCheck.Application.Notifying
{
    public interface IMemCheckLinkGenerator
    {
        string GetAbsoluteAddress(string relativeAddress);  //For example GetAbsoluteUri("/Learn/Index") returns "https://memcheckfr.azurewebsites.net/Learn/Index"
    }
    public interface IMemCheckMailSender
    {
        Task SendAsync(string toAddress, string subject, string body);
    }
    public sealed class NotificationMailer
    {
        #region Fields
        private readonly CallContext callContext;
        private readonly string globalReportToAddress;
        private readonly IMemCheckMailSender emailSender;
        private readonly IMemCheckLinkGenerator linkGenerator;
        #endregion
        #region Private methods
        private string GetAuthoringPageLink()
        {
            return linkGenerator.GetAbsoluteAddress("/Authoring/Index")!;
        }
        private string GetComparePageLink()
        {
            return linkGenerator.GetAbsoluteAddress("/Authoring/Compare")!;
        }
        private string GetHistoryPageLink()
        {
            return linkGenerator.GetAbsoluteAddress("/Authoring/History")!;
        }
        private void AddCardVersions(ImmutableArray<CardVersion> cardVersions, StringBuilder mailBody)
        {
            if (!cardVersions.Any())
                mailBody.Append("<h1>No card with new version</h1>");
            else
            {
                mailBody.Append(CultureInfo.InvariantCulture, $"<h1>{cardVersions.Length} Cards with new versions</h1>");
                mailBody.Append("<ul>");
                foreach (var card in cardVersions.OrderBy(cardVersion => cardVersion.VersionUtcDate))
                {
                    mailBody.Append("<li>");
                    mailBody.Append(CultureInfo.InvariantCulture, $"<a href={GetAuthoringPageLink()}?CardId={card.CardId}>{card.FrontSide}</a><br/>");
                    mailBody.Append(CultureInfo.InvariantCulture, $"By {card.VersionCreator}<br/>");
                    mailBody.Append(CultureInfo.InvariantCulture, $"On {card.VersionUtcDate} (UTC)<br/>");
                    mailBody.Append(CultureInfo.InvariantCulture, $"Version description: '{card.VersionDescription}'<br/>");
                    mailBody.Append(CultureInfo.InvariantCulture, $"<a href={GetHistoryPageLink()}?CardId={card.CardId}>History</a> - ");
                    if (card.VersionIdOnLastNotification != null)
                        mailBody.Append(CultureInfo.InvariantCulture, $"<a href={GetComparePageLink()}?CardId={card.CardId}&VersionId={card.VersionIdOnLastNotification}>View changes since your last notification</a>");
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
                mailBody.Append(CultureInfo.InvariantCulture, $"<h1>{deletedCards.Length} Deleted cards</h1>");
                mailBody.Append("<ul>");
                foreach (var card in deletedCards.OrderBy(card => card.DeletionUtcDate))
                {
                    mailBody.Append("<li>");
                    mailBody.Append(CultureInfo.InvariantCulture, $"{card.FrontSide}<br/>");
                    mailBody.Append(CultureInfo.InvariantCulture, $"By {card.DeletionAuthor}<br/>");
                    mailBody.Append(CultureInfo.InvariantCulture, $"On {card.DeletionUtcDate} (UTC)<br/>");
                    mailBody.Append(CultureInfo.InvariantCulture, $"Deletion description: '{card.DeletionDescription}'");
                    mailBody.Append("</li>");
                }
                mailBody.Append("</ul>");
            }
        }
        private void AddSearchNotifications(UserSearchNotifierResult searchNotifications, StringBuilder mailBody)
        {
            mailBody.Append(CultureInfo.InvariantCulture, $"<h1>{searchNotifications.SubscriptionName} search</h1>");

            if (searchNotifications.TotalNewlyFoundCardCount == 0)
                mailBody.Append("<h2>No newly found card</h2>");
            else
            {
                mailBody.Append(CultureInfo.InvariantCulture, $"<h2>{searchNotifications.TotalNewlyFoundCardCount} newly found cards</h2>");
                if (searchNotifications.TotalNewlyFoundCardCount > searchNotifications.NewlyFoundCards.Length)
                    mailBody.Append(CultureInfo.InvariantCulture, $"<p>Showing {searchNotifications.NewlyFoundCards.Length} first cards.</p>");
                mailBody.Append("<ul>");
                foreach (var card in searchNotifications.NewlyFoundCards.OrderBy(card => card.VersionUtcDate))
                {
                    mailBody.Append("<li>");
                    mailBody.Append(CultureInfo.InvariantCulture, $"<a href={GetAuthoringPageLink()}?CardId={card.CardId}>{card.FrontSide}</a><br/>");
                    mailBody.Append(CultureInfo.InvariantCulture, $"Changed by '{card.VersionCreator}'<br/>");
                    mailBody.Append(CultureInfo.InvariantCulture, $"On {card.VersionUtcDate} (UTC)<br/>");
                    mailBody.Append(CultureInfo.InvariantCulture, $"Version description: '{card.VersionDescription}'");
                    mailBody.Append("</li>");
                }
                mailBody.Append("</ul>");
            }

            if (searchNotifications.CountOfCardsNotFoundAnymoreStillExistsUserAllowedToView > 0
                || searchNotifications.CountOfCardsNotFoundAnymoreDeletedUserAllowedToView > 0
                || searchNotifications.CountOfCardsNotFoundAnymoreStillExistsUserNotAllowedToView > 0
                || searchNotifications.CountOfCardsNotFoundAnymoreDeletedUserNotAllowedToView > 0
                )
            {
                mailBody.Append("<h2>Cards no more reported by this search</h2>");

                if (searchNotifications.CountOfCardsNotFoundAnymoreStillExistsUserAllowedToView > 0)
                {
                    mailBody.Append("<ul>");
                    foreach (var card in searchNotifications.CardsNotFoundAnymoreStillExistsUserAllowedToView.OrderBy(card => card.VersionUtcDate))
                    {
                        mailBody.Append("<li>");
                        mailBody.Append(CultureInfo.InvariantCulture, $"<a href={GetAuthoringPageLink()}?CardId={card.CardId}>{card.FrontSide}</a><br/>");
                        mailBody.Append(CultureInfo.InvariantCulture, $"Changed by user '{card.VersionCreator}' and not reported anymore by this search<br/>");
                        mailBody.Append(CultureInfo.InvariantCulture, $"On {card.VersionUtcDate} (UTC)<br/>");
                        mailBody.Append(CultureInfo.InvariantCulture, $"Version description: '{card.VersionDescription}'");
                        mailBody.Append("</li>");
                    }
                    if (searchNotifications.CountOfCardsNotFoundAnymoreStillExistsUserAllowedToView > searchNotifications.CardsNotFoundAnymoreStillExistsUserAllowedToView.Length)
                    {
                        mailBody.Append("<li>");
                        mailBody.Append(CultureInfo.InvariantCulture, $"And {searchNotifications.CountOfCardsNotFoundAnymoreStillExistsUserAllowedToView - searchNotifications.CardsNotFoundAnymoreStillExistsUserAllowedToView.Length} more");
                        mailBody.Append("</li>");
                    }
                    mailBody.Append("</ul>");
                }

                if (searchNotifications.CountOfCardsNotFoundAnymoreDeletedUserAllowedToView > 0)
                {
                    mailBody.Append("<ul>");
                    foreach (var card in searchNotifications.CardsNotFoundAnymoreDeletedUserAllowedToView)
                    {
                        mailBody.Append("<li>");
                        mailBody.Append(CultureInfo.InvariantCulture, $"{card.FrontSide}<br/>");
                        mailBody.Append(CultureInfo.InvariantCulture, $"Deleted by user '{card.DeletionAuthor}'<br/>");
                        mailBody.Append(CultureInfo.InvariantCulture, $"On {card.DeletionUtcDate} (UTC)<br/>");
                        mailBody.Append(CultureInfo.InvariantCulture, $"With description: '{card.DeletionDescription}'");
                        mailBody.Append("</li>");
                    }
                    if (searchNotifications.CountOfCardsNotFoundAnymoreDeletedUserAllowedToView > searchNotifications.CardsNotFoundAnymoreDeletedUserAllowedToView.Length)
                    {
                        mailBody.Append("<li>");
                        mailBody.Append(CultureInfo.InvariantCulture, $"And {searchNotifications.CountOfCardsNotFoundAnymoreDeletedUserAllowedToView - searchNotifications.CardsNotFoundAnymoreDeletedUserAllowedToView.Length} more");
                        mailBody.Append("</li>");
                    }
                    mailBody.Append("</ul>");
                }

                if (searchNotifications.CountOfCardsNotFoundAnymoreStillExistsUserNotAllowedToView > 0)
                {
                    mailBody.Append("<ul>");
                    mailBody.Append("<li>");
                    mailBody.Append(CultureInfo.InvariantCulture, $"{searchNotifications.CountOfCardsNotFoundAnymoreStillExistsUserNotAllowedToView} cards have been modified and made private, preventing you from seeing them");
                    mailBody.Append("</li>");
                    mailBody.Append("</ul>");
                }

                if (searchNotifications.CountOfCardsNotFoundAnymoreDeletedUserNotAllowedToView > 0)
                {
                    mailBody.Append("<ul>");
                    mailBody.Append("<li>");
                    mailBody.Append(CultureInfo.InvariantCulture, $"{searchNotifications.CountOfCardsNotFoundAnymoreDeletedUserNotAllowedToView} cards have been made private and deleted, can not show any detail");
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
            mailBody.Append(CultureInfo.InvariantCulture, $"<p>Hello {userNotifications.UserName}</p>");
            mailBody.Append("<h1>Summary</h1>");
            mailBody.Append("<p>");
            mailBody.Append(CultureInfo.InvariantCulture, $"{userNotifications.SubscribedCardCount} registered cards<br/>");
            mailBody.Append(CultureInfo.InvariantCulture, $"Search finished at {DateTime.UtcNow}<br/>");
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


            mailBody.Append(CultureInfo.InvariantCulture, $"<p>Sent {sentEmailCount} emails.</p>");
            mailBody.Append($"<p>Perf indicators...</p>");
            mailBody.Append("<ul>");
            foreach (var performanceIndicator in performanceIndicators)
                mailBody.Append(CultureInfo.InvariantCulture, $"<li>{performanceIndicator}</li>");
            mailBody.Append("</ul>");
            mailBody.Append(CultureInfo.InvariantCulture, $"<p>Finished at {DateTime.UtcNow} (UTC)</p>");

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
                || searchNotificaction.CountOfCardsNotFoundAnymoreStillExistsUserAllowedToView > 0
                || searchNotificaction.CountOfCardsNotFoundAnymoreDeletedUserAllowedToView > 0
                || searchNotificaction.CountOfCardsNotFoundAnymoreStillExistsUserNotAllowedToView > 0
                || searchNotificaction.CountOfCardsNotFoundAnymoreDeletedUserNotAllowedToView > 0
                );
        }
        #endregion
        public NotificationMailer(CallContext callContext, string globalReportToAddress, IMemCheckMailSender emailSender, IMemCheckLinkGenerator linkGenerator)
        {
            this.callContext = callContext;
            this.globalReportToAddress = globalReportToAddress;
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

            mailSendingsToWaitFor.Add(emailSender.SendAsync(globalReportToAddress, "Notifier ended on success", GetAdminMailBody(sentEmailCount, performanceIndicators)));
            await Task.WhenAll(mailSendingsToWaitFor);
        }
    }
}
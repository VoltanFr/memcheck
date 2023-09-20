using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MemCheck.Basics;

namespace MemCheck.Application.Notifiying;

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
    private void AddCardVersions(ImmutableArray<IUserCardVersionsNotifier.ResultCard> xcards, StringBuilder mailBody)
    {
        var cardsWithNewVersions = xcards.Where(resultCard => resultCard.CardVersions.Any()).ToImmutableArray();
        mailBody = mailBody
            .Append(CultureInfo.InvariantCulture, $"<h1>{cardsWithNewVersions.Length} Cards with new versions</h1>")
            .Append("<ul>");
        foreach (var cardWithNewVersions in cardsWithNewVersions)
        {
            mailBody = mailBody
                .Append("<li>")
                .Append(CultureInfo.InvariantCulture, $"<a href={GetAuthoringPageLink()}?CardId={cardWithNewVersions.CardId}>{cardWithNewVersions.CardId}</a><br/>")
                .Append("<ul>");
            foreach (var cardVersion in cardWithNewVersions.CardVersions.OrderBy(cardVersion => cardVersion.VersionUtcDate))
            {
                mailBody = mailBody
                    .Append("<li>")
                    .Append(CultureInfo.InvariantCulture, $"{cardVersion.FrontSide}<br/>")
                    .Append(CultureInfo.InvariantCulture, $"By {cardVersion.VersionCreator}<br/>")
                    .Append(CultureInfo.InvariantCulture, $"On {cardVersion.VersionUtcDate} (UTC)<br/>")
                    .Append(CultureInfo.InvariantCulture, $"Version description: '{cardVersion.VersionDescription}'<br/>")
                    .Append(CultureInfo.InvariantCulture, $"<a href={GetHistoryPageLink()}?CardId={cardVersion.CardId}>History</a> - ");
                mailBody = cardVersion.VersionIdOnLastNotification != null
                    ? mailBody.Append(CultureInfo.InvariantCulture, $"<a href={GetComparePageLink()}?CardId={cardVersion.CardId}&VersionId={cardVersion.VersionIdOnLastNotification}>View changes since your last notification</a>")
                    : mailBody.Append("Did not exist on your previous notifications");
                mailBody = mailBody.Append("</li>");
            }
            mailBody = mailBody
                .Append("</ul>")
                .Append("</li>");
        }
        mailBody = mailBody.Append("</ul>");
    }
    private void AddCardDiscussions(ImmutableArray<IUserCardVersionsNotifier.ResultCard> xcards, StringBuilder mailBody)
    {
        var cardsWithNewDiscussionEntries = xcards.Where(resultCard => resultCard.DiscussionEntries.Any()).ToImmutableArray();
        mailBody = mailBody
            .Append(CultureInfo.InvariantCulture, $"<h1>{cardsWithNewDiscussionEntries.Length} Cards with new discussion entries</h1>")
            .Append("<ul>");
        foreach (var cardWithNewDiscussionEntries in cardsWithNewDiscussionEntries)
        {
            mailBody = mailBody
                .Append("<li>")
                .Append(CultureInfo.InvariantCulture, $"<a href={GetAuthoringPageLink()}?CardId={cardWithNewDiscussionEntries.CardId}>{cardWithNewDiscussionEntries.CardId}</a><br/>")
                .Append("<ul>");
            foreach (var discussionEntry in cardWithNewDiscussionEntries.DiscussionEntries.OrderBy(discussionEntry => discussionEntry.CreationUtcDate))
            {
                mailBody = mailBody
                    .Append("<li>")
                    .Append(CultureInfo.InvariantCulture, $"By {discussionEntry.VersionCreator}<br/>")
                    .Append(CultureInfo.InvariantCulture, $"On {discussionEntry.CreationUtcDate} (UTC)<br/>");
                if (discussionEntry.Text != null)
                    mailBody.Append(CultureInfo.InvariantCulture, $"Text hint: '{discussionEntry.Text.Truncate(50)}'<br/>");
                mailBody = mailBody.Append("</li>");
            }
            mailBody = mailBody
                .Append("</ul>")
                .Append("</li>");
        }
        mailBody = mailBody.Append("</ul>");
    }
    private static void AddCardDeletions(ImmutableArray<CardDeletion> deletedCards, StringBuilder mailBody)
    {
        if (!deletedCards.Any())
            mailBody = mailBody.Append("<h1>No deleted card</h1>");
        else
        {
            mailBody = mailBody
                .Append(CultureInfo.InvariantCulture, $"<h1>{deletedCards.Length} Deleted cards</h1>")
                .Append("<ul>");
            foreach (var card in deletedCards.OrderBy(card => card.DeletionUtcDate))
            {
                mailBody = mailBody
                    .Append("<li>")
                    .Append(CultureInfo.InvariantCulture, $"{card.FrontSide}<br/>")
                    .Append(CultureInfo.InvariantCulture, $"By {card.DeletionAuthor}<br/>")
                    .Append(CultureInfo.InvariantCulture, $"On {card.DeletionUtcDate} (UTC)<br/>")
                    .Append(CultureInfo.InvariantCulture, $"Deletion description: '{card.DeletionDescription}'")
                    .Append("</li>");
            }
            mailBody = mailBody.Append("</ul>");
        }
    }
    private void AddSearchNotifications(UserSearchNotifierResult searchNotifications, StringBuilder mailBody)
    {
        mailBody = mailBody.Append(CultureInfo.InvariantCulture, $"<h1>{searchNotifications.SubscriptionName} search</h1>");

        if (searchNotifications.TotalNewlyFoundCardCount == 0)
            mailBody = mailBody.Append("<h2>No newly found card</h2>");
        else
        {
            mailBody = mailBody.Append(CultureInfo.InvariantCulture, $"<h2>{searchNotifications.TotalNewlyFoundCardCount} newly found cards</h2>");
            if (searchNotifications.TotalNewlyFoundCardCount > searchNotifications.NewlyFoundCards.Length)
                mailBody = mailBody.Append(CultureInfo.InvariantCulture, $"<p>Showing {searchNotifications.NewlyFoundCards.Length} first cards.</p>");
            mailBody = mailBody.Append("<ul>");
            foreach (var card in searchNotifications.NewlyFoundCards.OrderBy(card => card.VersionUtcDate))
            {
                mailBody = mailBody
                    .Append("<li>")
                    .Append(CultureInfo.InvariantCulture, $"<a href={GetAuthoringPageLink()}?CardId={card.CardId}>{card.FrontSide}</a><br/>")
                    .Append(CultureInfo.InvariantCulture, $"Changed by '{card.VersionCreator}'<br/>")
                    .Append(CultureInfo.InvariantCulture, $"On {card.VersionUtcDate} (UTC)<br/>")
                    .Append(CultureInfo.InvariantCulture, $"Version description: '{card.VersionDescription}'")
                    .Append("</li>");
            }
            mailBody = mailBody.Append("</ul>");
        }

        if (searchNotifications.CountOfCardsNotFoundAnymoreStillExistsUserAllowedToView > 0
            || searchNotifications.CountOfCardsNotFoundAnymoreDeletedUserAllowedToView > 0
            || searchNotifications.CountOfCardsNotFoundAnymoreStillExistsUserNotAllowedToView > 0
            || searchNotifications.CountOfCardsNotFoundAnymoreDeletedUserNotAllowedToView > 0
            )
        {
            mailBody = mailBody.Append("<h2>Cards no more reported by this search</h2>");

            if (searchNotifications.CountOfCardsNotFoundAnymoreStillExistsUserAllowedToView > 0)
            {
                mailBody = mailBody.Append("<ul>");
                foreach (var card in searchNotifications.CardsNotFoundAnymoreStillExistsUserAllowedToView.OrderBy(card => card.VersionUtcDate))
                {
                    mailBody = mailBody
                        .Append("<li>")
                        .Append(CultureInfo.InvariantCulture, $"<a href={GetAuthoringPageLink()}?CardId={card.CardId}>{card.FrontSide}</a><br/>")
                        .Append(CultureInfo.InvariantCulture, $"Changed by user '{card.VersionCreator}' and not reported anymore by this search<br/>")
                        .Append(CultureInfo.InvariantCulture, $"On {card.VersionUtcDate} (UTC)<br/>")
                        .Append(CultureInfo.InvariantCulture, $"Version description: '{card.VersionDescription}'")
                        .Append("</li>");
                }
                if (searchNotifications.CountOfCardsNotFoundAnymoreStillExistsUserAllowedToView > searchNotifications.CardsNotFoundAnymoreStillExistsUserAllowedToView.Length)
                {
                    mailBody = mailBody
                        .Append("<li>")
                        .Append(CultureInfo.InvariantCulture, $"And {searchNotifications.CountOfCardsNotFoundAnymoreStillExistsUserAllowedToView - searchNotifications.CardsNotFoundAnymoreStillExistsUserAllowedToView.Length} more")
                        .Append("</li>");
                }
                mailBody = mailBody.Append("</ul>");
            }

            if (searchNotifications.CountOfCardsNotFoundAnymoreDeletedUserAllowedToView > 0)
            {
                mailBody = mailBody.Append("<ul>");
                foreach (var card in searchNotifications.CardsNotFoundAnymoreDeletedUserAllowedToView)
                {
                    mailBody = mailBody
                        .Append("<li>")
                        .Append(CultureInfo.InvariantCulture, $"{card.FrontSide}<br/>")
                        .Append(CultureInfo.InvariantCulture, $"Deleted by user '{card.DeletionAuthor}'<br/>")
                        .Append(CultureInfo.InvariantCulture, $"On {card.DeletionUtcDate} (UTC)<br/>")
                        .Append(CultureInfo.InvariantCulture, $"With description: '{card.DeletionDescription}'")
                        .Append("</li>");
                }
                if (searchNotifications.CountOfCardsNotFoundAnymoreDeletedUserAllowedToView > searchNotifications.CardsNotFoundAnymoreDeletedUserAllowedToView.Length)
                {
                    mailBody = mailBody
                        .Append("<li>")
                        .Append(CultureInfo.InvariantCulture, $"And {searchNotifications.CountOfCardsNotFoundAnymoreDeletedUserAllowedToView - searchNotifications.CardsNotFoundAnymoreDeletedUserAllowedToView.Length} more")
                        .Append("</li>");
                }
                mailBody = mailBody.Append("</ul>");
            }

            if (searchNotifications.CountOfCardsNotFoundAnymoreStillExistsUserNotAllowedToView > 0)
            {
                mailBody = mailBody
                    .Append("<ul>")
                    .Append("<li>")
                    .Append(CultureInfo.InvariantCulture, $"{searchNotifications.CountOfCardsNotFoundAnymoreStillExistsUserNotAllowedToView} cards have been modified and made private, preventing you from seeing them")
                    .Append("</li>")
                    .Append("</ul>");
            }

            if (searchNotifications.CountOfCardsNotFoundAnymoreDeletedUserNotAllowedToView > 0)
            {
                mailBody = mailBody
                    .Append("<ul>")
                    .Append("<li>")
                    .Append(CultureInfo.InvariantCulture, $"{searchNotifications.CountOfCardsNotFoundAnymoreDeletedUserNotAllowedToView} cards have been made private and deleted, can not show any detail")
                    .Append("</li>")
                    .Append("</ul>");
            }
        }
        else
            mailBody = mailBody.Append("<h2>No card is not reported by this search anymore</h2>");
    }
    private string GetMailBodyForUser(Notifier.UserNotifications userNotifications)
    {
        var mailBody = new StringBuilder()
            .Append("<html>")
            .Append("<body>")
            .Append(CultureInfo.InvariantCulture, $"<p>Hello {userNotifications.UserName}</p>")
            .Append("<h1>Summary</h1>")
            .Append("<p>")
            .Append(CultureInfo.InvariantCulture, $"{userNotifications.SubscribedCardCount} registered cards<br/>")
            .Append(CultureInfo.InvariantCulture, $"Search finished at {DateTime.UtcNow}<br/>")
            .Append("</p>");

        AddCardVersions(userNotifications.Cards, mailBody);
        AddCardDiscussions(userNotifications.Cards, mailBody);
        AddCardDeletions(userNotifications.DeletedCards, mailBody);
        foreach (var searchNotifications in userNotifications.SearchNotificactions)
            AddSearchNotifications(searchNotifications, mailBody);

        mailBody = mailBody
            .Append("</body>")
            .Append("</html>");

        return mailBody.ToString();
    }
    private static StringBuilder GetAdminMailBody(int sentEmailCount, List<string> performanceIndicators)
    {
        var mailBody = new StringBuilder()
            .Append(CultureInfo.InvariantCulture, $"<p>Sent {sentEmailCount} emails.</p>")
            .Append($"<p>Perf indicators...</p>")
            .Append("<ul>");
        foreach (var performanceIndicator in performanceIndicators)
            mailBody = mailBody.Append(CultureInfo.InvariantCulture, $"<li>{performanceIndicator}</li>");
        mailBody = mailBody
            .Append("</ul>")
            .Append(CultureInfo.InvariantCulture, $"<p>Finished at {DateTime.UtcNow} (UTC)</p>");
        return mailBody;
    }
    private static bool MustSendForNotifications(Notifier.UserNotifications userNotifications)
    {
        return userNotifications.Cards.Any()
            || userNotifications.DeletedCards.Any()
            || userNotifications.SearchNotificactions.Any(searchNotificaction =>
                searchNotificaction.TotalNewlyFoundCardCount > 0
                || searchNotificaction.CountOfCardsNotFoundAnymoreStillExistsUserAllowedToView > 0
                || searchNotificaction.CountOfCardsNotFoundAnymoreDeletedUserAllowedToView > 0
                || searchNotificaction.CountOfCardsNotFoundAnymoreStillExistsUserNotAllowedToView > 0
                || searchNotificaction.CountOfCardsNotFoundAnymoreDeletedUserNotAllowedToView > 0
                );
    }
    #endregion
    public NotificationMailer(CallContext callContext, IMemCheckMailSender emailSender, IMemCheckLinkGenerator linkGenerator)
    {
        this.callContext = callContext;
        this.emailSender = emailSender;
        this.linkGenerator = linkGenerator;
    }
    public async Task<StringBuilder> RunAndCreateReportMailMainPartAsync()
    {
        var mailSendingsToWaitFor = new List<Task>();
        var performanceIndicators = new List<string>();
        var notifierResult = await new Notifier(callContext, performanceIndicators).RunAsync(new Notifier.Request());
        var sentEmailCount = 0;

        foreach (var userNotifications in notifierResult.UserNotifications)
            if (MustSendForNotifications(userNotifications))
            {
                var mailBody = GetMailBodyForUser(userNotifications);
                mailSendingsToWaitFor.Add(emailSender.SendAsync(userNotifications.UserEmail, "Mnesios notifications", mailBody));
                sentEmailCount++;
            }

        await Task.WhenAll(mailSendingsToWaitFor);

        return GetAdminMailBody(sentEmailCount, performanceIndicators);
    }
}

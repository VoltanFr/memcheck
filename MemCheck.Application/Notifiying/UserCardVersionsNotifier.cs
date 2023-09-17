using MemCheck.Application.QueryValidation;
using MemCheck.Basics;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using static MemCheck.Application.Notifiying.IUserCardVersionsNotifier;

namespace MemCheck.Application.Notifiying;

/* Lists the notifications to send per card, which can contain card versions and discussion entries.
*/

internal interface IUserCardVersionsNotifier
{
    public Task<CardVersionsNotifierResult> RunAsync(Guid userId);
    #region Result types
    public sealed record CardVersionsNotifierResult(ImmutableArray<ResultCard> Cards);
    public sealed record ResultCard(Guid CardId, ImmutableArray<ResultCardVersion> CardVersions, ImmutableArray<ResultDiscussionEntry> DiscussionEntries);
    public sealed class ResultCardVersion
    {
        public ResultCardVersion(Guid cardId, string frontSide, string versionCreator, DateTime versionUtcDate, string versionDescription, bool cardIsViewable, Guid? versionIdOnLastNotification, CardNotificationSubscription subscription)
        {
            CardId = cardId;
            FrontSide = cardIsViewable ? frontSide.Truncate(Notifier.MaxLengthForTextFields) : null;
            VersionCreator = versionCreator;
            VersionUtcDate = versionUtcDate;
            VersionDescription = cardIsViewable ? versionDescription.Truncate(Notifier.MaxLengthForTextFields) : null;
            CardIsViewable = cardIsViewable;
            VersionIdOnLastNotification = versionIdOnLastNotification;
            Subscription = subscription;
        }
        public Guid CardId { get; }
        public string? FrontSide { get; }
        public string VersionCreator { get; }
        public DateTime VersionUtcDate { get; }
        public string? VersionDescription { get; }
        public bool CardIsViewable { get; } // User is registered for notif on a card, but does not have access to it
        public Guid? VersionIdOnLastNotification { get; }
        public CardNotificationSubscription Subscription { get; }
    }
    public sealed class ResultDiscussionEntry
    {
        public ResultDiscussionEntry(Guid discussionEntryId, string versionCreator, string text, DateTime creationUtcDate, bool cardIsViewable, CardNotificationSubscription subscription)
        {
            DiscussionEntryId = discussionEntryId;
            VersionCreator = versionCreator;
            Text = cardIsViewable ? text.Truncate(Notifier.MaxLengthForTextFields) : null;
            CreationUtcDate = creationUtcDate;
            Subscription = subscription;
        }
        public Guid DiscussionEntryId { get; }
        public string VersionCreator { get; }
        public string? Text { get; } // null if text was in a private version of the card
        public DateTime CreationUtcDate { get; }
        public CardNotificationSubscription Subscription { get; }
    }
    #endregion
}
internal sealed class UserCardVersionsNotifier : IUserCardVersionsNotifier
{
    #region Fields
    private readonly CallContext callContext;
    private readonly ICollection<string> performanceIndicators;
    private readonly DateTime runningUtcDate;
    #endregion
    #region Private methods
    private Guid? GetCardVersionOn(Guid cardId, DateTime utc)
    {
        var resultVersion = callContext.DbContext.CardPreviousVersions.AsNoTracking()
            .Where(cardVersion => cardVersion.Card == cardId && cardVersion.VersionUtcDate <= utc)
            .OrderByDescending(cardVersion => cardVersion.VersionUtcDate)
            .FirstOrDefault();
        return resultVersion?.Id;
    }
    private async Task<ImmutableArray<ResultCardVersion>> GetCardVersions(Guid userId)
    {
        performanceIndicators.Add($"{GetType().Name} querying DB for card versions");
        var chrono = Stopwatch.StartNew();
        var cardVersions = await callContext.DbContext.Cards
            .Include(card => card.VersionCreator)
            .Include(card => card.UsersWithView)
            .Join(callContext.DbContext.CardNotifications.Where(cardNotif => cardNotif.UserId == userId), card => card.Id, cardNotif => cardNotif.CardId, (card, cardNotif) => new { card, cardNotif })
            .Where(cardAndNotif => cardAndNotif.card.VersionUtcDate > cardAndNotif.cardNotif.LastNotificationUtcDate)
            .ToImmutableArrayAsync();
        performanceIndicators.Add($"{GetType().Name} took {chrono.Elapsed:hh\\:mm\\:ss\\:fff} to list user's registered cards with new versions (result count={cardVersions.Length})");

        chrono.Restart();
        var result = cardVersions.Select(cardToReport =>
            new ResultCardVersion(
                cardToReport.card.Id,
                cardToReport.card.FrontSide,
                cardToReport.card.VersionCreator.GetUserName(),
                cardToReport.card.VersionUtcDate,
                cardToReport.card.VersionDescription,
                CardVisibilityHelper.CardIsVisibleToUser(userId, cardToReport.card),
                GetCardVersionOn(cardToReport.card.Id, cardToReport.cardNotif.LastNotificationUtcDate),
                cardToReport.cardNotif)
            ).ToImmutableArray();
        performanceIndicators.Add($"{GetType().Name} took {chrono.Elapsed} to create the result list with getting card version on last notif");

        return result;
    }
    private async Task<ImmutableArray<ResultDiscussionEntry>> GetDiscussionEntries(Guid userId)
    {
        performanceIndicators.Add($"{GetType().Name} querying DB for card discussion entries");
        var chrono = Stopwatch.StartNew();
        var cardDiscussionEntries = await callContext.DbContext.Cards
            .Include(card => card.LatestDiscussionEntry)
            .ThenInclude(discussionEntry => discussionEntry!.Creator)
            .Include(card => card.UsersWithView)
            .Join(callContext.DbContext.CardNotifications.Where(cardNotif => cardNotif.UserId == userId), card => card.Id, cardNotif => cardNotif.CardId, (card, cardNotif) => new { card, cardNotif })
            .Where(cardAndNotif => cardAndNotif.card.LatestDiscussionEntry != null && cardAndNotif.card.LatestDiscussionEntry.CreationUtcDate > cardAndNotif.cardNotif.LastNotificationUtcDate)
            .Select(cardAndNotif => new { cardAndNotif.card.LatestDiscussionEntry, cardAndNotif.cardNotif, UsersWithView = cardAndNotif.card.UsersWithView.Select(usersWithView => usersWithView.UserId) })
            .ToImmutableArrayAsync();
        performanceIndicators.Add($"{GetType().Name} took {chrono.Elapsed:hh\\:mm\\:ss\\:fff} to list user's registered cards with new discussion entries (result count={cardDiscussionEntries.Length})");

        chrono.Restart();
        var result = cardDiscussionEntries.Select(cardToReport =>
            new ResultDiscussionEntry(
                cardToReport.LatestDiscussionEntry.Id,
                cardToReport.LatestDiscussionEntry.Creator.GetUserName(),
                cardToReport.LatestDiscussionEntry.Text,
                cardToReport.LatestDiscussionEntry.CreationUtcDate,
                CardVisibilityHelper.CardIsVisibleToUser(userId, cardToReport.UsersWithView),
                cardToReport.cardNotif)
            ).ToImmutableArray();

        return result;
    }
    #endregion
    public UserCardVersionsNotifier(CallContext callContext, ICollection<string> performanceIndicators)
    {
        //Prod constructor
        this.callContext = callContext;
        this.performanceIndicators = performanceIndicators;
        runningUtcDate = DateTime.UtcNow;
    }
    public UserCardVersionsNotifier(CallContext callContext, DateTime runningUtcDate)
    {
        //Unit tests constructor
        this.callContext = callContext;
        performanceIndicators = new List<string>();
        this.runningUtcDate = runningUtcDate;
    }
    public async Task<CardVersionsNotifierResult> RunAsync(Guid userId)
    {
        var allCardVersions = await GetCardVersions(userId);
        var resultCardVersions = allCardVersions.GroupBy(cardVersion => cardVersion.CardId).ToImmutableDictionary(cardIdAndVersions => cardIdAndVersions.Key, cardIdAndVersions => cardIdAndVersions.ToImmutableArray());
        var allDiscussionEntries = await GetDiscussionEntries(userId);
        var resultDiscussionEntries = allDiscussionEntries.GroupBy(cardDiscussionEntry => cardDiscussionEntry.Subscription.CardId).ToImmutableDictionary(cardIdAndDiscussionEntry => cardIdAndDiscussionEntry.Key, cardIdAndDiscussionEntry => cardIdAndDiscussionEntry.ToImmutableArray());
        var allCardIds = resultCardVersions.Keys.Union(resultDiscussionEntries.Keys);

        var result = new List<ResultCard>();
        foreach (var cardId in allCardIds)
        {
            var hasVersions = resultCardVersions.TryGetValue(cardId, out var cardVersions);
            var hasDiscussionEntries = resultDiscussionEntries.TryGetValue(cardId, out var discussionEntries);
            result.Add(new ResultCard(cardId, hasVersions ? cardVersions : ImmutableArray<ResultCardVersion>.Empty, hasDiscussionEntries ? discussionEntries : ImmutableArray<ResultDiscussionEntry>.Empty));
        }

        var chrono = Stopwatch.StartNew();
        foreach (var cardVersion in allCardVersions)
            cardVersion.Subscription.LastNotificationUtcDate = runningUtcDate;
        foreach (var discussionEntry in allDiscussionEntries)
            discussionEntry.Subscription.LastNotificationUtcDate = runningUtcDate;
        await callContext.DbContext.SaveChangesAsync();

        performanceIndicators.Add($"{GetType().Name} took {chrono.Elapsed} to update user's registered cards last notif date");
        callContext.TelemetryClient.TrackEvent("UserCardVersionsNotifier", ClassWithMetrics.IntMetric("ResultCount", result.Count));
        return new CardVersionsNotifierResult(result.ToImmutableArray());
    }
}

using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Notifiying;

internal interface IUserCardDeletionsNotifier
{
    public Task<ImmutableArray<CardDeletion>> RunAsync(Guid userId);
}
internal sealed class UserCardDeletionsNotifier : IUserCardDeletionsNotifier
{
    #region Fields
    private readonly CallContext callContext;
    private readonly ICollection<string> performanceIndicators;
    private readonly DateTime runningUtcDate;
    #endregion
    public UserCardDeletionsNotifier(CallContext callContext, ICollection<string> performanceIndicators)
    {
        //prod constructor
        this.callContext = callContext;
        this.performanceIndicators = performanceIndicators;
        runningUtcDate = DateTime.UtcNow;
    }
    public UserCardDeletionsNotifier(CallContext callContext, DateTime runningUtcDate)
    {
        //Unit tests constructor
        this.callContext = callContext;
        performanceIndicators = new List<string>();
        this.runningUtcDate = runningUtcDate;
    }
    public async Task<ImmutableArray<CardDeletion>> RunAsync(Guid userId)
    {
        //It is a little strange to keep checking for deleted cards when the user has been notified of their deletion. But I'm not clear right now about what to do in case a card is undeleted

        var chrono = Stopwatch.StartNew();
        var deletedCards = callContext.DbContext.CardPreviousVersions.Include(card => card.UsersWithView)
            .Join(
                callContext.DbContext.CardNotifications.Where(cardNotif => cardNotif.UserId == userId),
                previousVersion => previousVersion.Card,
                cardNotif => cardNotif.CardId,
                (previousVersion, cardNotif) => new { previousVersion, cardNotif }
            )
            .Where(cardAndNotif => cardAndNotif.previousVersion.VersionType == CardPreviousVersionType.Deletion && cardAndNotif.previousVersion.VersionUtcDate > cardAndNotif.cardNotif.LastNotificationUtcDate);

        foreach (var cardVersion in deletedCards)
            cardVersion.cardNotif.LastNotificationUtcDate = runningUtcDate;

        var result = deletedCards.Select(cardToReport =>
                         new CardDeletion(
                             cardToReport.previousVersion.FrontSide,
                             cardToReport.previousVersion.VersionCreator.GetUserName(),
                             cardToReport.previousVersion.VersionUtcDate,
                             cardToReport.previousVersion.VersionDescription,
                             !cardToReport.previousVersion.UsersWithView.Any() || cardToReport.previousVersion.UsersWithView.Any(u => u.AllowedUserId == userId)
                         )
                  ).ToImmutableArray();

        await callContext.DbContext.SaveChangesAsync();
        performanceIndicators.Add($"{GetType().Name} took {chrono.Elapsed} to list user's subscribed cards which have been deleted, and update the notification's last notif date");

        callContext.TelemetryClient.TrackEvent("UserCardDeletionsNotifier", ClassWithMetrics.IntMetric("ResultCount", result.Length));
        return result;
    }
}

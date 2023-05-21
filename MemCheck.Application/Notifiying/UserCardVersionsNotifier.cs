﻿using MemCheck.Application.QueryValidation;
using MemCheck.Basics;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Notifiying;

public sealed record CardVersionsNotifierResult(ImmutableArray<CardVersion> CardVersions, ImmutableArray<CardDiscussionEntryNotification> CardDiscussionEntryNotifications);

public sealed record CardDiscussionEntryNotification();

internal interface IUserCardVersionsNotifier
{
    public Task<CardVersionsNotifierResult> RunAsync(Guid userId);
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
        performanceIndicators.Add($"{GetType().Name} querying DB");
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
                          new CardVersion(
                              cardToReport.card.Id,
                              cardToReport.card.FrontSide,
                              cardToReport.card.VersionCreator.GetUserName(),
                              cardToReport.card.VersionUtcDate,
                              cardToReport.card.VersionDescription,
                              CardVisibilityHelper.CardIsVisibleToUser(userId, cardToReport.card),
                              GetCardVersionOn(cardToReport.card.Id, cardToReport.cardNotif.LastNotificationUtcDate)
                          )
                    ).ToImmutableArray();
        performanceIndicators.Add($"{GetType().Name} took {chrono.Elapsed} to create the result list with getting card version on last notif");

        var cardDiscussionEntryNotifications = ImmutableArray<CardDiscussionEntryNotification>.Empty;

        chrono.Restart();
        foreach (var cardVersion in cardVersions)
            cardVersion.cardNotif.LastNotificationUtcDate = runningUtcDate;
        await callContext.DbContext.SaveChangesAsync();
        performanceIndicators.Add($"{GetType().Name} took {chrono.Elapsed} to update user's registered cards last notif date");
        callContext.TelemetryClient.TrackEvent("UserCardVersionsNotifier", ClassWithMetrics.IntMetric("ResultCount", result.Length));
        return new CardVersionsNotifierResult(result, cardDiscussionEntryNotifications);
    }
}

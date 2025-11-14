using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Notifiying;

// Returns the number of cards the given user is subscribed to

internal interface IUserCardSubscriptionCounter
{
    Task<int> RunAsync(Guid userId);
}
internal sealed class UserCardSubscriptionCounter : IUserCardSubscriptionCounter
{
    #region Fields
    private readonly CallContext callContext;
    private readonly ICollection<string> performanceIndicators;
    #endregion
    public UserCardSubscriptionCounter(CallContext callContext, ICollection<string>? performanceIndicators = null)
    {
        this.callContext = callContext;
        this.performanceIndicators = performanceIndicators ?? new List<string>();
    }
    public async Task<int> RunAsync(Guid userId)
    {
        var chrono = Stopwatch.StartNew();
        var result = await callContext.DbContext.CardNotifications.AsNoTracking().Where(notif => notif.UserId == userId).CountAsync();
        performanceIndicators.Add($"{GetType().Name} took {chrono.Elapsed:hh\\:mm\\:ss\\:fff} to count user's card subscriptions for user with id {userId} (result={result} cards)");
        callContext.TelemetryClient.TrackEvent("UserCardSubscriptionCounter", ClassWithMetrics.IntMetric("Result", result));
        return result;
    }
}

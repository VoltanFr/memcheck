using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Notifying
{
    internal interface IUserSearchSubscriptionLister
    {
        public Task<ImmutableArray<SearchSubscription>> RunAsync(Guid userId);
    }
    internal sealed class UserSearchSubscriptionLister : IUserSearchSubscriptionLister
    {
        #region Fields
        private readonly CallContext callContext;
        private readonly ICollection<string> performanceIndicators;
        #endregion
        public UserSearchSubscriptionLister(CallContext callContext, ICollection<string>? performanceIndicators = null)
        {
            this.callContext = callContext;
            this.performanceIndicators = performanceIndicators ?? new List<string>();
        }
        public async Task<ImmutableArray<SearchSubscription>> RunAsync(Guid userId)
        {
            var chrono = Stopwatch.StartNew();
            var result = (await callContext.DbContext.SearchSubscriptions.Where(notif => notif.UserId == userId).ToListAsync()).ToImmutableArray();
            performanceIndicators.Add($"{GetType().Name} took {chrono.Elapsed} to list user's search subscriptions");
            callContext.TelemetryClient.TrackEvent("UserSearchSubscriptionLister", ClassWithMetrics.IntMetric("ResultCount", result.Length));
            return result;
        }
    }
}
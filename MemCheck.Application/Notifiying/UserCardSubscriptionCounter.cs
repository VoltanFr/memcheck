using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Notifiying
{
    internal interface IUserCardSubscriptionCounter
    {
        public Task<int> RunAsync(Guid userId);
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
            var result = await callContext.DbContext.CardNotifications.Where(notif => notif.UserId == userId).CountAsync();
            performanceIndicators.Add($"{GetType().Name} took {chrono.Elapsed} to list user's card subscriptions");
            callContext.TelemetryClient.TrackEvent("UserCardSubscriptionCounter", ClassWithMetrics.IntMetric("Result", result));
            return result;
        }
    }
}
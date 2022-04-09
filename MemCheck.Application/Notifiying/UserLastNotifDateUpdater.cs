using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Notifying
{
    internal interface IUserLastNotifDateUpdater
    {
        Task RunAsync(Guid userId);
    }
    internal sealed class UserLastNotifDateUpdater : IUserLastNotifDateUpdater
    {
        #region Fields
        private readonly CallContext callContext;
        private readonly ICollection<string> performanceIndicators;
        private readonly DateTime runningUtcDate;
        #endregion
        public UserLastNotifDateUpdater(CallContext callContext, ICollection<string> performanceIndicators, DateTime runningUtcDate)
        {
            //Prod constructor
            this.callContext = callContext;
            this.performanceIndicators = performanceIndicators;
            this.runningUtcDate = runningUtcDate;
        }
        public UserLastNotifDateUpdater(CallContext callContext, DateTime runningUtcDate)
        {
            //Unit tests constructor
            this.callContext = callContext;
            performanceIndicators = new List<string>();
            this.runningUtcDate = runningUtcDate;
        }
        public async Task RunAsync(Guid userId)
        {
            var chrono = Stopwatch.StartNew();
            callContext.DbContext.Users.Single(u => u.Id == userId).LastNotificationUtcDate = runningUtcDate;
            await callContext.DbContext.SaveChangesAsync();
            performanceIndicators.Add($"{GetType().Name} took {chrono.Elapsed} to update user's last notif date");
            callContext.TelemetryClient.TrackEvent("UserLastNotifDateUpdater");
        }
    }
}
using MemCheck.Database;
using MemCheck.Domain;
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
        private readonly MemCheckDbContext dbContext;
        private readonly List<string> performanceIndicators;
        private readonly DateTime runningUtcDate;
        #endregion
        public UserLastNotifDateUpdater(MemCheckDbContext dbContext, List<string> performanceIndicators, DateTime runningUtcDate)
        {
            //Prod constructor
            this.dbContext = dbContext;
            this.performanceIndicators = performanceIndicators;
            this.runningUtcDate = runningUtcDate;
        }
        public UserLastNotifDateUpdater(MemCheckDbContext dbContext, DateTime runningUtcDate)
        {
            //Unit tests constructor
            this.dbContext = dbContext;
            performanceIndicators = new List<string>();
            this.runningUtcDate = runningUtcDate;
        }
        public async Task RunAsync(Guid userId)
        {
            var chrono = Stopwatch.StartNew();
            dbContext.Users.Single(u => u.Id == userId).LastNotificationUtcDate = runningUtcDate;
            await dbContext.SaveChangesAsync();
            performanceIndicators.Add($"{GetType().Name} took {chrono.Elapsed} to update user's last notif date");
        }
    }
}
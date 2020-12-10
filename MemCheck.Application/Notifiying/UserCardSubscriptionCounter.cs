using MemCheck.Database;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MemCheck.Application.Notifying
{
    internal interface IUserCardSubscriptionCounter
    {
        public Task<int> RunAsync(Guid userId);
    }
    internal sealed class UserCardSubscriptionCounter : IUserCardSubscriptionCounter
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        private readonly List<string> performanceIndicators;
        #endregion
        public UserCardSubscriptionCounter(MemCheckDbContext dbContext, List<string>? performanceIndicators = null)
        {
            this.dbContext = dbContext;
            this.performanceIndicators = performanceIndicators ?? new List<string>();
        }
        public async Task<int> RunAsync(Guid userId)
        {
            var chrono = Stopwatch.StartNew();
            var result = await dbContext.CardNotifications.Where(notif => notif.UserId == userId).CountAsync();
            performanceIndicators.Add($"{GetType().Name} took {chrono.Elapsed} to list user's card subscriptions");
            return result;
        }
    }
}
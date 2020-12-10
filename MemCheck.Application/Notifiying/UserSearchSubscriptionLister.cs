using MemCheck.Database;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System;
using System.Collections.Immutable;
using MemCheck.Domain;
using System.Collections.Generic;
using System.Diagnostics;

namespace MemCheck.Application.Notifying
{
    internal interface IUserSearchSubscriptionLister
    {
        public Task<ImmutableArray<SearchSubscription>> RunAsync(Guid userId);
    }
    internal sealed class UserSearchSubscriptionLister : IUserSearchSubscriptionLister
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        private readonly List<string> performanceIndicators;
        #endregion
        public UserSearchSubscriptionLister(MemCheckDbContext dbContext, List<string>? performanceIndicators = null)
        {
            this.dbContext = dbContext;
            this.performanceIndicators = performanceIndicators ?? new List<string>();
        }
        public async Task<ImmutableArray<SearchSubscription>> RunAsync(Guid userId)
        {
            var chrono = Stopwatch.StartNew();
            var result = (await dbContext.SearchSubscriptions.Where(notif => notif.UserId == userId).ToListAsync()).ToImmutableArray();
            performanceIndicators.Add($"{GetType().Name} took {chrono.Elapsed} to list user's search subscriptions");
            return result;
        }
    }
}
using MemCheck.Database;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System;
using System.Collections.Immutable;
using MemCheck.Domain;

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
        #endregion
        public UserSearchSubscriptionLister(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task<ImmutableArray<SearchSubscription>> RunAsync(Guid userId)
        {
            return (await dbContext.SearchSubscriptions.Where(notif => notif.UserId == userId).ToListAsync()).ToImmutableArray();
        }
    }
}
using MemCheck.Database;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System;

namespace MemCheck.Application.Notifying
{
    internal interface IUserSearchSubscriptionCounter
    {
        public Task<int> RunAsync(Guid userId);
    }
    internal sealed class UserSearchSubscriptionCounter : IUserSearchSubscriptionCounter
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public UserSearchSubscriptionCounter(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task<int> RunAsync(Guid userId)
        {
            return await dbContext.SearchSubscriptions.Where(notif => notif.UserId == userId).CountAsync();
        }
    }
}
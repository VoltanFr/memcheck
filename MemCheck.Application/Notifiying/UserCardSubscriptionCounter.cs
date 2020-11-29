using MemCheck.Database;
using System.Linq;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System;

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
        #endregion
        public UserCardSubscriptionCounter(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task<int> RunAsync(Guid userId)
        {
            return await dbContext.CardNotifications.Where(notif => notif.UserId == userId).CountAsync();
        }
    }
}
using MemCheck.Database;
using System.Linq;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace MemCheck.Application.Notifying
{
    internal interface IUserCardSubscriptionCounter
    {
        public Task<int> RunAsync(MemCheckUser user);
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
        public async Task<int> RunAsync(MemCheckUser user)
        {
            return await dbContext.CardNotifications.Where(notif => notif.UserId == user.Id).CountAsync();
        }
    }
}
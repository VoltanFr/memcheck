using MemCheck.Database;
using MemCheck.Domain;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Notifying
{
    internal interface IUserLastNotifDateUpdater
    {
        Task RunAsync(Guid userId, DateTime lastNotifUtcDate);
    }
    internal sealed class UserLastNotifDateUpdater : IUserLastNotifDateUpdater
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public UserLastNotifDateUpdater(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task RunAsync(Guid userId, DateTime lastNotifUtcDate)
        {
            dbContext.Users.Single(u => u.Id == userId).LastNotificationUtcDate = lastNotifUtcDate;
            await dbContext.SaveChangesAsync();
        }
    }
}
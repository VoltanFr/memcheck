using MemCheck.Database;
using MemCheck.Domain;
using System;
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
        private readonly DateTime runningUtcDate;
        #endregion
        public UserLastNotifDateUpdater(MemCheckDbContext dbContext, DateTime runningUtcDate)
        {
            this.dbContext = dbContext;
            this.runningUtcDate = runningUtcDate;
        }
        public async Task RunAsync(Guid userId)
        {
            dbContext.Users.Single(u => u.Id == userId).LastNotificationUtcDate = runningUtcDate;
            await dbContext.SaveChangesAsync();
        }
    }
}
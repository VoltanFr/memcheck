using MemCheck.Database;
using System.Linq;
using System.Collections.Immutable;
using MemCheck.Domain;
using System;
using Microsoft.EntityFrameworkCore;

namespace MemCheck.Application.Notifying
{
    //The information reported by this class is only based on the subscriptions and the last notification date
    //ie, it does not take care of whether or not there are actually changes to notify
    internal interface IUsersToNotifyGetter
    {
        public ImmutableArray<MemCheckUser> Run(DateTime? now = null);
    }
    internal sealed class UsersToNotifyGetter : IUsersToNotifyGetter
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public UsersToNotifyGetter(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public ImmutableArray<MemCheckUser> Run(DateTime? now = null)
        {
            now = now ?? DateTime.UtcNow;
            var userList = dbContext.Users.Where(user => user.MinimumCountOfDaysBetweenNotifs > 0 && EF.Functions.DateDiffHour(user.LastNotificationUtcDate, now) >= user.MinimumCountOfDaysBetweenNotifs * 24);
            //Using DateDiffDay is not suitable because it counts the number of **day boundaries crossed** between the startDate and endDate
            return userList.ToImmutableArray();
        }
    }
}
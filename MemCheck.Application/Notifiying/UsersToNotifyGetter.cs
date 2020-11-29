using MemCheck.Database;
using System.Linq;
using System.Collections.Immutable;
using MemCheck.Domain;
using System;
using Microsoft.EntityFrameworkCore;

namespace MemCheck.Application.Notifying
{
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
            var userList = dbContext.Users.Where(user => user.MinimumCountOfDaysBetweenNotifs > 0 && EF.Functions.DateDiffDay(user.LastNotificationUtcDate, now) >= user.MinimumCountOfDaysBetweenNotifs);
            return userList.ToImmutableArray();
        }
    }
}
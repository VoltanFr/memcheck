using MemCheck.Database;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Immutable;
using MemCheck.Domain;

namespace MemCheck.Application.Notifying
{
    internal sealed class UsersToNotifyGetter
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public UsersToNotifyGetter(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public ImmutableArray<MemCheckUser> Run()
        {
            //Will be reviwed when we have the table of registration info
            var userList = dbContext.CardNotifications.Select(notif => notif.User).Distinct();
            return userList.ToImmutableArray();
        }
    }
}
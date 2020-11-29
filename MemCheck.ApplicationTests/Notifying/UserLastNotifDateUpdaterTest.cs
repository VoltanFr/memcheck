using Microsoft.VisualStudio.TestTools.UnitTesting;
using MemCheck.Database;
using MemCheck.Application.Notifying;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace MemCheck.Application.Tests.Notifying
{
    [TestClass()]
    public class UserLastNotifDateUpdaterTest
    {
        [TestMethod()]
        public async Task TestRun_UserWithoutSubscription()
        {
            var testDB = DbServices.GetEmptyTestDB(typeof(UserLastNotifDateUpdaterTest));

            var user = await UserHelper.CreateInDbAsync(testDB, 1, new DateTime(2040, 1, 1));
            var lastNotifDate = new DateTime(2040, 1, 2);

            using (var dbContext = new MemCheckDbContext(testDB))
                await new UserLastNotifDateUpdater(dbContext).RunAsync(user.Id, lastNotifDate);

            using (var dbContext = new MemCheckDbContext(testDB))
                Assert.AreEqual(lastNotifDate, dbContext.Users.First().LastNotificationUtcDate);
        }
    }
}
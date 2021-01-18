using MemCheck.Application.Notifying;
using MemCheck.Application.Tests.Helpers;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Tests.Notifying
{
    [TestClass()]
    public class UserLastNotifDateUpdaterTest
    {
        [TestMethod()]
        public async Task TestRun_UserWithoutSubscription()
        {
            var testDB = DbHelper.GetEmptyTestDB();

            var user = await UserHelper.CreateInDbAsync(testDB, 1, new DateTime(2040, 1, 1));
            var lastNotifDate = new DateTime(2040, 1, 2);

            using (var dbContext = new MemCheckDbContext(testDB))
                await new UserLastNotifDateUpdater(dbContext, lastNotifDate).RunAsync(user);

            using (var dbContext = new MemCheckDbContext(testDB))
                Assert.AreEqual(lastNotifDate, dbContext.Users.First().LastNotificationUtcDate);
        }
    }
}
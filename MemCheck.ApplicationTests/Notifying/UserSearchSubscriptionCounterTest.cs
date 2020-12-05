using Microsoft.VisualStudio.TestTools.UnitTesting;
using MemCheck.Database;
using System;
using MemCheck.Application.Notifying;
using System.Threading.Tasks;
using MemCheck.Application.Tests.Helpers;

namespace MemCheck.Application.Tests.Notifying
{
    [TestClass()]
    public class UserSearchSubscriptionCounterTest
    {
        [TestMethod()]
        public async Task TestWithoutUser()
        {
            using (var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB()))
                Assert.AreEqual(0, await new UserSearchSubscriptionCounter(dbContext).RunAsync(Guid.Empty));
        }
        [TestMethod()]
        public async Task TestUserWithoutSubscription()
        {
            var testDB = DbHelper.GetEmptyTestDB();

            var user = await UserHelper.CreateInDbAsync(testDB);

            using (var dbContext = new MemCheckDbContext(testDB))
                Assert.AreEqual(0, await new UserSearchSubscriptionCounter(dbContext).RunAsync(user));
        }
        [TestMethod()]
        public async Task TestUsersWithSubscriptions()
        {
            var testDB = DbHelper.GetEmptyTestDB();

            var user1 = await UserHelper.CreateInDbAsync(testDB);

            var user2 = await UserHelper.CreateInDbAsync(testDB);
            await SearchSubscriptionHelper.CreateAsync(testDB, user2);

            var user3 = await UserHelper.CreateInDbAsync(testDB);
            await SearchSubscriptionHelper.CreateAsync(testDB, user3);
            await SearchSubscriptionHelper.CreateAsync(testDB, user3);
            await SearchSubscriptionHelper.CreateAsync(testDB, user3);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var counter = new UserSearchSubscriptionCounter(dbContext);
                Assert.AreEqual(0, await counter.RunAsync(user1));
                Assert.AreEqual(1, await counter.RunAsync(user2));
                Assert.AreEqual(3, await counter.RunAsync(user3));
            }
        }
    }
}
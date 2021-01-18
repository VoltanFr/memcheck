using MemCheck.Application.Notifying;
using MemCheck.Application.Tests.Helpers;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace MemCheck.Application.Tests.Notifying
{
    [TestClass()]
    public class UserCardSubscriptionCounterTest
    {
        [TestMethod()]
        public async Task TestRun_UserWithoutSubscription()
        {
            var testDB = DbHelper.GetEmptyTestDB();

            var user = await UserHelper.CreateInDbAsync(testDB);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var count = await new UserCardSubscriptionCounter(dbContext).RunAsync(user);
                Assert.AreEqual(0, count);
            }
        }
        [TestMethod()]
        public async Task TestRun_UserWithOneSubscription()
        {
            var testDB = DbHelper.GetEmptyTestDB();

            var user = await UserHelper.CreateInDbAsync(testDB);
            var card = await CardHelper.CreateAsync(testDB, user);
            await CardSubscriptionHelper.CreateAsync(testDB, user, card.Id);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var count = await new UserCardSubscriptionCounter(dbContext).RunAsync(user);
                Assert.AreEqual(1, count);
            }
        }
        [TestMethod()]
        public async Task TestRun_UserWithSubscriptions()
        {
            var testDB = DbHelper.GetEmptyTestDB();

            var user = await UserHelper.CreateInDbAsync(testDB);
            await CardSubscriptionHelper.CreateAsync(testDB, user, (await CardHelper.CreateAsync(testDB, user)).Id);
            await CardSubscriptionHelper.CreateAsync(testDB, user, (await CardHelper.CreateAsync(testDB, user)).Id);
            await CardSubscriptionHelper.CreateAsync(testDB, user, (await CardHelper.CreateAsync(testDB, user)).Id);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var count = await new UserCardSubscriptionCounter(dbContext).RunAsync(user);
                Assert.AreEqual(3, count);
            }
        }
        [TestMethod()]
        public async Task TestRun_UsersWithSubscriptions()
        {
            var testDB = DbHelper.GetEmptyTestDB();

            var user1 = await UserHelper.CreateInDbAsync(testDB);

            var user2 = await UserHelper.CreateInDbAsync(testDB);
            await CardSubscriptionHelper.CreateAsync(testDB, user2, (await CardHelper.CreateAsync(testDB, user1)).Id);

            var user3 = await UserHelper.CreateInDbAsync(testDB);
            await CardSubscriptionHelper.CreateAsync(testDB, user3, (await CardHelper.CreateAsync(testDB, user1)).Id);
            await CardSubscriptionHelper.CreateAsync(testDB, user3, (await CardHelper.CreateAsync(testDB, user2)).Id);
            await CardSubscriptionHelper.CreateAsync(testDB, user3, (await CardHelper.CreateAsync(testDB, user3)).Id);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var counter = new UserCardSubscriptionCounter(dbContext);
                Assert.AreEqual(0, await counter.RunAsync(user1));
                Assert.AreEqual(1, await counter.RunAsync(user2));
                Assert.AreEqual(3, await counter.RunAsync(user3));
            }
        }
    }
}
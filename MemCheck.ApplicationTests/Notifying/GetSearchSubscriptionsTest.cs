using Microsoft.VisualStudio.TestTools.UnitTesting;
using MemCheck.Database;
using MemCheck.Application.Notifying;
using System.Threading.Tasks;
using MemCheck.Application.Tests.Helpers;
using System.Linq;
using System;

namespace MemCheck.Application.Tests.Notifying
{
    [TestClass()]
    public class GetSearchSubscriptionsTest
    {
        [TestMethod()]
        public async Task TestEmptyDb()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(testDB);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new GetSearchSubscriptions.Request(userId);
                var subscriptions = await new GetSearchSubscriptions(dbContext).RunAsync(request);
                Assert.IsTrue(!subscriptions.Any());
            }
        }
        [TestMethod()]
        public async Task TestSimplestSearch()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(testDB);
            await SearchSubscriptionHelper.CreateAsync(testDB, userId);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new GetSearchSubscriptions.Request(userId);
                var subscriptions = await new GetSearchSubscriptions(dbContext).RunAsync(request);
                Assert.AreEqual(1, subscriptions.Count());
            }
        }
        [TestMethod()]
        public async Task TestGetRightSearchesPerUser()
        {
            var testDB = DbHelper.GetEmptyTestDB();

            var user1Id = await UserHelper.CreateInDbAsync(testDB);
            await SearchSubscriptionHelper.CreateAsync(testDB, user1Id);
            await SearchSubscriptionHelper.CreateAsync(testDB, user1Id);

            var user2Id = await UserHelper.CreateInDbAsync(testDB);
            await SearchSubscriptionHelper.CreateAsync(testDB, user2Id);
            await SearchSubscriptionHelper.CreateAsync(testDB, user2Id);
            await SearchSubscriptionHelper.CreateAsync(testDB, user2Id);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var user1Request = new GetSearchSubscriptions.Request(user1Id);
                var user1Subscriptions = await new GetSearchSubscriptions(dbContext).RunAsync(user1Request);
                Assert.AreEqual(2, user1Subscriptions.Count());

                var user2Request = new GetSearchSubscriptions.Request(user2Id);
                var user2Subscriptions = await new GetSearchSubscriptions(dbContext).RunAsync(user2Request);
                Assert.AreEqual(3, user2Subscriptions.Count());
            }
        }
        [TestMethod()]
        public async Task TestRightData()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(testDB);
            var deckDescription = StringServices.RandomString();
            var deck = await DeckHelper.CreateAsync(testDB, userId, deckDescription);
            var name = StringServices.RandomString();
            var requiredText = StringServices.RandomString();
            var lastNotifDate = new DateTime(2032, 1, 8);
            var savedSubscription = await SearchSubscriptionHelper.CreateAsync(testDB, userId, name: name, requiredText: requiredText, excludedDeckId: deck, lastNotificationDate: lastNotifDate);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new GetSearchSubscriptions.Request(userId);
                var subscription = (await new GetSearchSubscriptions(dbContext).RunAsync(request)).Single();
                Assert.AreEqual(savedSubscription.Id, subscription.Id);
                Assert.AreEqual(name, subscription.Name);
                Assert.AreEqual(requiredText, subscription.RequiredText);
                Assert.AreEqual(deckDescription, subscription.ExcludedDeck);
                Assert.AreEqual(lastNotifDate, subscription.LastRunUtcDate);
                Assert.AreEqual(lastNotifDate, subscription.RegistrationUtcDate);
            }
        }
        [TestMethod()]
        public async Task TestCardCountOnLastRun()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(testDB);
            var subscription = await SearchSubscriptionHelper.CreateAsync(testDB, userId);

            using (var dbContext = new MemCheckDbContext(testDB))
                await new UserSearchNotifier(dbContext, 100, new DateTime(2050, 05, 01)).RunAsync(subscription.Id);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new GetSearchSubscriptions.Request(userId);
                var s = (await new GetSearchSubscriptions(dbContext).RunAsync(request)).Single();
                Assert.AreEqual(0, s.CardCountOnLastRun);
            }

            await CardHelper.CreateAsync(testDB, userId);
            await CardHelper.CreateAsync(testDB, userId);
            await CardHelper.CreateAsync(testDB, userId);
            await CardHelper.CreateAsync(testDB, userId);

            using (var dbContext = new MemCheckDbContext(testDB))
                await new UserSearchNotifier(dbContext, 100, new DateTime(2050, 05, 02)).RunAsync(subscription.Id);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new GetSearchSubscriptions.Request(userId);
                var s = (await new GetSearchSubscriptions(dbContext).RunAsync(request)).Single();
                Assert.AreEqual(4, s.CardCountOnLastRun);
            }
        }
    }
}

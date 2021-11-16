using MemCheck.Application.Notifying;
using MemCheck.Application.QueryValidation;
using MemCheck.Application.Tags;
using MemCheck.Application.Tests.Helpers;
using MemCheck.Basics;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Tests.Notifying
{
    [TestClass()]
    public class DeleteSearchSubscriptionTest
    {
        [TestMethod()]
        public async Task NoUser()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(testDB);
            var subscription = await SearchSubscriptionHelper.CreateAsync(testDB, userId);

            using var dbContext = new MemCheckDbContext(testDB);
            var request = new DeleteSearchSubscription.Request(Guid.Empty, subscription.Id);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new DeleteSearchSubscription(dbContext.AsCallContext()).RunAsync(request));
        }
        [TestMethod()]
        public async Task InvalidSubscriptionId()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(testDB);

            using var dbContext = new MemCheckDbContext(testDB);
            var request = new DeleteSearchSubscription.Request(userId, Guid.Empty);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new DeleteSearchSubscription(dbContext.AsCallContext()).RunAsync(request));
        }
        [TestMethod()]
        public async Task UserNotOwnerOfSubscription()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var subscriptionOwnerId = await UserHelper.CreateInDbAsync(testDB);
            var subscription = await SearchSubscriptionHelper.CreateAsync(testDB, subscriptionOwnerId);
            var userId = await UserHelper.CreateInDbAsync(testDB);

            using var dbContext = new MemCheckDbContext(testDB);
            var request = new DeleteSearchSubscription.Request(userId, subscription.Id);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new DeleteSearchSubscription(dbContext.AsCallContext()).RunAsync(request));
        }
        [TestMethod()]
        public async Task CorrectDeletion_OnlySubscription()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(testDB);
            var subscription = await SearchSubscriptionHelper.CreateAsync(testDB, userId);

            using (var dbContext = new MemCheckDbContext(testDB))
                await new DeleteSearchSubscription(dbContext.AsCallContext()).RunAsync(new DeleteSearchSubscription.Request(userId, subscription.Id));

            using (var dbContext = new MemCheckDbContext(testDB))
                Assert.AreEqual(0, (await new GetSearchSubscriptions(dbContext.AsCallContext()).RunAsync(new GetSearchSubscriptions.Request(userId))).Count());
        }
        [TestMethod()]
        public async Task CorrectDeletion_OtherSubscriptionsExist()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(testDB);
            await SearchSubscriptionHelper.CreateAsync(testDB, userId);
            var subscription = await SearchSubscriptionHelper.CreateAsync(testDB, userId);
            await SearchSubscriptionHelper.CreateAsync(testDB, userId);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new DeleteSearchSubscription.Request(userId, subscription.Id);
                await new DeleteSearchSubscription(dbContext.AsCallContext()).RunAsync(request);
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new GetSearchSubscriptions.Request(userId);
                var subscriptions = await new GetSearchSubscriptions(dbContext.AsCallContext()).RunAsync(request);
                Assert.AreEqual(2, subscriptions.Count());
                Assert.IsFalse(subscriptions.Any(s => s.Id == subscription.Id));
            }
        }
        [TestMethod()]
        public async Task CascadeDeletion()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);

            Guid tagId1;
            using (var dbContext = new MemCheckDbContext(db))
                tagId1 = await new CreateTag(dbContext).RunAsync(new CreateTag.Request(user, RandomHelper.String(), ""), new TestLocalizer());

            await CardHelper.CreateAsync(db, user, tagIds: tagId1.AsArray());
            await CardHelper.CreateAsync(db, user);
            await CardHelper.CreateAsync(db, user, tagIds: tagId1.AsArray());

            Guid subscriptionId;
            using (var dbContext = new MemCheckDbContext(db))
            {
                var tagId2 = await new CreateTag(dbContext).RunAsync(new CreateTag.Request(user, RandomHelper.String(), ""), new TestLocalizer());
                var tagId3 = await new CreateTag(dbContext).RunAsync(new CreateTag.Request(user, RandomHelper.String(), ""), new TestLocalizer());
                var request = new SubscribeToSearch.Request(user, Guid.Empty, RandomHelper.String(), "", tagId1.AsArray(), new[] { tagId2, tagId3 });
                subscriptionId = await new SubscribeToSearch(dbContext).RunAsync(request);
            }

            using (var dbContext = new MemCheckDbContext(db))
                await new UserSearchNotifier(dbContext, 10, new DateTime(2050, 05, 01)).RunAsync(subscriptionId);

            using (var dbContext = new MemCheckDbContext(db))
            {
                Assert.AreEqual(2, dbContext.CardsInSearchResults.Count(c => c.SearchSubscriptionId == subscriptionId));
                Assert.AreEqual(1, dbContext.RequiredTagInSearchSubscriptions.Count(c => c.SearchSubscriptionId == subscriptionId));
                Assert.AreEqual(2, dbContext.ExcludedTagInSearchSubscriptions.Count(c => c.SearchSubscriptionId == subscriptionId));
            }

            using (var dbContext = new MemCheckDbContext(db))
                await new DeleteSearchSubscription(dbContext.AsCallContext()).RunAsync(new DeleteSearchSubscription.Request(user, subscriptionId));

            using (var dbContext = new MemCheckDbContext(db))
            {
                Assert.AreEqual(0, dbContext.CardsInSearchResults.Count(c => c.SearchSubscriptionId == subscriptionId));
                Assert.AreEqual(0, dbContext.RequiredTagInSearchSubscriptions.Count(c => c.SearchSubscriptionId == subscriptionId));
                Assert.AreEqual(0, dbContext.ExcludedTagInSearchSubscriptions.Count(c => c.SearchSubscriptionId == subscriptionId));
            }
        }
    }
}

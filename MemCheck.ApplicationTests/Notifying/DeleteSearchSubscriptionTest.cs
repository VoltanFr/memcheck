﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using MemCheck.Database;
using MemCheck.Application.Notifying;
using System.Threading.Tasks;
using MemCheck.Application.Tests.Helpers;
using System.Linq;
using System;
using MemCheck.Application.QueryValidation;

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

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new DeleteSearchSubscription.Request(Guid.Empty, subscription.Id);
                await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new DeleteSearchSubscription(dbContext).RunAsync(request));
            }
        }
        [TestMethod()]
        public async Task InvalidSubscriptionId()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(testDB);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new DeleteSearchSubscription.Request(userId, Guid.Empty);
                await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new DeleteSearchSubscription(dbContext).RunAsync(request));
            }
        }
        [TestMethod()]
        public async Task UserNotOwnerOfSubscription()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var subscriptionOwnerId = await UserHelper.CreateInDbAsync(testDB);
            var subscription = await SearchSubscriptionHelper.CreateAsync(testDB, subscriptionOwnerId);
            var userId = await UserHelper.CreateInDbAsync(testDB);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new DeleteSearchSubscription.Request(userId, subscription.Id);
                await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new DeleteSearchSubscription(dbContext).RunAsync(request));
            }
        }
        [TestMethod()]
        public async Task CorrectDeletion_OnlySubscription()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(testDB);
            var subscription = await SearchSubscriptionHelper.CreateAsync(testDB, userId);

            using (var dbContext = new MemCheckDbContext(testDB))
                await new DeleteSearchSubscription(dbContext).RunAsync(new DeleteSearchSubscription.Request(userId, subscription.Id));

            using (var dbContext = new MemCheckDbContext(testDB))
                Assert.AreEqual(0, (await new GetSearchSubscriptions(dbContext).RunAsync(new GetSearchSubscriptions.Request(userId))).Count());
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
                await new DeleteSearchSubscription(dbContext).RunAsync(request);
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new GetSearchSubscriptions.Request(userId);
                var subscriptions = await new GetSearchSubscriptions(dbContext).RunAsync(request);
                Assert.AreEqual(2, subscriptions.Count());
                Assert.IsFalse(subscriptions.Any(s => s.Id == subscription.Id));
            }
        }
        [TestMethod()]
        public async Task CascadeDeletion()
        {
            var db = DbHelper.GetEmptyTestDB();

            Guid tagId1;
            using (var dbContext = new MemCheckDbContext(db))
                tagId1 = await new CreateTag(dbContext).RunAsync(StringServices.RandomString());

            var user = await UserHelper.CreateInDbAsync(db);
            await CardHelper.CreateAsync(db, user, tagIds: new[] { tagId1 });
            await CardHelper.CreateAsync(db, user);
            await CardHelper.CreateAsync(db, user, tagIds: new[] { tagId1 });

            Guid subscriptionId;
            using (var dbContext = new MemCheckDbContext(db))
            {
                var tagId2 = await new CreateTag(dbContext).RunAsync(StringServices.RandomString());
                var tagId3 = await new CreateTag(dbContext).RunAsync(StringServices.RandomString());
                var request = new SubscribeToSearch.Request(user, Guid.Empty, StringServices.RandomString(), "", new[] { tagId1 }, new[] { tagId2, tagId3 });
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
                await new DeleteSearchSubscription(dbContext).RunAsync(new DeleteSearchSubscription.Request(user, subscriptionId));

            using (var dbContext = new MemCheckDbContext(db))
            {
                Assert.AreEqual(0, dbContext.CardsInSearchResults.Count(c => c.SearchSubscriptionId == subscriptionId));
                Assert.AreEqual(0, dbContext.RequiredTagInSearchSubscriptions.Count(c => c.SearchSubscriptionId == subscriptionId));
                Assert.AreEqual(0, dbContext.ExcludedTagInSearchSubscriptions.Count(c => c.SearchSubscriptionId == subscriptionId));
            }
        }
    }
}

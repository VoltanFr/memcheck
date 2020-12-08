using Microsoft.VisualStudio.TestTools.UnitTesting;
using MemCheck.Database;
using System.Linq;
using MemCheck.Application.Notifying;
using System.Threading.Tasks;
using MemCheck.Application.Tests.Helpers;
using System;
using MemCheck.Application.CardChanging;
using MemCheck.Application.Tests.BasicHelpers;
using System.Collections.Generic;
using MemCheck.Application.Searching;

namespace MemCheck.Application.Tests.Notifying
{
    [TestClass()]
    public class UserSearchNotifierTests
    {
        [TestMethod()]
        public async Task NothingToReportBecauseDbEmpty()
        {
            var db = DbHelper.GetEmptyTestDB();

            var user = await UserHelper.CreateInDbAsync(db);
            var subscription = await SearchSubscriptionHelper.CreateAsync(db, user, lastNotificationDate: new DateTime(2050, 04, 01));
            var runDate = new DateTime(2050, 05, 01);

            using (var dbContext = new MemCheckDbContext(db))
            {
                var searchResult = await new UserSearchNotifier(dbContext, 100, runDate).RunAsync(subscription.Id);
                Assert.AreEqual(0, searchResult.TotalNewlyFoundCardCount);
                Assert.AreEqual(0, searchResult.TotalNotFoundAnymoreCardCount);
            }

            using (var dbContext = new MemCheckDbContext(db))
                Assert.AreEqual(runDate, dbContext.SearchSubscriptions.Single(s => s.Id == subscription.Id).LastRunUtcDate);
        }
        [TestMethod()]
        public async Task CardToReportBecauseFirstRun()
        {
            var db = DbHelper.GetEmptyTestDB();

            var user = await UserHelper.CreateInDbAsync(db);
            var card = await CardHelper.CreateAsync(db, user, versionDate: new DateTime(2050, 03, 01), userWithViewIds: new[] { user });
            var subscription = await SearchSubscriptionHelper.CreateAsync(db, user);
            var runDate = new DateTime(2050, 05, 01);

            using (var dbContext = new MemCheckDbContext(db))
            {
                var searchResult = await new UserSearchNotifier(dbContext, 100, runDate).RunAsync(subscription.Id);
                Assert.AreEqual(1, searchResult.TotalNewlyFoundCardCount);
                Assert.AreEqual(0, searchResult.TotalNotFoundAnymoreCardCount);
            }

            using (var dbContext = new MemCheckDbContext(db))
                Assert.AreEqual(runDate, dbContext.SearchSubscriptions.Single(s => s.Id == subscription.Id).LastRunUtcDate);
        }
        [TestMethod()]
        public async Task NothingToReportBecauseCardWasAlreadyInPreviousSearch()
        {
            var db = DbHelper.GetEmptyTestDB();

            var user = await UserHelper.CreateInDbAsync(db);
            var language = await CardLanguagHelper.CreateAsync(db);
            await CardHelper.CreateAsync(db, user, versionDate: new DateTime(2050, 03, 01), language: language);
            var card2 = await CardHelper.CreateAsync(db, user, versionDate: new DateTime(2050, 04, 02), language: language);

            var subscription = await SearchSubscriptionHelper.CreateAsync(db, user, lastNotificationDate: new DateTime(2050, 04, 01));

            using (var dbContext = new MemCheckDbContext(db))
                await new UserSearchNotifier(dbContext, 100, new DateTime(2050, 05, 01)).RunAsync(subscription.Id);

            using (var dbContext = new MemCheckDbContext(db))
            {
                var updateRequest = new UpdateCard.Request(card2.Id, user, StringServices.RandomString(), new Guid[0], StringServices.RandomString(), new Guid[0], StringServices.RandomString(), new Guid[0], language, new Guid[0], new Guid[0], StringServices.RandomString());
                await new UpdateCard(dbContext).RunAsync(updateRequest, new TestLocalizer(), new DateTime(2050, 05, 02));
            }

            var runDate = new DateTime(2050, 05, 03);

            using (var dbContext = new MemCheckDbContext(db))
            {
                var searchResult = await new UserSearchNotifier(dbContext, 100, runDate).RunAsync(subscription.Id);
                Assert.AreEqual(0, searchResult.TotalNewlyFoundCardCount);
                Assert.AreEqual(0, searchResult.TotalNotFoundAnymoreCardCount);
            }

            using (var dbContext = new MemCheckDbContext(db))
                Assert.AreEqual(runDate, dbContext.SearchSubscriptions.Single(s => s.Id == subscription.Id).LastRunUtcDate);
        }
        [TestMethod()]
        public async Task NotFoundAnymoreToReport()
        {
            var db = DbHelper.GetEmptyTestDB();

            var user = await UserHelper.CreateInDbAsync(db);
            var language = await CardLanguagHelper.CreateAsync(db);
            var card1 = await CardHelper.CreateAsync(db, user, versionDate: new DateTime(2050, 03, 01), language: language);
            var card2 = await CardHelper.CreateAsync(db, user, versionDate: new DateTime(2050, 04, 02), language: language);

            var subscription = await SearchSubscriptionHelper.CreateAsync(db, user, requiredText: card1.FrontSide);

            using (var dbContext = new MemCheckDbContext(db))
            {
                var searchResult = await new UserSearchNotifier(dbContext, 100, new DateTime(2050, 05, 01)).RunAsync(subscription.Id);
                Assert.AreEqual(1, searchResult.TotalNewlyFoundCardCount);
                Assert.AreEqual(0, searchResult.TotalNotFoundAnymoreCardCount);
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var updateRequest = new UpdateCard.Request(card1.Id, user, StringServices.RandomString(), new Guid[0], StringServices.RandomString(), new Guid[0], StringServices.RandomString(), new Guid[0], language, new Guid[0], new Guid[0], StringServices.RandomString());
                await new UpdateCard(dbContext).RunAsync(updateRequest, new TestLocalizer(), new DateTime(2050, 05, 02));
            }

            var runDate = new DateTime(2050, 05, 03);

            using (var dbContext = new MemCheckDbContext(db))
            {
                var searchResult = await new UserSearchNotifier(dbContext, 100, runDate).RunAsync(subscription.Id);
                Assert.AreEqual(0, searchResult.TotalNewlyFoundCardCount);
                Assert.AreEqual(1, searchResult.TotalNotFoundAnymoreCardCount);
                Assert.AreEqual(card1.Id, searchResult.CardsNotFoundAnymore.First());
            }

            using (var dbContext = new MemCheckDbContext(db))
                Assert.AreEqual(runDate, dbContext.SearchSubscriptions.Single(s => s.Id == subscription.Id).LastRunUtcDate);
        }
        [TestMethod()]
        public async Task NewlyFoundToReport()
        {
            var db = DbHelper.GetEmptyTestDB();

            var user = await UserHelper.CreateInDbAsync(db);
            var language = await CardLanguagHelper.CreateAsync(db);
            var card1 = await CardHelper.CreateAsync(db, user, versionDate: new DateTime(2050, 03, 01), language: language);
            var card2 = await CardHelper.CreateAsync(db, user, versionDate: new DateTime(2050, 04, 02), language: language);

            var someText = StringServices.RandomString();
            var subscription = await SearchSubscriptionHelper.CreateAsync(db, user, requiredText: someText);

            using (var dbContext = new MemCheckDbContext(db))
            {
                var searchResult = await new UserSearchNotifier(dbContext, 10, new DateTime(2050, 05, 01)).RunAsync(subscription.Id);
                Assert.AreEqual(0, searchResult.TotalNewlyFoundCardCount);
                Assert.AreEqual(0, searchResult.TotalNotFoundAnymoreCardCount);
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var updateRequest = new UpdateCard.Request(card1.Id, user, someText, new Guid[0], StringServices.RandomString(), new Guid[0], StringServices.RandomString(), new Guid[0], language, new Guid[0], new Guid[0], StringServices.RandomString());
                await new UpdateCard(dbContext).RunAsync(updateRequest, new TestLocalizer(), new DateTime(2050, 05, 02));
            }

            var runDate = new DateTime(2050, 05, 03);

            using (var dbContext = new MemCheckDbContext(db))
            {
                var searchResult = await new UserSearchNotifier(dbContext, 100, runDate).RunAsync(subscription.Id);
                Assert.AreEqual(1, searchResult.TotalNewlyFoundCardCount);
                Assert.AreEqual(0, searchResult.TotalNotFoundAnymoreCardCount);
                Assert.AreEqual(card1.Id, searchResult.NewlyFoundCards.First().CardId);
            }

            using (var dbContext = new MemCheckDbContext(db))
                Assert.AreEqual(runDate, dbContext.SearchSubscriptions.Single(s => s.Id == subscription.Id).LastRunUtcDate);
        }
        [TestMethod()]
        public async Task NewToReportAndOtherWithoutChange()
        {
            var db = DbHelper.GetEmptyTestDB();

            var user = await UserHelper.CreateInDbAsync(db);
            var language = await CardLanguagHelper.CreateAsync(db);
            var requiredTag = await TagHelper.CreateAsync(db);
            var excludedTag = await TagHelper.CreateAsync(db);

            var card1 = await CardHelper.CreateAsync(db, user, language: language, tagIds: new[] { requiredTag, excludedTag });
            var card2 = await CardHelper.CreateAsync(db, user, language: language, tagIds: new[] { requiredTag });

            Guid subscriptionId;
            using (var dbContext = new MemCheckDbContext(db))
            {
                var subscriberRequest = new SubscribeToSearch.Request(user, Guid.Empty, "", new[] { requiredTag }, new[] { excludedTag });
                var subscriber = new SubscribeToSearch(dbContext);
                subscriptionId = await subscriber.RunAsync(subscriberRequest);
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var searchResult = await new UserSearchNotifier(dbContext, 12, new DateTime(2050, 05, 01)).RunAsync(subscriptionId);
                Assert.AreEqual(1, searchResult.TotalNewlyFoundCardCount);
                Assert.AreEqual(card2.Id, searchResult.NewlyFoundCards.First().CardId);
                Assert.AreEqual(0, searchResult.TotalNotFoundAnymoreCardCount);
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var updateDate = new DateTime(2050, 05, 02);
                await new UpdateCard(dbContext).RunAsync(UpdateCardHelper.RequestForTagChanges(card1, new[] { requiredTag }), new TestLocalizer(), updateDate);
            }

            var runDate = new DateTime(2050, 05, 03);

            using (var dbContext = new MemCheckDbContext(db))
            {
                var searchResult = await new UserSearchNotifier(dbContext, 13, runDate).RunAsync(subscriptionId);
                Assert.AreEqual(1, searchResult.TotalNewlyFoundCardCount);
                Assert.AreEqual(0, searchResult.TotalNotFoundAnymoreCardCount);
                Assert.AreEqual(card1.Id, searchResult.NewlyFoundCards.First().CardId);
            }

            using (var dbContext = new MemCheckDbContext(db))
                Assert.AreEqual(runDate, dbContext.SearchSubscriptions.Single(s => s.Id == subscriptionId).LastRunUtcDate);
        }
        [TestMethod()]
        public async Task NewAndNotAnymoreToReport()
        {
            var db = DbHelper.GetEmptyTestDB();

            var user = await UserHelper.CreateInDbAsync(db);
            var language = await CardLanguagHelper.CreateAsync(db);
            var ignoredTag = await TagHelper.CreateAsync(db);
            var requiredTag1 = await TagHelper.CreateAsync(db);
            var requiredTag2 = await TagHelper.CreateAsync(db);
            var excludedTag = await TagHelper.CreateAsync(db);

            var card1 = await CardHelper.CreateAsync(db, user, language: language, tagIds: new[] { ignoredTag, requiredTag1, requiredTag2, excludedTag });
            var card2 = await CardHelper.CreateAsync(db, user, language: language, tagIds: new[] { ignoredTag, requiredTag1, requiredTag2 });
            var card3 = await CardHelper.CreateAsync(db, user, language: language, tagIds: new[] { ignoredTag });
            var card4 = await CardHelper.CreateAsync(db, user, language: language, tagIds: new[] { ignoredTag });

            Guid subscriptionId;
            using (var dbContext = new MemCheckDbContext(db))
            {
                var subscriberRequest = new SubscribeToSearch.Request(user, Guid.Empty, "", new[] { requiredTag1, requiredTag2 }, new[] { excludedTag });
                subscriptionId = await new SubscribeToSearch(dbContext).RunAsync(subscriberRequest);
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var searchResult = await new UserSearchNotifier(dbContext, 10, new DateTime(2050, 05, 01)).RunAsync(subscriptionId);
                Assert.AreEqual(1, searchResult.TotalNewlyFoundCardCount);
                Assert.AreEqual(card2.Id, searchResult.NewlyFoundCards.First().CardId);
                Assert.AreEqual(0, searchResult.TotalNotFoundAnymoreCardCount);
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var updateDate = new DateTime(2050, 05, 02);
                await new UpdateCard(dbContext).RunAsync(UpdateCardHelper.RequestForTagChanges(card1, new[] { ignoredTag, requiredTag1, requiredTag2 }), new TestLocalizer(), updateDate);
                await new UpdateCard(dbContext).RunAsync(UpdateCardHelper.RequestForTagChanges(card2, new[] { requiredTag1, requiredTag2, excludedTag }), new TestLocalizer(), updateDate);
                await new UpdateCard(dbContext).RunAsync(UpdateCardHelper.RequestForTagChanges(card3, new[] { requiredTag1 }), new TestLocalizer(), updateDate);
                await new UpdateCard(dbContext).RunAsync(UpdateCardHelper.RequestForTagChanges(card3, new[] { requiredTag1, requiredTag2 }), new TestLocalizer(), new DateTime(2050, 05, 03));
                await new UpdateCard(dbContext).RunAsync(UpdateCardHelper.RequestForTagChanges(card4, new[] { requiredTag1 }), new TestLocalizer(), updateDate);
            }

            var runDate = new DateTime(2050, 05, 04);

            using (var dbContext = new MemCheckDbContext(db))
            {
                var searchResult = await new UserSearchNotifier(dbContext, 55, runDate).RunAsync(subscriptionId);
                Assert.AreEqual(2, searchResult.TotalNewlyFoundCardCount);
                Assert.AreEqual(1, searchResult.TotalNotFoundAnymoreCardCount);
                Assert.IsTrue(searchResult.NewlyFoundCards.Any(c => c.CardId == card1.Id));
                Assert.IsTrue(searchResult.NewlyFoundCards.Any(c => c.CardId == card3.Id));
                Assert.AreEqual(card2.Id, searchResult.CardsNotFoundAnymore.First());
            }

            using (var dbContext = new MemCheckDbContext(db))
                Assert.AreEqual(runDate, dbContext.SearchSubscriptions.Single(s => s.Id == subscriptionId).LastRunUtcDate);
        }
        [TestMethod()]
        public async Task MoreNewFoundThanMaxReported()
        {
            var db = DbHelper.GetEmptyTestDB();

            var user = await UserHelper.CreateInDbAsync(db);
            var language = await CardLanguagHelper.CreateAsync(db);

            for (int i = 0; i < SearchCards.Request.MaxPageSize * 2; i++)
                await CardHelper.CreateAsync(db, user, language: language);
            var card = await CardHelper.CreateAsync(db, user, language: language);

            Guid subscriptionId;
            using (var dbContext = new MemCheckDbContext(db))
            {
                var subscriberRequest = new SubscribeToSearch.Request(user, Guid.Empty, "", new Guid[0], new Guid[0]);
                subscriptionId = await new SubscribeToSearch(dbContext).RunAsync(subscriberRequest);
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var searchResult = await new UserSearchNotifier(dbContext, 2, new DateTime(2050, 05, 01)).RunAsync(subscriptionId);
                Assert.AreEqual(SearchCards.Request.MaxPageSize * 2 + 1, searchResult.TotalNewlyFoundCardCount);
                Assert.AreEqual(2, searchResult.NewlyFoundCards.Length);
                Assert.AreEqual(0, searchResult.TotalNotFoundAnymoreCardCount);
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var deleter = new DeleteCards(dbContext, new TestLocalizer());
                await deleter.RunAsync(new DeleteCards.Request(user, new[] { card.Id }));
            }

            var runDate = new DateTime(2050, 05, 04);

            using (var dbContext = new MemCheckDbContext(db))
            {
                var searchResult = await new UserSearchNotifier(dbContext, 2, runDate).RunAsync(subscriptionId);
                Assert.AreEqual(0, searchResult.TotalNewlyFoundCardCount);
                Assert.AreEqual(0, searchResult.NewlyFoundCards.Length);
                Assert.AreEqual(1, searchResult.TotalNotFoundAnymoreCardCount);
                Assert.AreEqual(card.Id, searchResult.CardsNotFoundAnymore.First());
            }

            using (var dbContext = new MemCheckDbContext(db))
                Assert.AreEqual(runDate, dbContext.SearchSubscriptions.Single(s => s.Id == subscriptionId).LastRunUtcDate);
        }
    }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using MemCheck.Database;
using MemCheck.Application.Notifying;
using System.Threading.Tasks;
using System;
using Microsoft.EntityFrameworkCore;
using MemCheck.Application.Tests.Helpers;
using System.Linq;
using MemCheck.Application.Heaping;
using MemCheck.Application.QueryValidation;

namespace MemCheck.Application.Tests.Notifying
{
    [TestClass()]
    public class SubscribeToSearchTest
    {
        [TestMethod()]
        public async Task TestSearchEverything()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(testDB);
            var subscriptionName = StringServices.RandomString();

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new SubscribeToSearch.Request(userId, Guid.Empty, subscriptionName, "", new Guid[0], new Guid[0]);
                await new SubscribeToSearch(dbContext).RunAsync(request);
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var subscription = await dbContext.SearchSubscriptions
                    .Include(subscription => subscription.ExcludedTags)
                    .Include(subscription => subscription.RequiredTags)
                    .SingleAsync();
                Assert.AreEqual(userId, subscription.UserId);
                Assert.AreEqual(subscriptionName, subscription.Name);
                Assert.AreEqual(Guid.Empty, subscription.ExcludedDeck);
                Assert.AreEqual("", subscription.RequiredText);
                Assert.AreEqual(0, subscription.RequiredTags.Count());
                Assert.IsFalse(subscription.ExcludeAllTags);
                Assert.AreEqual(0, subscription.ExcludedTags!.Count());
            }
        }
        [TestMethod()]
        public async Task TestSearchExcludingDeck()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(testDB);
            Guid deckId;

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var user = await dbContext.Users.SingleAsync();
                deckId = await new CreateDeck(dbContext).RunAsync(new CreateDeck.Request(user, StringServices.RandomString(), HeapingAlgorithms.DefaultAlgoId));
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new SubscribeToSearch.Request(userId, deckId, StringServices.RandomString(), "", new Guid[0], new Guid[0]);
                await new SubscribeToSearch(dbContext).RunAsync(request);
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var subscription = await dbContext.SearchSubscriptions
                    .Include(subscription => subscription.ExcludedTags)
                    .Include(subscription => subscription.RequiredTags)
                    .SingleAsync();
                Assert.AreEqual(userId, subscription.UserId);
                Assert.AreEqual(deckId, subscription.ExcludedDeck);
                Assert.AreEqual("", subscription.RequiredText);
                Assert.AreEqual(0, subscription.RequiredTags.Count());
                Assert.IsFalse(subscription.ExcludeAllTags);
                Assert.AreEqual(0, subscription.ExcludedTags!.Count());
            }
        }
        [TestMethod()]
        public async Task TestSearchText()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(testDB);
            var text = StringServices.RandomString();
            var subscriptionName = StringServices.RandomString();

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new SubscribeToSearch.Request(userId, Guid.Empty, subscriptionName, text, new Guid[0], new Guid[0]);
                await new SubscribeToSearch(dbContext).RunAsync(request);
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var subscription = await dbContext.SearchSubscriptions
                    .Include(subscription => subscription.ExcludedTags)
                    .Include(subscription => subscription.RequiredTags)
                    .SingleAsync();
                Assert.AreEqual(userId, subscription.UserId);
                Assert.AreEqual(subscriptionName, subscription.Name);
                Assert.AreEqual(Guid.Empty, subscription.ExcludedDeck);
                Assert.AreEqual(text, subscription.RequiredText);
                Assert.AreEqual(0, subscription.RequiredTags.Count());
                Assert.IsFalse(subscription.ExcludeAllTags);
                Assert.AreEqual(0, subscription.ExcludedTags!.Count());
            }
        }
        [TestMethod()]
        public async Task TestSearchWithTag()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(testDB);
            Guid tagId;

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var user = await dbContext.Users.SingleAsync();
                tagId = await new CreateTag(dbContext).RunAsync(StringServices.RandomString());
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new SubscribeToSearch.Request(userId, Guid.Empty, StringServices.RandomString(), "", new Guid[] { tagId }, new Guid[0]);
                await new SubscribeToSearch(dbContext).RunAsync(request);
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var subscription = await dbContext.SearchSubscriptions
                    .Include(subscription => subscription.ExcludedTags)
                    .Include(subscription => subscription.RequiredTags)
                    .SingleAsync();
                Assert.AreEqual(userId, subscription.UserId);
                Assert.AreEqual(Guid.Empty, subscription.ExcludedDeck);
                Assert.AreEqual("", subscription.RequiredText);
                Assert.AreEqual(tagId, subscription.RequiredTags.First().TagId);
                Assert.IsFalse(subscription.ExcludeAllTags);
                Assert.AreEqual(0, subscription.ExcludedTags!.Count());
            }
        }
        [TestMethod()]
        public async Task TestSearchWithoutTag()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(testDB);
            Guid tagId;

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var user = await dbContext.Users.SingleAsync();
                tagId = await new CreateTag(dbContext).RunAsync(StringServices.RandomString());
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new SubscribeToSearch.Request(userId, Guid.Empty, StringServices.RandomString(), "", new Guid[0], new Guid[] { tagId });
                await new SubscribeToSearch(dbContext).RunAsync(request);
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var subscription = await dbContext.SearchSubscriptions
                    .Include(subscription => subscription.ExcludedTags)
                    .Include(subscription => subscription.RequiredTags)
                    .SingleAsync();
                Assert.AreEqual(userId, subscription.UserId);
                Assert.AreEqual(Guid.Empty, subscription.ExcludedDeck);
                Assert.AreEqual("", subscription.RequiredText);
                Assert.AreEqual(0, subscription.RequiredTags.Count());
                Assert.IsFalse(subscription.ExcludeAllTags);
                Assert.AreEqual(tagId, subscription.ExcludedTags.First().TagId);
            }
        }
        [TestMethod()]
        public async Task TestSearchWithoutAnyTag()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(testDB);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new SubscribeToSearch.Request(userId, Guid.Empty, StringServices.RandomString(), "", new Guid[0], null);
                await new SubscribeToSearch(dbContext).RunAsync(request);
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var subscription = await dbContext.SearchSubscriptions
                    .Include(subscription => subscription.ExcludedTags)
                    .Include(subscription => subscription.RequiredTags)
                    .SingleAsync();
                Assert.AreEqual(userId, subscription.UserId);
                Assert.AreEqual(Guid.Empty, subscription.ExcludedDeck);
                Assert.AreEqual("", subscription.RequiredText);
                Assert.AreEqual(0, subscription.RequiredTags.Count());
                Assert.IsTrue(subscription.ExcludeAllTags);
            }
        }
        [TestMethod()]
        public async Task TestBadDeck()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(testDB);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new SubscribeToSearch.Request(userId, Guid.NewGuid(), StringServices.RandomString(), "", new Guid[0], new Guid[0]);
                await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new SubscribeToSearch(dbContext).RunAsync(request));
            }
        }
        [TestMethod()]
        public async Task TestBadRequiredTag()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(testDB);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new SubscribeToSearch.Request(userId, Guid.Empty, StringServices.RandomString(), "", new Guid[] { Guid.NewGuid() }, new Guid[0]);
                await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new SubscribeToSearch(dbContext).RunAsync(request));
            }
        }
        [TestMethod()]
        public async Task TestBadExcludedTag()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(testDB);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new SubscribeToSearch.Request(userId, Guid.Empty, StringServices.RandomString(), "", new Guid[0], new Guid[] { Guid.NewGuid() });
                await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new SubscribeToSearch(dbContext).RunAsync(request));
            }
        }
        [TestMethod()]
        public async Task TestSearchWithSameTagTwice()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(testDB);
            Guid tagId;

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var user = await dbContext.Users.SingleAsync();
                tagId = await new CreateTag(dbContext).RunAsync(StringServices.RandomString());
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new SubscribeToSearch.Request(userId, Guid.Empty, StringServices.RandomString(), "", new Guid[] { tagId, tagId }, new Guid[0]);
                await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new SubscribeToSearch(dbContext).RunAsync(request));
            }
        }
        [TestMethod()]
        public async Task TestSearchWithSameExcludedTagTwice()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(testDB);
            Guid tagId;

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var user = await dbContext.Users.SingleAsync();
                tagId = await new CreateTag(dbContext).RunAsync(StringServices.RandomString());
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new SubscribeToSearch.Request(userId, Guid.Empty, StringServices.RandomString(), "", new Guid[0], new Guid[] { tagId, tagId });
                await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new SubscribeToSearch(dbContext).RunAsync(request));
            }
        }
        [TestMethod()]
        public async Task TestTooManySubscriptions()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(testDB);
            using (var dbContext = new MemCheckDbContext(testDB))
                for (int i = 0; i < SubscribeToSearch.Request.MaxSubscriptionCount; i++)
                    await new SubscribeToSearch(dbContext).RunAsync(new SubscribeToSearch.Request(userId, Guid.Empty, StringServices.RandomString(), "", new Guid[0], new Guid[0]));

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new SubscribeToSearch.Request(userId, Guid.Empty, StringServices.RandomString(), "", new Guid[0], new Guid[0]);
                await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new SubscribeToSearch(dbContext).RunAsync(request));
            }
        }
    }
}
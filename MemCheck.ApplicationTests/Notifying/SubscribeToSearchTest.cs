using MemCheck.Application.Heaping;
using MemCheck.Application.Notifying;
using MemCheck.Application.QueryValidation;
using MemCheck.Application.Tests.Helpers;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

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
            var subscriptionName = StringHelper.RandomString();

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new SubscribeToSearch.Request(userId, Guid.Empty, subscriptionName, "", Array.Empty<Guid>(), Array.Empty<Guid>());
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
                deckId = await new CreateDeck(dbContext).RunAsync(new CreateDeck.Request(user, StringHelper.RandomString(), HeapingAlgorithms.DefaultAlgoId), new TestLocalizer());
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new SubscribeToSearch.Request(userId, deckId, StringHelper.RandomString(), "", Array.Empty<Guid>(), Array.Empty<Guid>());
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
            var text = StringHelper.RandomString();
            var subscriptionName = StringHelper.RandomString();

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new SubscribeToSearch.Request(userId, Guid.Empty, subscriptionName, text, Array.Empty<Guid>(), Array.Empty<Guid>());
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
                tagId = await new CreateTag(dbContext).RunAsync(StringHelper.RandomString());

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new SubscribeToSearch.Request(userId, Guid.Empty, StringHelper.RandomString(), "", new Guid[] { tagId }, Array.Empty<Guid>());
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
                tagId = await new CreateTag(dbContext).RunAsync(StringHelper.RandomString());
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new SubscribeToSearch.Request(userId, Guid.Empty, StringHelper.RandomString(), "", Array.Empty<Guid>(), new Guid[] { tagId });
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
                var request = new SubscribeToSearch.Request(userId, Guid.Empty, StringHelper.RandomString(), "", Array.Empty<Guid>(), null);
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

            using var dbContext = new MemCheckDbContext(testDB);
            var request = new SubscribeToSearch.Request(userId, Guid.NewGuid(), StringHelper.RandomString(), "", Array.Empty<Guid>(), Array.Empty<Guid>());
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new SubscribeToSearch(dbContext).RunAsync(request));
        }
        [TestMethod()]
        public async Task TestBadRequiredTag()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(testDB);

            using var dbContext = new MemCheckDbContext(testDB);
            var request = new SubscribeToSearch.Request(userId, Guid.Empty, StringHelper.RandomString(), "", new Guid[] { Guid.NewGuid() }, Array.Empty<Guid>());
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new SubscribeToSearch(dbContext).RunAsync(request));
        }
        [TestMethod()]
        public async Task TestBadExcludedTag()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(testDB);

            using var dbContext = new MemCheckDbContext(testDB);
            var request = new SubscribeToSearch.Request(userId, Guid.Empty, StringHelper.RandomString(), "", Array.Empty<Guid>(), new Guid[] { Guid.NewGuid() });
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new SubscribeToSearch(dbContext).RunAsync(request));
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
                tagId = await new CreateTag(dbContext).RunAsync(StringHelper.RandomString());
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new SubscribeToSearch.Request(userId, Guid.Empty, StringHelper.RandomString(), "", new Guid[] { tagId, tagId }, Array.Empty<Guid>());
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
                tagId = await new CreateTag(dbContext).RunAsync(StringHelper.RandomString());
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new SubscribeToSearch.Request(userId, Guid.Empty, StringHelper.RandomString(), "", Array.Empty<Guid>(), new Guid[] { tagId, tagId });
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
                    await new SubscribeToSearch(dbContext).RunAsync(new SubscribeToSearch.Request(userId, Guid.Empty, StringHelper.RandomString(), "", Array.Empty<Guid>(), Array.Empty<Guid>()));

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new SubscribeToSearch.Request(userId, Guid.Empty, StringHelper.RandomString(), "", Array.Empty<Guid>(), Array.Empty<Guid>());
                await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new SubscribeToSearch(dbContext).RunAsync(request));
            }
        }
    }
}
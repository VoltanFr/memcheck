using MemCheck.Application.Helpers;
using MemCheck.Application.Notifiying;
using MemCheck.Application.QueryValidation;
using MemCheck.Application.Tags;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Notifying;

[TestClass()]
public class SubscribeToSearchTest
{
    [TestMethod()]
    public async Task TestSearchEverything()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(testDB);
        var subscriptionName = RandomHelper.String();

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var request = new SubscribeToSearch.Request(userId, Guid.Empty, subscriptionName, "", Array.Empty<Guid>(), Array.Empty<Guid>());
            await new SubscribeToSearch(dbContext.AsCallContext()).RunAsync(request);
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
        var deckId = await DeckHelper.CreateAsync(testDB, userId);

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var request = new SubscribeToSearch.Request(userId, deckId, RandomHelper.String(), "", Array.Empty<Guid>(), Array.Empty<Guid>());
            await new SubscribeToSearch(dbContext.AsCallContext()).RunAsync(request);
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
        var text = RandomHelper.String();
        var subscriptionName = RandomHelper.String();

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var request = new SubscribeToSearch.Request(userId, Guid.Empty, subscriptionName, text, Array.Empty<Guid>(), Array.Empty<Guid>());
            await new SubscribeToSearch(dbContext.AsCallContext()).RunAsync(request);
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
            tagId = (await new CreateTag(dbContext.AsCallContext()).RunAsync(new CreateTag.Request(userId, RandomHelper.String(), ""))).TagId;

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var request = new SubscribeToSearch.Request(userId, Guid.Empty, RandomHelper.String(), "", new Guid[] { tagId }, Array.Empty<Guid>());
            await new SubscribeToSearch(dbContext.AsCallContext()).RunAsync(request);
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
            tagId = (await new CreateTag(dbContext.AsCallContext()).RunAsync(new CreateTag.Request(user.Id, RandomHelper.String(), ""))).TagId;
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var request = new SubscribeToSearch.Request(userId, Guid.Empty, RandomHelper.String(), "", Array.Empty<Guid>(), new Guid[] { tagId });
            await new SubscribeToSearch(dbContext.AsCallContext()).RunAsync(request);
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
            var request = new SubscribeToSearch.Request(userId, Guid.Empty, RandomHelper.String(), "", Array.Empty<Guid>(), null);
            await new SubscribeToSearch(dbContext.AsCallContext()).RunAsync(request);
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
        var request = new SubscribeToSearch.Request(userId, Guid.NewGuid(), RandomHelper.String(), "", Array.Empty<Guid>(), Array.Empty<Guid>());
        await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new SubscribeToSearch(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task TestBadRequiredTag()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(testDB);

        using var dbContext = new MemCheckDbContext(testDB);
        var request = new SubscribeToSearch.Request(userId, Guid.Empty, RandomHelper.String(), "", new Guid[] { Guid.NewGuid() }, Array.Empty<Guid>());
        await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new SubscribeToSearch(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task TestBadExcludedTag()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(testDB);

        using var dbContext = new MemCheckDbContext(testDB);
        var request = new SubscribeToSearch.Request(userId, Guid.Empty, RandomHelper.String(), "", Array.Empty<Guid>(), new Guid[] { Guid.NewGuid() });
        await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new SubscribeToSearch(dbContext.AsCallContext()).RunAsync(request));
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
            tagId = (await new CreateTag(dbContext.AsCallContext()).RunAsync(new CreateTag.Request(user.Id, RandomHelper.String(), ""))).TagId;
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var request = new SubscribeToSearch.Request(userId, Guid.Empty, RandomHelper.String(), "", new Guid[] { tagId, tagId }, Array.Empty<Guid>());
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new SubscribeToSearch(dbContext.AsCallContext()).RunAsync(request));
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
            tagId = (await new CreateTag(dbContext.AsCallContext()).RunAsync(new CreateTag.Request(user.Id, RandomHelper.String(), ""))).TagId;
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var request = new SubscribeToSearch.Request(userId, Guid.Empty, RandomHelper.String(), "", Array.Empty<Guid>(), new Guid[] { tagId, tagId });
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new SubscribeToSearch(dbContext.AsCallContext()).RunAsync(request));
        }
    }
    [TestMethod()]
    public async Task TestTooManySubscriptions()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(testDB);
        using (var dbContext = new MemCheckDbContext(testDB))
            for (var i = 0; i < SubscribeToSearch.Request.MaxSubscriptionCount; i++)
                await new SubscribeToSearch(dbContext.AsCallContext()).RunAsync(new SubscribeToSearch.Request(userId, Guid.Empty, RandomHelper.String(), "", Array.Empty<Guid>(), Array.Empty<Guid>()));

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var request = new SubscribeToSearch.Request(userId, Guid.Empty, RandomHelper.String(), "", Array.Empty<Guid>(), Array.Empty<Guid>());
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new SubscribeToSearch(dbContext.AsCallContext()).RunAsync(request));
        }
    }
}

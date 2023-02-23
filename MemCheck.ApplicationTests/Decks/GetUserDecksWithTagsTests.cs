using MemCheck.Application.Helpers;
using MemCheck.Application.QueryValidation;
using MemCheck.Application.Users;
using MemCheck.Basics;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Decks;

[TestClass()]
public class GetUserDecksWithTagsTests
{
    [TestMethod()]
    public async Task UserNotLoggedIn()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<NonexistentUserException>(async () => await new GetUserDecksWithTags(dbContext.AsCallContext()).RunAsync(new GetUserDecksWithTags.Request(Guid.Empty)));
    }
    [TestMethod()]
    public async Task UserDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<NonexistentUserException>(async () => await new GetUserDecksWithTags(dbContext.AsCallContext()).RunAsync(new GetUserDecksWithTags.Request(Guid.NewGuid())));
    }
    [TestMethod()]
    public async Task NoDeck()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);

        using (var dbContext = new MemCheckDbContext(db))
        {
            var deck = dbContext.Decks.Single();
            await new DeleteDeck(dbContext.AsCallContext()).RunAsync(new DeleteDeck.Request(user, deck.Id));
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new GetUserDecksWithTags(dbContext.AsCallContext()).RunAsync(new GetUserDecksWithTags.Request(user));
            Assert.IsFalse(result.Any());
        }
    }
    [TestMethod()]
    public async Task OneDeck_NoTag()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.GetUserSingleDeckAndSetTestHeapingAlgoAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        var result = await new GetUserDecksWithTags(dbContext.AsCallContext()).RunAsync(new GetUserDecksWithTags.Request(user));
        var resultDeck = result.Single();
        Assert.AreEqual(deck, resultDeck.DeckId);
        Assert.AreEqual(MemCheckUserManager.DefaultDeckName, resultDeck.Description);
        Assert.IsFalse(resultDeck.Tags.Any());
    }
    [TestMethod()]
    public async Task OneDeck_WithTags()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);

        var deck = await DeckHelper.GetUserSingleDeckAndSetTestHeapingAlgoAsync(db, user.Id);

        var tag1 = await TagHelper.CreateAsync(db, user);
        var card1 = await CardHelper.CreateAsync(db, user.Id, tagIds: tag1.AsArray());
        await DeckHelper.AddCardAsync(db, deck, card1.Id);

        var tag2 = await TagHelper.CreateAsync(db, user);
        var card2 = await CardHelper.CreateAsync(db, user.Id, tagIds: tag2.AsArray());
        await DeckHelper.AddCardAsync(db, deck, card2.Id);

        var card3 = await CardHelper.CreateAsync(db, user.Id, tagIds: new[] { tag1, tag2 });
        await DeckHelper.AddCardAsync(db, deck, card3.Id);

        var card4 = await CardHelper.CreateAsync(db, user.Id);
        await DeckHelper.AddCardAsync(db, deck, card4.Id);

        using var dbContext = new MemCheckDbContext(db);
        var result = await new GetUserDecksWithTags(dbContext.AsCallContext()).RunAsync(new GetUserDecksWithTags.Request(user.Id));
        var resultDeck = result.Single();
        Assert.AreEqual(deck, resultDeck.DeckId);
        Assert.AreEqual(MemCheckUserManager.DefaultDeckName, resultDeck.Description);
        Assert.AreEqual(2, resultDeck.Tags.Count());
        Assert.IsTrue(resultDeck.Tags.Any(t => t.TagId == tag1));
        Assert.IsTrue(resultDeck.Tags.Any(t => t.TagId == tag2));
    }
    [TestMethod()]
    public async Task TwoDecks_WithTags()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);

        var deck1Name = RandomHelper.String();
        var deck1Algo = RandomHelper.HeapingAlgorithm();
        var deck1 = await DeckHelper.CreateAsync(db, user.Id, deck1Name, deck1Algo);

        var deck2Name = RandomHelper.String();
        var deck2Algo = RandomHelper.HeapingAlgorithm();
        var deck2 = await DeckHelper.CreateAsync(db, user.Id, deck2Name, deck2Algo);

        var tag1 = await TagHelper.CreateAsync(db, user);
        var card1 = await CardHelper.CreateAsync(db, user.Id, tagIds: tag1.AsArray());
        await DeckHelper.AddCardAsync(db, deck1, card1.Id);
        await DeckHelper.AddCardAsync(db, deck2, card1.Id);

        var tag2 = await TagHelper.CreateAsync(db, user);
        var card2 = await CardHelper.CreateAsync(db, user.Id, tagIds: tag2.AsArray());
        await DeckHelper.AddCardAsync(db, deck1, card2.Id);

        var card3 = await CardHelper.CreateAsync(db, user.Id, tagIds: new[] { tag1, tag2 });
        await DeckHelper.AddCardAsync(db, deck1, card3.Id);

        var card4 = await CardHelper.CreateAsync(db, user.Id);
        await DeckHelper.AddCardAsync(db, deck1, card4.Id);
        await DeckHelper.AddCardAsync(db, deck2, card4.Id);

        using var dbContext = new MemCheckDbContext(db);
        var result = await new GetUserDecksWithTags(dbContext.AsCallContext()).RunAsync(new GetUserDecksWithTags.Request(user.Id));

        var resultDeck1 = result.Single(d => d.DeckId == deck1);
        Assert.AreEqual(deck1, resultDeck1.DeckId);
        Assert.AreEqual(deck1Name, resultDeck1.Description);
        Assert.AreEqual(2, resultDeck1.Tags.Count());
        Assert.IsTrue(resultDeck1.Tags.Any(t => t.TagId == tag1));
        Assert.IsTrue(resultDeck1.Tags.Any(t => t.TagId == tag2));

        var resultDeck2 = result.Single(d => d.DeckId == deck2);
        Assert.AreEqual(deck2, resultDeck2.DeckId);
        Assert.AreEqual(deck2Name, resultDeck2.Description);
        Assert.AreEqual(tag1, resultDeck2.Tags.Single().TagId);
    }
}

using MemCheck.Application.Heaping;
using MemCheck.Application.Helpers;
using MemCheck.Application.QueryValidation;
using MemCheck.Basics;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Decks;

[TestClass()]
public class GetUserDecksWithHeapsAndTagsAndTagsTests
{
    [TestMethod()]
    public async Task UserNotLoggedIn()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<NonexistentUserException>(async () => await new GetUserDecksWithHeapsAndTags(dbContext.AsCallContext()).RunAsync(new GetUserDecksWithHeapsAndTags.Request(Guid.Empty)));
    }
    [TestMethod()]
    public async Task UserDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<NonexistentUserException>(async () => await new GetUserDecksWithHeapsAndTags(dbContext.AsCallContext()).RunAsync(new GetUserDecksWithHeapsAndTags.Request(Guid.NewGuid())));
    }
    [TestMethod()]
    public async Task NoDeck()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        var result = await new GetUserDecksWithHeapsAndTags(dbContext.AsCallContext()).RunAsync(new GetUserDecksWithHeapsAndTags.Request(user));
        Assert.IsFalse(result.Any());
    }
    [TestMethod()]
    public async Task OneDeck_Empty()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);

        var deckName = RandomHelper.String();
        var deckAlgo = RandomHelper.HeapingAlgorithm();
        var deck = await DeckHelper.CreateAsync(db, user, deckName, deckAlgo);

        using var dbContext = new MemCheckDbContext(db);
        var result = await new GetUserDecksWithHeapsAndTags(dbContext.AsCallContext()).RunAsync(new GetUserDecksWithHeapsAndTags.Request(user));
        var resultDeck = result.Single();
        Assert.AreEqual(deck, resultDeck.DeckId);
        Assert.AreEqual(deckName, resultDeck.Description);
        Assert.IsFalse(resultDeck.Heaps.Any());
        Assert.IsFalse(resultDeck.Tags.Any());
    }
    [TestMethod()]
    public async Task OneDeck()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user, algorithmId: HeapingAlgorithms.DefaultAlgoId);
        var tag1 = await TagHelper.CreateAsync(db);
        var tag2 = await TagHelper.CreateAsync(db);

        await DeckHelper.AddCardAsync(db, deck, (await CardHelper.CreateAsync(db, user, tagIds: tag1.AsArray())).Id, 0);
        await DeckHelper.AddCardAsync(db, deck, (await CardHelper.CreateAsync(db, user)).Id, 0);

        await DeckHelper.AddCardAsync(db, deck, (await CardHelper.CreateAsync(db, user)).Id, 1);
        await DeckHelper.AddCardAsync(db, deck, (await CardHelper.CreateAsync(db, user, tagIds: new[] { tag1, tag2 })).Id, 1);

        await DeckHelper.AddCardAsync(db, deck, (await CardHelper.CreateAsync(db, user)).Id, 2);

        await DeckHelper.AddCardAsync(db, deck, (await CardHelper.CreateAsync(db, user)).Id, 4);
        await DeckHelper.AddCardAsync(db, deck, (await CardHelper.CreateAsync(db, user)).Id, 4);

        using var dbContext = new MemCheckDbContext(db);
        var result = await new GetUserDecksWithHeapsAndTags(dbContext.AsCallContext()).RunAsync(new GetUserDecksWithHeapsAndTags.Request(user));
        var resultDeck = result.Single();
        Assert.AreEqual(deck, resultDeck.DeckId);
        Assert.IsTrue(resultDeck.Heaps.SequenceEqual(new[] { 0, 1, 2, 4 }));
        Assert.AreEqual(2, resultDeck.Tags.Count());
        Assert.IsTrue(resultDeck.Tags.Any(tag => tag.TagId == tag1));
        Assert.IsTrue(resultDeck.Tags.Any(tag => tag.TagId == tag2));
    }
    [TestMethod()]
    public async Task TwoDecks()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck1 = await DeckHelper.CreateAsync(db, user, algorithmId: HeapingAlgorithms.DefaultAlgoId);
        var deck2 = await DeckHelper.CreateAsync(db, user, algorithmId: HeapingAlgorithms.DefaultAlgoId);
        var tag1 = await TagHelper.CreateAsync(db);
        var tag2 = await TagHelper.CreateAsync(db);

        await DeckHelper.AddCardAsync(db, deck1, (await CardHelper.CreateAsync(db, user, tagIds: tag1.AsArray())).Id, 0);
        await DeckHelper.AddCardAsync(db, deck1, (await CardHelper.CreateAsync(db, user)).Id, 0);

        await DeckHelper.AddCardAsync(db, deck1, (await CardHelper.CreateAsync(db, user)).Id, 1);
        var card = await CardHelper.CreateAsync(db, user, tagIds: new[] { tag1, tag2 });
        await DeckHelper.AddCardAsync(db, deck1, card.Id, 1);
        await DeckHelper.AddCardAsync(db, deck2, card.Id, 2);

        await DeckHelper.AddCardAsync(db, deck1, (await CardHelper.CreateAsync(db, user)).Id, 2);

        await DeckHelper.AddCardAsync(db, deck1, (await CardHelper.CreateAsync(db, user)).Id, 4);
        await DeckHelper.AddCardAsync(db, deck1, (await CardHelper.CreateAsync(db, user)).Id, 4);

        using var dbContext = new MemCheckDbContext(db);
        var result = await new GetUserDecksWithHeapsAndTags(dbContext.AsCallContext()).RunAsync(new GetUserDecksWithHeapsAndTags.Request(user));
        Assert.AreEqual(2, result.Count());

        var resultDeck1 = result.Single(deck => deck.DeckId == deck1);
        Assert.IsTrue(resultDeck1.Heaps.SequenceEqual(new[] { 0, 1, 2, 4 }));
        Assert.AreEqual(2, resultDeck1.Tags.Count());
        Assert.IsTrue(resultDeck1.Tags.Any(tag => tag.TagId == tag1));
        Assert.IsTrue(resultDeck1.Tags.Any(tag => tag.TagId == tag2));

        var resultDeck2 = result.Single(deck => deck.DeckId == deck2);
        Assert.IsTrue(resultDeck2.Heaps.SequenceEqual(2.AsArray()));
        Assert.AreEqual(2, resultDeck2.Tags.Count());
        Assert.IsTrue(resultDeck2.Tags.Any(tag => tag.TagId == tag1));
        Assert.IsTrue(resultDeck2.Tags.Any(tag => tag.TagId == tag2));
    }
}

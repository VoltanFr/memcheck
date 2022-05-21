using MemCheck.Application.Helpers;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Decks;

[TestClass()]
public class GetUserDecksTests
{
    [TestMethod()]
    public async Task UserNotLoggedIn()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetUserDecks(dbContext.AsCallContext()).RunAsync(new GetUserDecks.Request(Guid.Empty)));
    }
    [TestMethod()]
    public async Task UserDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetUserDecks(dbContext.AsCallContext()).RunAsync(new GetUserDecks.Request(Guid.NewGuid())));
    }
    [TestMethod()]
    public async Task NoDeck()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        var result = await new GetUserDecks(dbContext.AsCallContext()).RunAsync(new GetUserDecks.Request(user));
        Assert.IsFalse(result.Any());
    }
    [TestMethod()]
    public async Task OneDeck()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);

        var deckName = RandomHelper.String();
        var deckAlgo = RandomHelper.HeapingAlgorithm();
        var deck = await DeckHelper.CreateAsync(db, user, deckName, deckAlgo);

        using var dbContext = new MemCheckDbContext(db);
        var result = await new GetUserDecks(dbContext.AsCallContext()).RunAsync(new GetUserDecks.Request(user));
        var resultDeck = result.Single();
        Assert.AreEqual(deck, resultDeck.DeckId);
        Assert.AreEqual(deckName, resultDeck.Description);
        Assert.AreEqual(deckAlgo, resultDeck.HeapingAlgorithmId);
    }
    [TestMethod()]
    public async Task TwoDecks()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);

        var deck1Name = RandomHelper.String();
        var deck1Algo = RandomHelper.HeapingAlgorithm();
        var deck1 = await DeckHelper.CreateAsync(db, user, deck1Name, deck1Algo);

        var deck2Name = RandomHelper.String();
        var deck2Algo = RandomHelper.HeapingAlgorithm();
        var deck2 = await DeckHelper.CreateAsync(db, user, deck2Name, deck2Algo);

        using var dbContext = new MemCheckDbContext(db);
        var result = await new GetUserDecks(dbContext.AsCallContext()).RunAsync(new GetUserDecks.Request(user));
        Assert.AreEqual(2, result.Count());
        var resultDeck1 = result.Single(d => d.DeckId == deck1);
        Assert.AreEqual(deck1Name, resultDeck1.Description);
        Assert.AreEqual(deck1Algo, resultDeck1.HeapingAlgorithmId);
        var resultDeck2 = result.Single(d => d.DeckId == deck2);
        Assert.AreEqual(deck2Name, resultDeck2.Description);
        Assert.AreEqual(deck2Algo, resultDeck2.HeapingAlgorithmId);
    }
}

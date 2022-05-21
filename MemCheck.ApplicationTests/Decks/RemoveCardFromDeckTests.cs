using MemCheck.Application.Helpers;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Decks;

[TestClass()]
public class RemoveCardFromDeckTests
{
    [TestMethod()]
    public async Task UserNotLoggedIn()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);
        var card = await CardHelper.CreateAsync(db, user);
        await DeckHelper.AddCardAsync(db, deck, card.Id);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new RemoveCardFromDeck(dbContext.AsCallContext()).RunAsync(new RemoveCardFromDeck.Request(Guid.Empty, deck, card.Id)));
    }
    [TestMethod()]
    public async Task UserDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);
        var card = await CardHelper.CreateAsync(db, user);
        await DeckHelper.AddCardAsync(db, deck, card.Id);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new RemoveCardFromDeck(dbContext.AsCallContext()).RunAsync(new RemoveCardFromDeck.Request(Guid.NewGuid(), deck, card.Id)));
    }
    [TestMethod()]
    public async Task DeckDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var card = await CardHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new RemoveCardFromDeck(dbContext.AsCallContext()).RunAsync(new RemoveCardFromDeck.Request(user, Guid.NewGuid(), card.Id)));
    }
    [TestMethod()]
    public async Task UserNotOwnerOfDeck()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);
        var card = await CardHelper.CreateAsync(db, user);
        await DeckHelper.AddCardAsync(db, deck, card.Id);
        var otherUser = await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new RemoveCardFromDeck(dbContext.AsCallContext()).RunAsync(new RemoveCardFromDeck.Request(otherUser, deck, card.Id)));
    }
    [TestMethod()]
    public async Task CardDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new RemoveCardFromDeck(dbContext.AsCallContext()).RunAsync(new RemoveCardFromDeck.Request(user, deck, Guid.NewGuid())));
    }
    [TestMethod()]
    public async Task CardNotInDeck()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);
        var card = await CardHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new RemoveCardFromDeck(dbContext.AsCallContext()).RunAsync(new RemoveCardFromDeck.Request(user, deck, card.Id)));
    }
    [TestMethod()]
    public async Task OnlyCardInTheDeck()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);
        var card = await CardHelper.CreateAsync(db, user);
        await DeckHelper.AddCardAsync(db, deck, card.Id);

        using (var dbContext = new MemCheckDbContext(db))
            await new RemoveCardFromDeck(dbContext.AsCallContext()).RunAsync(new RemoveCardFromDeck.Request(user, deck, card.Id));

        await DeckHelper.CheckDeckDoesNotContainCard(db, deck, card.Id);
    }
    [TestMethod()]
    public async Task Complex()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);
        var card1 = await CardHelper.CreateAsync(db, user);
        await DeckHelper.AddCardAsync(db, deck, card1.Id);
        var card2 = await CardHelper.CreateAsync(db, user);
        await DeckHelper.AddCardAsync(db, deck, card2.Id);
        var card3 = await CardHelper.CreateAsync(db, user);
        await DeckHelper.AddCardAsync(db, deck, card3.Id);

        using (var dbContext = new MemCheckDbContext(db))
            await new RemoveCardFromDeck(dbContext.AsCallContext()).RunAsync(new RemoveCardFromDeck.Request(user, deck, card2.Id));

        await DeckHelper.CheckDeckDoesNotContainCard(db, deck, card2.Id);
        await DeckHelper.CheckDeckContainsCards(db, deck, card1.Id, card3.Id);
    }
}




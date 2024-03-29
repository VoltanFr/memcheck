﻿using MemCheck.Application.Heaping;
using MemCheck.Application.Helpers;
using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Decks;

[TestClass()]
public class UpdateDeckTests
{
    [TestMethod()]
    public async Task UserNotLoggedIn()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        var request = new UpdateDeck.Request(Guid.Empty, deck, RandomHelper.String(), 0);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new UpdateDeck(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task UserDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        var request = new UpdateDeck.Request(Guid.NewGuid(), deck, RandomHelper.String(), 0);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new UpdateDeck(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task DeckDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        var request = new UpdateDeck.Request(user, Guid.NewGuid(), RandomHelper.String(), 0);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new UpdateDeck(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task UserNotOwner()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);
        var otherUser = await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        var request = new UpdateDeck.Request(otherUser, deck, RandomHelper.String(), RandomHelper.HeapingAlgorithm());
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new UpdateDeck(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task NameNotTrimmed()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        var request = new UpdateDeck.Request(user, deck, RandomHelper.String() + '\t', 0);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new UpdateDeck(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task NameTooShort()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        var request = new UpdateDeck.Request(user, deck, RandomHelper.String(QueryValidationHelper.DeckMinNameLength - 1), 0);
        await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateDeck(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task NameTooLong()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        var request = new UpdateDeck.Request(user, deck, RandomHelper.String(QueryValidationHelper.DeckMaxNameLength + 1), 0);
        await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateDeck(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task DeckWithThisNameExists()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);
        var otherDeckName = RandomHelper.String();
        await DeckHelper.CreateAsync(db, user, otherDeckName);

        using var dbContext = new MemCheckDbContext(db);
        var request = new UpdateDeck.Request(user, deck, otherDeckName, RandomHelper.HeapingAlgorithm());
        await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateDeck(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task InexistentAlgorithm()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        var request = new UpdateDeck.Request(user, deck, RandomHelper.String(), RandomHelper.ValueNotInSet(HeapingAlgorithms.Instance.Ids));
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new UpdateDeck(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task FieldsCorrectlyUpdated()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);

        var newName = RandomHelper.String();
        var newAlgo = RandomHelper.HeapingAlgorithm();
        var request = new UpdateDeck.Request(user, deck, newName, newAlgo);

        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateDeck(dbContext.AsCallContext()).RunAsync(request);

        using (var dbContext = new MemCheckDbContext(db))
        {
            var deckFromDb = await dbContext.Decks.SingleAsync(d => d.Id == deck);
            Assert.AreEqual(newName, deckFromDb.Description);
            Assert.AreEqual(newAlgo, deckFromDb.HeapingAlgorithmId);
        }
    }
    [TestMethod()]
    public async Task FieldsNotAltered()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);
        var card = await CardHelper.CreateAsync(db, user);
        await DeckHelper.AddCardAsync(db, deck, card.Id);

        var request = new UpdateDeck.Request(user, deck, RandomHelper.String(), RandomHelper.HeapingAlgorithm());

        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateDeck(dbContext.AsCallContext()).RunAsync(request);

        using (var dbContext = new MemCheckDbContext(db))
        {
            var deckFromDb = await dbContext.Decks.Include(d => d.Owner).Include(d => d.CardInDecks).SingleAsync(d => d.Id == deck);
            Assert.AreEqual(user, deckFromDb.Owner.Id);
            Assert.AreEqual(card.Id, deckFromDb.CardInDecks.Single().CardId);
        }
    }
}

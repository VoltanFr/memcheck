﻿using MemCheck.Application.Heaping;
using MemCheck.Application.Helpers;
using MemCheck.Application.QueryValidation;
using MemCheck.Application.Users;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Decks;

[TestClass()]
public class GetUserDecksWithHeapsTests
{
    [TestMethod()]
    public async Task UserNotLoggedIn()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<NonexistentUserException>(async () => await new GetUserDecksWithHeaps(dbContext.AsCallContext()).RunAsync(new GetUserDecksWithHeaps.Request(Guid.Empty)));
    }
    [TestMethod()]
    public async Task UserDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<NonexistentUserException>(async () => await new GetUserDecksWithHeaps(dbContext.AsCallContext()).RunAsync(new GetUserDecksWithHeaps.Request(Guid.NewGuid())));
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
            var result = await new GetUserDecksWithHeaps(dbContext.AsCallContext()).RunAsync(new GetUserDecksWithHeaps.Request(user));
            Assert.IsFalse(result.Any());
        }
    }
    [TestMethod()]
    public async Task OneDeck_Empty()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);

        var deck = await DeckHelper.GetUserSingleDeckAndSetTestHeapingAlgoAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        var result = await new GetUserDecksWithHeaps(dbContext.AsCallContext()).RunAsync(new GetUserDecksWithHeaps.Request(user));
        var resultDeck = result.Single();
        Assert.AreEqual(deck, resultDeck.DeckId);
        Assert.AreEqual(MemCheckUserManager.DefaultDeckName, resultDeck.Description);
        Assert.AreEqual(UnitTestsHeapingAlgorithm.ID, resultDeck.HeapingAlgorithmId);
        Assert.AreEqual(0, resultDeck.CardCount);
        Assert.IsFalse(resultDeck.Heaps.Any());
    }
    [TestMethod()]
    public async Task OneDeck_WithCards()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.GetUserSingleDeckAndSetTestHeapingAlgoAsync(db, user, algorithmId: HeapingAlgorithms.DefaultAlgoId);

        var runDate = RandomHelper.Date();

        await DeckHelper.AddCardAsync(db, deck, (await CardHelper.CreateAsync(db, user)).Id, 0);
        await DeckHelper.AddCardAsync(db, deck, (await CardHelper.CreateAsync(db, user)).Id, 0);

        await DeckHelper.AddCardAsync(db, deck, (await CardHelper.CreateAsync(db, user)).Id, 1, RandomHelper.DateBefore(runDate.AddDays(-3)));  //expired
        await DeckHelper.AddCardAsync(db, deck, (await CardHelper.CreateAsync(db, user)).Id, 1, runDate.AddDays(-1));

        await DeckHelper.AddCardAsync(db, deck, (await CardHelper.CreateAsync(db, user)).Id, 2, runDate.AddDays(-10));  //expired

        await DeckHelper.AddCardAsync(db, deck, (await CardHelper.CreateAsync(db, user)).Id, 4, runDate.AddDays(-10));
        await DeckHelper.AddCardAsync(db, deck, (await CardHelper.CreateAsync(db, user)).Id, 4, runDate.AddDays(-13));

        using var dbContext = new MemCheckDbContext(db);
        var result = await new GetUserDecksWithHeaps(dbContext.AsCallContext(), runDate).RunAsync(new GetUserDecksWithHeaps.Request(user));
        var resultDeck = result.Single();
        Assert.AreEqual(deck, resultDeck.DeckId);
        Assert.AreEqual(7, resultDeck.CardCount);
        Assert.AreEqual(4, resultDeck.Heaps.Count());

        var unknownHeap = resultDeck.Heaps.Single(heap => heap.HeapId == 0);
        Assert.AreEqual(DateTime.MaxValue, unknownHeap.NextExpiryUtcDate);
        Assert.AreEqual(2, unknownHeap.TotalCardCount);

        var heap1 = resultDeck.Heaps.Single(heap => heap.HeapId == 1);
        Assert.AreEqual(2, heap1.TotalCardCount);
        Assert.AreEqual(1, heap1.ExpiredCardCount);
        DateAssert.IsInRange(runDate.AddDays(1), TimeSpan.FromMinutes(20), heap1.NextExpiryUtcDate);

        var heap2 = resultDeck.Heaps.Single(heap => heap.HeapId == 2);
        Assert.AreEqual(1, heap2.TotalCardCount);
        Assert.AreEqual(1, heap2.ExpiredCardCount);
        Assert.AreEqual(DateTime.MaxValue, heap2.NextExpiryUtcDate);

        var heap4 = resultDeck.Heaps.Single(heap => heap.HeapId == 4);
        Assert.AreEqual(2, heap4.TotalCardCount);
        Assert.AreEqual(0, heap4.ExpiredCardCount);
        DateAssert.IsInRange(runDate.AddDays(3), TimeSpan.FromMinutes(160), heap4.NextExpiryUtcDate);
    }
    [TestMethod()]
    public async Task TwoDecks_WithCards()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck1 = await DeckHelper.CreateAsync(db, user, algorithmId: HeapingAlgorithms.DefaultAlgoId);
        var deck2 = await DeckHelper.CreateAsync(db, user, algorithmId: HeapingAlgorithms.DefaultAlgoId);

        var runDate = RandomHelper.Date();

        var card1 = (await CardHelper.CreateAsync(db, user)).Id;
        await DeckHelper.AddCardAsync(db, deck1, card1, 0);
        await DeckHelper.AddCardAsync(db, deck2, card1, 0);
        await DeckHelper.AddCardAsync(db, deck1, (await CardHelper.CreateAsync(db, user)).Id, 0);

        await DeckHelper.AddCardAsync(db, deck1, (await CardHelper.CreateAsync(db, user)).Id, 1, RandomHelper.DateBefore(runDate.AddDays(-3))); //expired
        await DeckHelper.AddCardAsync(db, deck2, (await CardHelper.CreateAsync(db, user)).Id, 1, runDate.AddDays(-1));

        await DeckHelper.AddCardAsync(db, deck1, (await CardHelper.CreateAsync(db, user)).Id, 2, runDate.AddDays(-10)); //expired

        await DeckHelper.AddCardAsync(db, deck2, (await CardHelper.CreateAsync(db, user)).Id, 4, runDate.AddDays(-10));
        await DeckHelper.AddCardAsync(db, deck2, (await CardHelper.CreateAsync(db, user)).Id, 4, runDate.AddDays(-13));

        using var dbContext = new MemCheckDbContext(db);
        var result = await new GetUserDecksWithHeaps(dbContext.AsCallContext(), runDate).RunAsync(new GetUserDecksWithHeaps.Request(user));

        var resultDeck1 = result.Single(deck => deck.DeckId == deck1);
        Assert.AreEqual(4, resultDeck1.CardCount);
        Assert.AreEqual(3, resultDeck1.Heaps.Count());

        var resultDeck1Heap0 = resultDeck1.Heaps.Single(heap => heap.HeapId == 0);
        Assert.AreEqual(DateTime.MaxValue, resultDeck1Heap0.NextExpiryUtcDate);
        Assert.AreEqual(2, resultDeck1Heap0.TotalCardCount);

        var resultDeck1Heap1 = resultDeck1.Heaps.Single(heap => heap.HeapId == 1);
        Assert.AreEqual(1, resultDeck1Heap1.TotalCardCount);
        Assert.AreEqual(1, resultDeck1Heap1.ExpiredCardCount);
        Assert.AreEqual(DateTime.MaxValue, resultDeck1Heap1.NextExpiryUtcDate);

        var resultDeck1Heap2 = resultDeck1.Heaps.Single(heap => heap.HeapId == 2);
        Assert.AreEqual(1, resultDeck1Heap2.TotalCardCount);
        Assert.AreEqual(1, resultDeck1Heap2.ExpiredCardCount);
        Assert.AreEqual(DateTime.MaxValue, resultDeck1Heap2.NextExpiryUtcDate);

        var resultDeck2 = result.Single(deck => deck.DeckId == deck2);
        Assert.AreEqual(4, resultDeck1.CardCount);
        Assert.AreEqual(3, resultDeck1.Heaps.Count());

        var resultDeck2Heap0 = resultDeck2.Heaps.Single(heap => heap.HeapId == 0);
        Assert.AreEqual(DateTime.MaxValue, resultDeck2Heap0.NextExpiryUtcDate);
        Assert.AreEqual(1, resultDeck2Heap0.TotalCardCount);

        var resultDeck2Heap1 = resultDeck2.Heaps.Single(heap => heap.HeapId == 1);
        Assert.AreEqual(1, resultDeck2Heap1.TotalCardCount);
        Assert.AreEqual(0, resultDeck2Heap1.ExpiredCardCount);
        DateAssert.IsInRange(runDate.AddDays(1), TimeSpan.FromMinutes(20), resultDeck2Heap1.NextExpiryUtcDate);

        var resultDeck2Heap4 = resultDeck2.Heaps.Single(heap => heap.HeapId == 4);
        Assert.AreEqual(2, resultDeck2Heap4.TotalCardCount);
        Assert.AreEqual(0, resultDeck2Heap4.ExpiredCardCount);
        DateAssert.IsInRange(runDate.AddDays(3), TimeSpan.FromMinutes(160), resultDeck2Heap4.NextExpiryUtcDate);
    }
}

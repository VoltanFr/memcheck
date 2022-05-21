using MemCheck.Application.Helpers;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Heaping;

[TestClass()]
public class MoveCardToHeapTests
{
    #region Failure cases
    [TestMethod()]
    public async Task UserNotLoggedIn()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);
        var card = await CardHelper.CreateAsync(db, user);
        await DeckHelper.AddCardAsync(db, deck, card.Id);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new MoveCardToHeap(dbContext.AsCallContext()).RunAsync(new MoveCardToHeap.Request(Guid.Empty, deck, card.Id, RandomHelper.Heap())));
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
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new MoveCardToHeap(dbContext.AsCallContext()).RunAsync(new MoveCardToHeap.Request(Guid.NewGuid(), deck, card.Id, RandomHelper.Heap())));
    }
    [TestMethod()]
    public async Task DeckDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);
        var card = await CardHelper.CreateAsync(db, user);
        await DeckHelper.AddCardAsync(db, deck, card.Id);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new MoveCardToHeap(dbContext.AsCallContext()).RunAsync(new MoveCardToHeap.Request(user, Guid.NewGuid(), card.Id, RandomHelper.Heap())));
    }
    [TestMethod()]
    public async Task UserNotOwner()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);
        var card = await CardHelper.CreateAsync(db, user);
        await DeckHelper.AddCardAsync(db, deck, card.Id);
        var otherUser = await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new MoveCardToHeap(dbContext.AsCallContext()).RunAsync(new MoveCardToHeap.Request(otherUser, deck, card.Id, RandomHelper.Heap())));
    }
    [TestMethod()]
    public async Task CardDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);
        var card = await CardHelper.CreateAsync(db, user);
        await DeckHelper.AddCardAsync(db, deck, card.Id);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new MoveCardToHeap(dbContext.AsCallContext()).RunAsync(new MoveCardToHeap.Request(user, deck, Guid.NewGuid(), RandomHelper.Heap())));
    }
    [TestMethod()]
    public async Task CardNotInDeck()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);
        var card = await CardHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new MoveCardToHeap(dbContext.AsCallContext()).RunAsync(new MoveCardToHeap.Request(user, deck, card.Id, RandomHelper.Heap())));
    }
    [TestMethod()]
    public async Task HeapTooSmall()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);
        var card = await CardHelper.CreateAsync(db, user);
        await DeckHelper.AddCardAsync(db, deck, card.Id);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new MoveCardToHeap(dbContext.AsCallContext()).RunAsync(new MoveCardToHeap.Request(user, deck, card.Id, -1)));
    }
    [TestMethod()]
    public async Task HeapTooBig()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);
        var card = await CardHelper.CreateAsync(db, user);
        await DeckHelper.AddCardAsync(db, deck, card.Id);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new MoveCardToHeap(dbContext.AsCallContext()).RunAsync(new MoveCardToHeap.Request(user, deck, card.Id, CardInDeck.MaxHeapValue + 1)));
    }
    [TestMethod()]
    public async Task LearnMoveUpMoreThanOneHeap()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);
        var card = await CardHelper.CreateAsync(db, user);
        await DeckHelper.AddCardAsync(db, deck, card.Id, heap: 1);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new MoveCardToHeap(dbContext.AsCallContext()).RunAsync(new MoveCardToHeap.Request(user, deck, card.Id, 3)));
    }
    [TestMethod()]
    public async Task LearnMoveDown()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);
        var card = await CardHelper.CreateAsync(db, user);
        await DeckHelper.AddCardAsync(db, deck, card.Id, heap: 4);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new MoveCardToHeap(dbContext.AsCallContext()).RunAsync(new MoveCardToHeap.Request(user, deck, card.Id, 3)));
    }
    #endregion
    [TestMethod()]
    public async Task LearnMoveUp()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user, algorithmId: UnitTestsHeapingAlgorithm.ID);
        var card = await CardHelper.CreateAsync(db, user);
        await DeckHelper.AddCardAsync(db, deck, card.Id, heap: 1);
        var runTime = RandomHelper.Date();

        using (var dbContext = new MemCheckDbContext(db))
            await new MoveCardToHeap(dbContext.AsCallContext(), runTime).RunAsync(new MoveCardToHeap.Request(user, deck, card.Id, 2));

        using (var dbContext = new MemCheckDbContext(db))
        {
            var cardInDeck = dbContext.CardsInDecks.Single();
            Assert.AreEqual(2, cardInDeck.CurrentHeap);
            Assert.AreEqual(runTime, cardInDeck.LastLearnUtcTime);
            Assert.AreEqual(1, cardInDeck.NbTimesInNotLearnedHeap);
            Assert.AreEqual(2, cardInDeck.BiggestHeapReached);
            Assert.AreEqual(runTime.AddDays(2), cardInDeck.ExpiryUtcTime);
        }
    }
    [TestMethod()]
    public async Task LearnMoveToSameHeap()
    {
        //This could happen due to multiple sessions by the user

        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);
        var card = await CardHelper.CreateAsync(db, user);
        var initialTime = RandomHelper.Date();
        await DeckHelper.AddCardAsync(db, deck, card.Id, heap: 1, lastLearnUtcTime: initialTime);

        using (var dbContext = new MemCheckDbContext(db))
            await new MoveCardToHeap(dbContext.AsCallContext(), RandomHelper.Date(initialTime)).RunAsync(new MoveCardToHeap.Request(user, deck, card.Id, 1));

        using (var dbContext = new MemCheckDbContext(db))
        {
            var cardInDeck = dbContext.CardsInDecks.Single();
            Assert.AreEqual(1, cardInDeck.CurrentHeap);
            Assert.AreEqual(initialTime, cardInDeck.LastLearnUtcTime);
            Assert.AreEqual(1, cardInDeck.NbTimesInNotLearnedHeap);
            Assert.AreEqual(1, cardInDeck.BiggestHeapReached);
            Assert.AreEqual(initialTime.AddDays(1), cardInDeck.ExpiryUtcTime);
        }
    }
    [TestMethod()]
    public async Task LearnMoveToUnknown()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);
        var card = await CardHelper.CreateAsync(db, user);
        await DeckHelper.AddCardAsync(db, deck, card.Id, heap: 4);
        var runTime = RandomHelper.Date();

        using (var dbContext = new MemCheckDbContext(db))
            await new MoveCardToHeap(dbContext.AsCallContext(), runTime).RunAsync(new MoveCardToHeap.Request(user, deck, card.Id, CardInDeck.UnknownHeap));

        using (var dbContext = new MemCheckDbContext(db))
        {
            var cardInDeck = dbContext.CardsInDecks.Single();
            Assert.AreEqual(CardInDeck.UnknownHeap, cardInDeck.CurrentHeap);
            Assert.AreEqual(runTime, cardInDeck.LastLearnUtcTime);
            Assert.AreEqual(2, cardInDeck.NbTimesInNotLearnedHeap);
            Assert.AreEqual(4, cardInDeck.BiggestHeapReached);
            Assert.AreEqual(DateTime.MinValue, cardInDeck.ExpiryUtcTime);
        }
    }
    [TestMethod()]
    public async Task LearnMoveFromUnknownToUnknown()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);
        var card = await CardHelper.CreateAsync(db, user);
        await DeckHelper.AddCardAsync(db, deck, card.Id, heap: 4);

        var runTime = RandomHelper.Date();
        using (var dbContext = new MemCheckDbContext(db))
            await new MoveCardToHeap(dbContext.AsCallContext(), runTime).RunAsync(new MoveCardToHeap.Request(user, deck, card.Id, CardInDeck.UnknownHeap));
        using (var dbContext = new MemCheckDbContext(db))
        {
            var cardInDeck = dbContext.CardsInDecks.Single();
            Assert.AreEqual(CardInDeck.UnknownHeap, cardInDeck.CurrentHeap);
            Assert.AreEqual(runTime, cardInDeck.LastLearnUtcTime);
            Assert.AreEqual(2, cardInDeck.NbTimesInNotLearnedHeap);
            Assert.AreEqual(4, cardInDeck.BiggestHeapReached);
            Assert.AreEqual(DateTime.MinValue, cardInDeck.ExpiryUtcTime);
        }

        runTime = RandomHelper.Date(runTime);
        using (var dbContext = new MemCheckDbContext(db))
            await new MoveCardToHeap(dbContext.AsCallContext(), runTime).RunAsync(new MoveCardToHeap.Request(user, deck, card.Id, CardInDeck.UnknownHeap));
        using (var dbContext = new MemCheckDbContext(db))
        {
            var cardInDeck = dbContext.CardsInDecks.Single();
            Assert.AreEqual(CardInDeck.UnknownHeap, cardInDeck.CurrentHeap);
            Assert.AreEqual(runTime, cardInDeck.LastLearnUtcTime);
            Assert.AreEqual(2, cardInDeck.NbTimesInNotLearnedHeap);
            Assert.AreEqual(4, cardInDeck.BiggestHeapReached);
            Assert.AreEqual(DateTime.MinValue, cardInDeck.ExpiryUtcTime);
        }
    }
    [TestMethod()]
    public async Task LearnMoves()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);
        var card = await CardHelper.CreateAsync(db, user);
        await DeckHelper.AddCardAsync(db, deck, card.Id, heap: 0);

        var runTime = RandomHelper.Date();
        using (var dbContext = new MemCheckDbContext(db))
            await new MoveCardToHeap(dbContext.AsCallContext(), runTime).RunAsync(new MoveCardToHeap.Request(user, deck, card.Id, 1));
        using (var dbContext = new MemCheckDbContext(db))
        {
            var cardInDeck = dbContext.CardsInDecks.Single();
            Assert.AreEqual(1, cardInDeck.CurrentHeap);
            Assert.AreEqual(runTime, cardInDeck.LastLearnUtcTime);
            Assert.AreEqual(1, cardInDeck.NbTimesInNotLearnedHeap);
            Assert.AreEqual(1, cardInDeck.BiggestHeapReached);
            Assert.AreEqual(runTime.AddDays(1), cardInDeck.ExpiryUtcTime);
        }

        runTime = RandomHelper.Date(runTime);
        using (var dbContext = new MemCheckDbContext(db))
            await new MoveCardToHeap(dbContext.AsCallContext(), runTime).RunAsync(new MoveCardToHeap.Request(user, deck, card.Id, 2));
        using (var dbContext = new MemCheckDbContext(db))
        {
            var cardInDeck = dbContext.CardsInDecks.Single();
            Assert.AreEqual(2, cardInDeck.CurrentHeap);
            Assert.AreEqual(runTime, cardInDeck.LastLearnUtcTime);
            Assert.AreEqual(1, cardInDeck.NbTimesInNotLearnedHeap);
            Assert.AreEqual(2, cardInDeck.BiggestHeapReached);
            Assert.AreEqual(runTime.AddDays(2), cardInDeck.ExpiryUtcTime);
        }

        runTime = RandomHelper.Date(runTime);
        using (var dbContext = new MemCheckDbContext(db))
            await new MoveCardToHeap(dbContext.AsCallContext(), runTime).RunAsync(new MoveCardToHeap.Request(user, deck, card.Id, CardInDeck.UnknownHeap));
        using (var dbContext = new MemCheckDbContext(db))
        {
            var cardInDeck = dbContext.CardsInDecks.Single();
            Assert.AreEqual(CardInDeck.UnknownHeap, cardInDeck.CurrentHeap);
            Assert.AreEqual(runTime, cardInDeck.LastLearnUtcTime);
            Assert.AreEqual(2, cardInDeck.NbTimesInNotLearnedHeap);
            Assert.AreEqual(2, cardInDeck.BiggestHeapReached);
            Assert.AreEqual(DateTime.MinValue, cardInDeck.ExpiryUtcTime);
        }

        runTime = RandomHelper.Date(runTime);
        using (var dbContext = new MemCheckDbContext(db))
            await new MoveCardToHeap(dbContext.AsCallContext(), runTime).RunAsync(new MoveCardToHeap.Request(user, deck, card.Id, CardInDeck.UnknownHeap));
        using (var dbContext = new MemCheckDbContext(db))
        {
            var cardInDeck = dbContext.CardsInDecks.Single();
            Assert.AreEqual(CardInDeck.UnknownHeap, cardInDeck.CurrentHeap);
            Assert.AreEqual(runTime, cardInDeck.LastLearnUtcTime);
            Assert.AreEqual(2, cardInDeck.NbTimesInNotLearnedHeap);
            Assert.AreEqual(2, cardInDeck.BiggestHeapReached);
            Assert.AreEqual(DateTime.MinValue, cardInDeck.ExpiryUtcTime);
        }
    }
}

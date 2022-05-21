using MemCheck.Application.Helpers;
using MemCheck.Basics;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Heaping;

[TestClass()]
public class MoveCardsToHeapTests
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
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new MoveCardsToHeap(dbContext.AsCallContext()).RunAsync(new MoveCardsToHeap.Request(Guid.Empty, deck, RandomHelper.Heap(), card.Id.AsArray())));
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
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new MoveCardsToHeap(dbContext.AsCallContext()).RunAsync(new MoveCardsToHeap.Request(Guid.NewGuid(), deck, RandomHelper.Heap(), card.Id.AsArray())));
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
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new MoveCardsToHeap(dbContext.AsCallContext()).RunAsync(new MoveCardsToHeap.Request(user, Guid.NewGuid(), RandomHelper.Heap(), card.Id.AsArray())));
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
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new MoveCardsToHeap(dbContext.AsCallContext()).RunAsync(new MoveCardsToHeap.Request(otherUser, deck, RandomHelper.Heap(), card.Id.AsArray())));
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
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new MoveCardsToHeap(dbContext.AsCallContext()).RunAsync(new MoveCardsToHeap.Request(user, deck, RandomHelper.Heap(), new[] { card.Id, Guid.NewGuid() })));
    }
    [TestMethod()]
    public async Task CardNotInDeck()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);
        var cardInDeck = await CardHelper.CreateAsync(db, user);
        await DeckHelper.AddCardAsync(db, deck, cardInDeck.Id);
        var cardNotInDeck = await CardHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new MoveCardsToHeap(dbContext.AsCallContext()).RunAsync(new MoveCardsToHeap.Request(user, deck, RandomHelper.Heap(), new[] { cardInDeck.Id, cardNotInDeck.Id })));
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
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new MoveCardsToHeap(dbContext.AsCallContext()).RunAsync(new MoveCardsToHeap.Request(user, deck, -1, card.Id.AsArray())));
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
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new MoveCardsToHeap(dbContext.AsCallContext()).RunAsync(new MoveCardsToHeap.Request(user, deck, CardInDeck.MaxHeapValue + 1, card.Id.AsArray())));
    }
    [TestMethod()]
    public async Task MoveMultiple()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);
        var card1 = await CardHelper.CreateAsync(db, user);
        var card2 = await CardHelper.CreateAsync(db, user);
        var card3 = await CardHelper.CreateAsync(db, user);
        var card1LastLearnTime = RandomHelper.Date();
        var card2LastLearnTime = RandomHelper.Date();
        var card3LastLearnTime = RandomHelper.Date();
        await DeckHelper.AddCardAsync(db, deck, card1.Id, heap: 1, lastLearnUtcTime: card1LastLearnTime);
        await DeckHelper.AddCardAsync(db, deck, card2.Id, heap: 8, lastLearnUtcTime: card2LastLearnTime);
        await DeckHelper.AddCardAsync(db, deck, card3.Id, heap: CardInDeck.UnknownHeap, lastLearnUtcTime: card3LastLearnTime);

        using (var dbContext = new MemCheckDbContext(db))
            await new MoveCardsToHeap(dbContext.AsCallContext()).RunAsync(new MoveCardsToHeap.Request(user, deck, 3, new[] { card1.Id, card2.Id, card3.Id }));

        using (var dbContext = new MemCheckDbContext(db))
        {
            var loadedCard1 = dbContext.CardsInDecks.Single(c => c.CardId == card1.Id);
            Assert.AreEqual(3, loadedCard1.CurrentHeap);
            Assert.AreEqual(card1LastLearnTime, loadedCard1.LastLearnUtcTime);
            Assert.AreEqual(card1LastLearnTime.AddDays(3), loadedCard1.ExpiryUtcTime);
            Assert.AreEqual(1, loadedCard1.NbTimesInNotLearnedHeap);
            Assert.AreEqual(3, loadedCard1.BiggestHeapReached);

            var loadedCard2 = dbContext.CardsInDecks.Single(c => c.CardId == card2.Id);
            Assert.AreEqual(3, loadedCard2.CurrentHeap);
            Assert.AreEqual(card2LastLearnTime, loadedCard2.LastLearnUtcTime);
            Assert.AreEqual(card2LastLearnTime.AddDays(3), loadedCard2.ExpiryUtcTime);
            Assert.AreEqual(1, loadedCard2.NbTimesInNotLearnedHeap);
            Assert.AreEqual(8, loadedCard2.BiggestHeapReached);

            var loadedCard3 = dbContext.CardsInDecks.Single(c => c.CardId == card3.Id);
            Assert.AreEqual(3, loadedCard3.CurrentHeap);
            Assert.AreEqual(card3LastLearnTime, loadedCard3.LastLearnUtcTime);
            Assert.AreEqual(card3LastLearnTime.AddDays(3), loadedCard3.ExpiryUtcTime);
            Assert.AreEqual(1, loadedCard3.NbTimesInNotLearnedHeap);
            Assert.AreEqual(3, loadedCard3.BiggestHeapReached);
        }
    }
    [TestMethod()]
    public async Task MoveToUnknown()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);
        var card1 = await CardHelper.CreateAsync(db, user);
        var card2 = await CardHelper.CreateAsync(db, user);
        var card1AddTime = RandomHelper.Date();
        var card2AddTime = RandomHelper.Date();
        await DeckHelper.AddCardAsync(db, deck, card1.Id, heap: CardInDeck.UnknownHeap, lastLearnUtcTime: card1AddTime);
        await DeckHelper.AddCardAsync(db, deck, card2.Id, heap: 8, lastLearnUtcTime: card2AddTime);

        using (var dbContext = new MemCheckDbContext(db))
            await new MoveCardsToHeap(dbContext.AsCallContext()).RunAsync(new MoveCardsToHeap.Request(user, deck, CardInDeck.UnknownHeap, new[] { card1.Id, card2.Id }));

        using (var dbContext = new MemCheckDbContext(db))
        {
            var loadedCard1 = dbContext.CardsInDecks.Single(c => c.CardId == card1.Id);
            Assert.AreEqual(CardInDeck.UnknownHeap, loadedCard1.CurrentHeap);
            Assert.AreEqual(card1AddTime, loadedCard1.LastLearnUtcTime);
            Assert.AreEqual(DateTime.MinValue, loadedCard1.ExpiryUtcTime);
            Assert.AreEqual(1, loadedCard1.NbTimesInNotLearnedHeap);
            Assert.AreEqual(CardInDeck.UnknownHeap, loadedCard1.BiggestHeapReached);

            var loadedCard2 = dbContext.CardsInDecks.Single(c => c.CardId == card2.Id);
            Assert.AreEqual(CardInDeck.UnknownHeap, loadedCard2.CurrentHeap);
            Assert.AreEqual(card2AddTime, loadedCard2.LastLearnUtcTime);
            Assert.AreEqual(DateTime.MinValue, loadedCard2.ExpiryUtcTime);
            Assert.AreEqual(2, loadedCard2.NbTimesInNotLearnedHeap);
            Assert.AreEqual(8, loadedCard2.BiggestHeapReached);
        }
    }
    [TestMethod()]
    public async Task MoveUp()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);
        var card = await CardHelper.CreateAsync(db, user);
        var lastLearnTime = RandomHelper.Date();
        await DeckHelper.AddCardAsync(db, deck, card.Id, heap: 1, lastLearnUtcTime: lastLearnTime);

        using (var dbContext = new MemCheckDbContext(db))
            await new MoveCardsToHeap(dbContext.AsCallContext()).RunAsync(new MoveCardsToHeap.Request(user, deck, 3, card.Id.AsArray()));

        using (var dbContext = new MemCheckDbContext(db))
        {
            var cardInDeck = dbContext.CardsInDecks.Single();
            Assert.AreEqual(3, cardInDeck.CurrentHeap);
            Assert.AreEqual(lastLearnTime, cardInDeck.LastLearnUtcTime);
            Assert.AreEqual(lastLearnTime.AddDays(3), cardInDeck.ExpiryUtcTime);
            Assert.AreEqual(1, cardInDeck.NbTimesInNotLearnedHeap);
            Assert.AreEqual(3, cardInDeck.BiggestHeapReached);
        }
    }
    [TestMethod()]
    public async Task MoveDown()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);
        var card = await CardHelper.CreateAsync(db, user);
        var lastLearnTime = RandomHelper.Date();
        await DeckHelper.AddCardAsync(db, deck, card.Id, heap: 4, lastLearnUtcTime: lastLearnTime);

        using (var dbContext = new MemCheckDbContext(db))
            await new MoveCardsToHeap(dbContext.AsCallContext()).RunAsync(new MoveCardsToHeap.Request(user, deck, 3, card.Id.AsArray()));

        using (var dbContext = new MemCheckDbContext(db))
        {
            var cardInDeck = dbContext.CardsInDecks.Single();
            Assert.AreEqual(3, cardInDeck.CurrentHeap);
            Assert.AreEqual(lastLearnTime, cardInDeck.LastLearnUtcTime);
            Assert.AreEqual(lastLearnTime.AddDays(3), cardInDeck.ExpiryUtcTime);
            Assert.AreEqual(1, cardInDeck.NbTimesInNotLearnedHeap);
            Assert.AreEqual(4, cardInDeck.BiggestHeapReached);
        }
    }
    [TestMethod()]
    public async Task MoveToSameHeap()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);
        var card = await CardHelper.CreateAsync(db, user);
        var lastLearnTime = RandomHelper.Date();
        await DeckHelper.AddCardAsync(db, deck, card.Id, heap: 1, lastLearnUtcTime: lastLearnTime);

        using (var dbContext = new MemCheckDbContext(db))
            await new MoveCardsToHeap(dbContext.AsCallContext()).RunAsync(new MoveCardsToHeap.Request(user, deck, 1, card.Id.AsArray()));

        using (var dbContext = new MemCheckDbContext(db))
        {
            var cardInDeck = dbContext.CardsInDecks.Single();
            Assert.AreEqual(1, cardInDeck.CurrentHeap);
            Assert.AreEqual(lastLearnTime, cardInDeck.LastLearnUtcTime);
            Assert.AreEqual(lastLearnTime.AddDays(1), cardInDeck.ExpiryUtcTime);
            Assert.AreEqual(1, cardInDeck.NbTimesInNotLearnedHeap);
            Assert.AreEqual(1, cardInDeck.BiggestHeapReached);
        }
    }
    [TestMethod()]
    public async Task MoveFromUnknownToUnknown()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);
        var card = await CardHelper.CreateAsync(db, user);
        var initialTime = RandomHelper.Date();
        await DeckHelper.AddCardAsync(db, deck, card.Id, heap: 4, lastLearnUtcTime: initialTime);

        using (var dbContext = new MemCheckDbContext(db))
            await new MoveCardsToHeap(dbContext.AsCallContext()).RunAsync(new MoveCardsToHeap.Request(user, deck, CardInDeck.UnknownHeap, card.Id.AsArray()));
        using (var dbContext = new MemCheckDbContext(db))
        {
            var cardInDeck = dbContext.CardsInDecks.Single();
            Assert.AreEqual(CardInDeck.UnknownHeap, cardInDeck.CurrentHeap);
            Assert.AreEqual(initialTime, cardInDeck.LastLearnUtcTime);
            Assert.AreEqual(DateTime.MinValue, cardInDeck.ExpiryUtcTime);
            Assert.AreEqual(2, cardInDeck.NbTimesInNotLearnedHeap);
            Assert.AreEqual(4, cardInDeck.BiggestHeapReached);
            Assert.AreEqual(DateTime.MinValue, cardInDeck.ExpiryUtcTime);
        }

        using (var dbContext = new MemCheckDbContext(db))
            await new MoveCardsToHeap(dbContext.AsCallContext()).RunAsync(new MoveCardsToHeap.Request(user, deck, CardInDeck.UnknownHeap, card.Id.AsArray()));
        using (var dbContext = new MemCheckDbContext(db))
        {
            var cardInDeck = dbContext.CardsInDecks.Single();
            Assert.AreEqual(CardInDeck.UnknownHeap, cardInDeck.CurrentHeap);
            Assert.AreEqual(initialTime, cardInDeck.LastLearnUtcTime);
            Assert.AreEqual(DateTime.MinValue, cardInDeck.ExpiryUtcTime);
            Assert.AreEqual(2, cardInDeck.NbTimesInNotLearnedHeap);
            Assert.AreEqual(4, cardInDeck.BiggestHeapReached);
            Assert.AreEqual(DateTime.MinValue, cardInDeck.ExpiryUtcTime);
        }
    }
    [TestMethod()]
    public async Task MoveToZeroTwice()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);
        var card = await CardHelper.CreateAsync(db, user);
        var addTime = RandomHelper.Date();
        await DeckHelper.AddCardAsync(db, deck, card.Id, heap: 8, lastLearnUtcTime: addTime);

        using (var dbContext = new MemCheckDbContext(db))
            await new MoveCardsToHeap(dbContext.AsCallContext()).RunAsync(new MoveCardsToHeap.Request(user, deck, CardInDeck.UnknownHeap, card.Id.AsArray()));

        for (int i = 0; i < 2; i++)
            using (var dbContext = new MemCheckDbContext(db))
            {
                var loadedCard = dbContext.CardsInDecks.Single();
                Assert.AreEqual(CardInDeck.UnknownHeap, loadedCard.CurrentHeap);
                Assert.AreEqual(addTime, loadedCard.LastLearnUtcTime);
                Assert.AreEqual(DateTime.MinValue, loadedCard.ExpiryUtcTime);
                Assert.AreEqual(2, loadedCard.NbTimesInNotLearnedHeap);
                Assert.AreEqual(8, loadedCard.BiggestHeapReached);
            }
    }
}

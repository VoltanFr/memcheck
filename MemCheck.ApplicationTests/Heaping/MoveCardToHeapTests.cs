﻿using MemCheck.Application.Tests.Helpers;
using MemCheck.Basics;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Heaping
{
    [TestClass()]
    public class MoveCardToHeapTests
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
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new MoveCardToHeap(dbContext).RunAsync(new MoveCardToHeap.Request(Guid.Empty, deck, card.Id, RandomHelper.Heap(), false)));
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
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new MoveCardToHeap(dbContext).RunAsync(new MoveCardToHeap.Request(Guid.NewGuid(), deck, card.Id, RandomHelper.Heap(), false)));
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
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new MoveCardToHeap(dbContext).RunAsync(new MoveCardToHeap.Request(user, Guid.NewGuid(), card.Id, RandomHelper.Heap(), false)));
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
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new MoveCardToHeap(dbContext).RunAsync(new MoveCardToHeap.Request(otherUser, deck, card.Id, RandomHelper.Heap(), false)));
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
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new MoveCardToHeap(dbContext).RunAsync(new MoveCardToHeap.Request(user, deck, Guid.NewGuid(), RandomHelper.Heap(), false)));
        }
        [TestMethod()]
        public async Task CardNotInDeck()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);
            var card = await CardHelper.CreateAsync(db, user);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new MoveCardToHeap(dbContext).RunAsync(new MoveCardToHeap.Request(user, deck, card.Id, RandomHelper.Heap(), false)));
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
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new MoveCardToHeap(dbContext).RunAsync(new MoveCardToHeap.Request(user, deck, card.Id, -1, false)));
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
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new MoveCardToHeap(dbContext).RunAsync(new MoveCardToHeap.Request(user, deck, card.Id, CardInDeck.MaxHeapValue + 1, false)));
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
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new MoveCardToHeap(dbContext).RunAsync(new MoveCardToHeap.Request(user, deck, card.Id, 3, false)));
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
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new MoveCardToHeap(dbContext).RunAsync(new MoveCardToHeap.Request(user, deck, card.Id, 3, false)));
        }
        [TestMethod()]
        public async Task LearnMoveUp()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);
            var card = await CardHelper.CreateAsync(db, user);
            await DeckHelper.AddCardAsync(db, deck, card.Id, heap: 1);
            var runTime = RandomHelper.Date();

            using (var dbContext = new MemCheckDbContext(db))
                await new MoveCardToHeap(dbContext).RunAsync(new MoveCardToHeap.Request(user, deck, card.Id, 2, false), runTime);

            using (var dbContext = new MemCheckDbContext(db))
            {
                var cardInDeck = dbContext.CardsInDecks.Single();
                Assert.AreEqual(2, cardInDeck.CurrentHeap);
                Assert.AreEqual(runTime, cardInDeck.LastLearnUtcTime);
                Assert.AreEqual(1, cardInDeck.NbTimesInNotLearnedHeap);
                Assert.AreEqual(2, cardInDeck.BiggestHeapReached);
            }
        }
        [TestMethod()]
        public async Task LearnMoveToSameHeap()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);
            var card = await CardHelper.CreateAsync(db, user);
            var initialTime = RandomHelper.Date();
            await DeckHelper.AddCardAsync(db, deck, card.Id, heap: 1, lastLearnUtcTime: initialTime);

            using (var dbContext = new MemCheckDbContext(db))
                await new MoveCardToHeap(dbContext).RunAsync(new MoveCardToHeap.Request(user, deck, card.Id, 1, false), RandomHelper.Date(initialTime));

            using (var dbContext = new MemCheckDbContext(db))
            {
                var cardInDeck = dbContext.CardsInDecks.Single();
                Assert.AreEqual(1, cardInDeck.CurrentHeap);
                Assert.AreEqual(initialTime, cardInDeck.LastLearnUtcTime);
                Assert.AreEqual(1, cardInDeck.NbTimesInNotLearnedHeap);
                Assert.AreEqual(1, cardInDeck.BiggestHeapReached);
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
                await new MoveCardToHeap(dbContext).RunAsync(new MoveCardToHeap.Request(user, deck, card.Id, CardInDeck.UnknownHeap, false), runTime);

            using (var dbContext = new MemCheckDbContext(db))
            {
                var cardInDeck = dbContext.CardsInDecks.Single();
                Assert.AreEqual(CardInDeck.UnknownHeap, cardInDeck.CurrentHeap);
                Assert.AreEqual(runTime, cardInDeck.LastLearnUtcTime);
                Assert.AreEqual(2, cardInDeck.NbTimesInNotLearnedHeap);
                Assert.AreEqual(4, cardInDeck.BiggestHeapReached);
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
                await new MoveCardToHeap(dbContext).RunAsync(new MoveCardToHeap.Request(user, deck, card.Id, CardInDeck.UnknownHeap, false), runTime);
            using (var dbContext = new MemCheckDbContext(db))
            {
                var cardInDeck = dbContext.CardsInDecks.Single();
                Assert.AreEqual(CardInDeck.UnknownHeap, cardInDeck.CurrentHeap);
                Assert.AreEqual(runTime, cardInDeck.LastLearnUtcTime);
                Assert.AreEqual(2, cardInDeck.NbTimesInNotLearnedHeap);
                Assert.AreEqual(4, cardInDeck.BiggestHeapReached);
            }

            runTime = RandomHelper.Date(runTime);
            using (var dbContext = new MemCheckDbContext(db))
                await new MoveCardToHeap(dbContext).RunAsync(new MoveCardToHeap.Request(user, deck, card.Id, CardInDeck.UnknownHeap, false), runTime);
            using (var dbContext = new MemCheckDbContext(db))
            {
                var cardInDeck = dbContext.CardsInDecks.Single();
                Assert.AreEqual(CardInDeck.UnknownHeap, cardInDeck.CurrentHeap);
                Assert.AreEqual(runTime, cardInDeck.LastLearnUtcTime);
                Assert.AreEqual(2, cardInDeck.NbTimesInNotLearnedHeap);
                Assert.AreEqual(4, cardInDeck.BiggestHeapReached);
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
                await new MoveCardToHeap(dbContext).RunAsync(new MoveCardToHeap.Request(user, deck, card.Id, 1, false), runTime);
            using (var dbContext = new MemCheckDbContext(db))
            {
                var cardInDeck = dbContext.CardsInDecks.Single();
                Assert.AreEqual(1, cardInDeck.CurrentHeap);
                Assert.AreEqual(runTime, cardInDeck.LastLearnUtcTime);
                Assert.AreEqual(1, cardInDeck.NbTimesInNotLearnedHeap);
                Assert.AreEqual(1, cardInDeck.BiggestHeapReached);
            }

            runTime = RandomHelper.Date(runTime);
            using (var dbContext = new MemCheckDbContext(db))
                await new MoveCardToHeap(dbContext).RunAsync(new MoveCardToHeap.Request(user, deck, card.Id, 2, false), runTime);
            using (var dbContext = new MemCheckDbContext(db))
            {
                var cardInDeck = dbContext.CardsInDecks.Single();
                Assert.AreEqual(2, cardInDeck.CurrentHeap);
                Assert.AreEqual(runTime, cardInDeck.LastLearnUtcTime);
                Assert.AreEqual(1, cardInDeck.NbTimesInNotLearnedHeap);
                Assert.AreEqual(2, cardInDeck.BiggestHeapReached);
            }

            runTime = RandomHelper.Date(runTime);
            using (var dbContext = new MemCheckDbContext(db))
                await new MoveCardToHeap(dbContext).RunAsync(new MoveCardToHeap.Request(user, deck, card.Id, CardInDeck.UnknownHeap, false), runTime);
            using (var dbContext = new MemCheckDbContext(db))
            {
                var cardInDeck = dbContext.CardsInDecks.Single();
                Assert.AreEqual(CardInDeck.UnknownHeap, cardInDeck.CurrentHeap);
                Assert.AreEqual(runTime, cardInDeck.LastLearnUtcTime);
                Assert.AreEqual(2, cardInDeck.NbTimesInNotLearnedHeap);
                Assert.AreEqual(2, cardInDeck.BiggestHeapReached);
            }

            runTime = RandomHelper.Date(runTime);
            using (var dbContext = new MemCheckDbContext(db))
                await new MoveCardToHeap(dbContext).RunAsync(new MoveCardToHeap.Request(user, deck, card.Id, CardInDeck.UnknownHeap, false), runTime);
            using (var dbContext = new MemCheckDbContext(db))
            {
                var cardInDeck = dbContext.CardsInDecks.Single();
                Assert.AreEqual(CardInDeck.UnknownHeap, cardInDeck.CurrentHeap);
                Assert.AreEqual(runTime, cardInDeck.LastLearnUtcTime);
                Assert.AreEqual(2, cardInDeck.NbTimesInNotLearnedHeap);
                Assert.AreEqual(2, cardInDeck.BiggestHeapReached);
            }
        }
        [TestMethod()]
        public async Task ManualMoveUpMoreThanOneHeap()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);
            var card = await CardHelper.CreateAsync(db, user);
            var initialTime = RandomHelper.Date();
            await DeckHelper.AddCardAsync(db, deck, card.Id, heap: 1, lastLearnUtcTime: initialTime);

            using (var dbContext = new MemCheckDbContext(db))
                await new MoveCardToHeap(dbContext).RunAsync(new MoveCardToHeap.Request(user, deck, card.Id, 3, true), RandomHelper.Date(initialTime));

            using (var dbContext = new MemCheckDbContext(db))
            {
                var cardInDeck = dbContext.CardsInDecks.Single();
                Assert.AreEqual(3, cardInDeck.CurrentHeap);
                Assert.AreEqual(initialTime, cardInDeck.LastLearnUtcTime);
                Assert.AreEqual(1, cardInDeck.NbTimesInNotLearnedHeap);
                Assert.AreEqual(3, cardInDeck.BiggestHeapReached);
            }
        }
        [TestMethod()]
        public async Task ManualMoveDown()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);
            var card = await CardHelper.CreateAsync(db, user);
            var initialTime = RandomHelper.Date();
            await DeckHelper.AddCardAsync(db, deck, card.Id, heap: 4, lastLearnUtcTime: initialTime);

            using (var dbContext = new MemCheckDbContext(db))
                await new MoveCardToHeap(dbContext).RunAsync(new MoveCardToHeap.Request(user, deck, card.Id, 3, true), RandomHelper.Date(initialTime));

            using (var dbContext = new MemCheckDbContext(db))
            {
                var cardInDeck = dbContext.CardsInDecks.Single();
                Assert.AreEqual(3, cardInDeck.CurrentHeap);
                Assert.AreEqual(initialTime, cardInDeck.LastLearnUtcTime);
                Assert.AreEqual(1, cardInDeck.NbTimesInNotLearnedHeap);
                Assert.AreEqual(4, cardInDeck.BiggestHeapReached);
            }
        }
        [TestMethod()]
        public async Task ManualMoveUp()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);
            var card = await CardHelper.CreateAsync(db, user);
            var initialTime = RandomHelper.Date();
            await DeckHelper.AddCardAsync(db, deck, card.Id, heap: 1, lastLearnUtcTime: initialTime);

            using (var dbContext = new MemCheckDbContext(db))
                await new MoveCardToHeap(dbContext).RunAsync(new MoveCardToHeap.Request(user, deck, card.Id, 2, true), RandomHelper.Date(initialTime));

            using (var dbContext = new MemCheckDbContext(db))
            {
                var cardInDeck = dbContext.CardsInDecks.Single();
                Assert.AreEqual(2, cardInDeck.CurrentHeap);
                Assert.AreEqual(initialTime, cardInDeck.LastLearnUtcTime);
                Assert.AreEqual(1, cardInDeck.NbTimesInNotLearnedHeap);
                Assert.AreEqual(2, cardInDeck.BiggestHeapReached);
            }
        }
        [TestMethod()]
        public async Task ManuelMoveToSameHeap()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);
            var card = await CardHelper.CreateAsync(db, user);
            var initialTime = RandomHelper.Date();
            await DeckHelper.AddCardAsync(db, deck, card.Id, heap: 1, lastLearnUtcTime: initialTime);

            using (var dbContext = new MemCheckDbContext(db))
                await new MoveCardToHeap(dbContext).RunAsync(new MoveCardToHeap.Request(user, deck, card.Id, 1, true), RandomHelper.Date(initialTime));

            using (var dbContext = new MemCheckDbContext(db))
            {
                var cardInDeck = dbContext.CardsInDecks.Single();
                Assert.AreEqual(1, cardInDeck.CurrentHeap);
                Assert.AreEqual(initialTime, cardInDeck.LastLearnUtcTime);
                Assert.AreEqual(1, cardInDeck.NbTimesInNotLearnedHeap);
                Assert.AreEqual(1, cardInDeck.BiggestHeapReached);
            }
        }
        [TestMethod()]
        public async Task ManualMoveToUnknown()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);
            var card = await CardHelper.CreateAsync(db, user);
            var initialTime = RandomHelper.Date();
            await DeckHelper.AddCardAsync(db, deck, card.Id, heap: 4, lastLearnUtcTime: initialTime);

            using (var dbContext = new MemCheckDbContext(db))
                await new MoveCardToHeap(dbContext).RunAsync(new MoveCardToHeap.Request(user, deck, card.Id, CardInDeck.UnknownHeap, true), RandomHelper.Date(initialTime));

            using (var dbContext = new MemCheckDbContext(db))
            {
                var cardInDeck = dbContext.CardsInDecks.Single();
                Assert.AreEqual(CardInDeck.UnknownHeap, cardInDeck.CurrentHeap);
                Assert.AreEqual(initialTime, cardInDeck.LastLearnUtcTime);
                Assert.AreEqual(2, cardInDeck.NbTimesInNotLearnedHeap);
                Assert.AreEqual(4, cardInDeck.BiggestHeapReached);
            }
        }
        [TestMethod()]
        public async Task ManualMoveFromUnknownToUnknown()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);
            var card = await CardHelper.CreateAsync(db, user);
            var initialTime = RandomHelper.Date();
            await DeckHelper.AddCardAsync(db, deck, card.Id, heap: 4, lastLearnUtcTime: initialTime);

            var runTime = RandomHelper.Date(initialTime);
            using (var dbContext = new MemCheckDbContext(db))
                await new MoveCardToHeap(dbContext).RunAsync(new MoveCardToHeap.Request(user, deck, card.Id, CardInDeck.UnknownHeap, true), runTime);
            using (var dbContext = new MemCheckDbContext(db))
            {
                var cardInDeck = dbContext.CardsInDecks.Single();
                Assert.AreEqual(CardInDeck.UnknownHeap, cardInDeck.CurrentHeap);
                Assert.AreEqual(initialTime, cardInDeck.LastLearnUtcTime);
                Assert.AreEqual(2, cardInDeck.NbTimesInNotLearnedHeap);
                Assert.AreEqual(4, cardInDeck.BiggestHeapReached);
            }

            runTime = RandomHelper.Date(runTime);
            using (var dbContext = new MemCheckDbContext(db))
                await new MoveCardToHeap(dbContext).RunAsync(new MoveCardToHeap.Request(user, deck, card.Id, CardInDeck.UnknownHeap, true), runTime);
            using (var dbContext = new MemCheckDbContext(db))
            {
                var cardInDeck = dbContext.CardsInDecks.Single();
                Assert.AreEqual(CardInDeck.UnknownHeap, cardInDeck.CurrentHeap);
                Assert.AreEqual(initialTime, cardInDeck.LastLearnUtcTime);
                Assert.AreEqual(2, cardInDeck.NbTimesInNotLearnedHeap);
                Assert.AreEqual(4, cardInDeck.BiggestHeapReached);
            }
        }
        [TestMethod()]
        public async Task ManualMoves()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);
            var card = await CardHelper.CreateAsync(db, user);
            var initialTime = RandomHelper.Date();
            await DeckHelper.AddCardAsync(db, deck, card.Id, heap: 0, lastLearnUtcTime: initialTime);

            var runTime = RandomHelper.Date(initialTime);
            using (var dbContext = new MemCheckDbContext(db))
                await new MoveCardToHeap(dbContext).RunAsync(new MoveCardToHeap.Request(user, deck, card.Id, 1, false), runTime);

            using (var dbContext = new MemCheckDbContext(db))
                await new MoveCardToHeap(dbContext).RunAsync(new MoveCardToHeap.Request(user, deck, card.Id, CardInDeck.UnknownHeap, true), RandomHelper.Date(runTime));
            using (var dbContext = new MemCheckDbContext(db))
            {
                var cardInDeck = dbContext.CardsInDecks.Single();
                Assert.AreEqual(CardInDeck.UnknownHeap, cardInDeck.CurrentHeap);
                Assert.AreEqual(runTime, cardInDeck.LastLearnUtcTime);
                Assert.AreEqual(2, cardInDeck.NbTimesInNotLearnedHeap);
                Assert.AreEqual(1, cardInDeck.BiggestHeapReached);
            }

            using (var dbContext = new MemCheckDbContext(db))
                await new MoveCardToHeap(dbContext).RunAsync(new MoveCardToHeap.Request(user, deck, card.Id, CardInDeck.UnknownHeap, true), RandomHelper.Date(runTime));
            using (var dbContext = new MemCheckDbContext(db))
            {
                var cardInDeck = dbContext.CardsInDecks.Single();
                Assert.AreEqual(CardInDeck.UnknownHeap, cardInDeck.CurrentHeap);
                Assert.AreEqual(runTime, cardInDeck.LastLearnUtcTime);
                Assert.AreEqual(2, cardInDeck.NbTimesInNotLearnedHeap);
                Assert.AreEqual(1, cardInDeck.BiggestHeapReached);
            }
        }
    }
}

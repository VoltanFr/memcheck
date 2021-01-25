using MemCheck.Application.Heaping;
using MemCheck.Application.QueryValidation;
using MemCheck.Application.Tests.Helpers;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Loading
{
    [TestClass()]
    public class GetUnknownCardsToLearnTests
    {
        [TestMethod()]
        public async Task UserNotLoggedIn()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);

            using var dbContext = new MemCheckDbContext(db);
            var request = new GetUnknownCardsToLearn.Request(Guid.Empty, deck, Array.Empty<Guid>(), Array.Empty<Guid>(), 10);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new GetUnknownCardsToLearn(dbContext).RunAsync(request));
        }
        [TestMethod()]
        public async Task UserDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);

            using var dbContext = new MemCheckDbContext(db);
            var request = new GetUnknownCardsToLearn.Request(Guid.NewGuid(), deck, Array.Empty<Guid>(), Array.Empty<Guid>(), 10);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new GetUnknownCardsToLearn(dbContext).RunAsync(request));
        }
        [TestMethod()]
        public async Task DeckDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            var request = new GetUnknownCardsToLearn.Request(user, Guid.NewGuid(), Array.Empty<Guid>(), Array.Empty<Guid>(), 10);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetUnknownCardsToLearn(dbContext).RunAsync(request));
        }
        [TestMethod()]
        public async Task UserNotOwner()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);
            var otherUser = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            var request = new GetUnknownCardsToLearn.Request(otherUser, deck, Array.Empty<Guid>(), Array.Empty<Guid>(), 10);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new GetUnknownCardsToLearn(dbContext).RunAsync(request));
        }
        [TestMethod()]
        public async Task OneCardToRepeat()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user, algorithmId: DefaultHeapingAlgorithm.ID);
            var card = await CardHelper.CreateAsync(db, user);
            await DeckHelper.AddCardAsync(db, deck, card.Id, 1, new DateTime(2000, 1, 1));

            using var dbContext = new MemCheckDbContext(db);
            var request = new GetUnknownCardsToLearn.Request(user, deck, Array.Empty<Guid>(), Array.Empty<Guid>(), 10);
            var cards = await new GetUnknownCardsToLearn(dbContext).RunAsync(request);
            Assert.IsFalse(cards.Any());
        }
        [TestMethod()]
        public async Task OneNeverLearnt()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user, algorithmId: DefaultHeapingAlgorithm.ID);
            var card = await CardHelper.CreateAsync(db, user);
            await DeckHelper.AddNeverLearntCardAsync(db, deck, card.Id);

            using var dbContext = new MemCheckDbContext(db);
            var request = new GetUnknownCardsToLearn.Request(user, deck, Array.Empty<Guid>(), Array.Empty<Guid>(), 10);
            var cards = await new GetUnknownCardsToLearn(dbContext).RunAsync(request);
            Assert.AreEqual(1, cards.Count());
        }
        [TestMethod()]
        public async Task OneLearnt()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user, algorithmId: DefaultHeapingAlgorithm.ID);
            var card = await CardHelper.CreateAsync(db, user);
            await DeckHelper.AddCardAsync(db, deck, card.Id, 0);

            using var dbContext = new MemCheckDbContext(db);
            var request = new GetUnknownCardsToLearn.Request(user, deck, Array.Empty<Guid>(), Array.Empty<Guid>(), 10);
            var cards = await new GetUnknownCardsToLearn(dbContext).RunAsync(request);
            Assert.AreEqual(1, cards.Count());
            Assert.AreNotEqual(CardInDeck.NeverLearntLastLearnTime, cards.First().LastLearnUtcTime);
        }
        [TestMethod()]
        public async Task CardsNeverLearnt_NotTheSameCardsOnSuccessiveRuns()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user, algorithmId: DefaultHeapingAlgorithm.ID);
            for (int i = 0; i < 100; i++)
            {
                var card = await CardHelper.CreateAsync(db, user);
                await DeckHelper.AddNeverLearntCardAsync(db, deck, card.Id);
            }
            using var dbContext = new MemCheckDbContext(db);
            const int requestCardCount = 10;
            var request = new GetUnknownCardsToLearn.Request(user, deck, Array.Empty<Guid>(), Array.Empty<Guid>(), requestCardCount);
            var firstRunCards = (await new GetUnknownCardsToLearn(dbContext).RunAsync(request)).Select(c => c.CardId).ToImmutableHashSet();
            Assert.AreEqual(requestCardCount, firstRunCards.Count);
            var secondRunCards = (await new GetUnknownCardsToLearn(dbContext).RunAsync(request)).Select(c => c.CardId).ToImmutableHashSet();
            Assert.AreEqual(requestCardCount, secondRunCards.Count);
            var thirdRunCards = (await new GetUnknownCardsToLearn(dbContext).RunAsync(request)).Select(c => c.CardId).ToImmutableHashSet();
            Assert.AreEqual(requestCardCount, thirdRunCards.Count);
            Assert.IsFalse(firstRunCards.SetEquals(secondRunCards));
            Assert.IsFalse(firstRunCards.SetEquals(thirdRunCards));
            Assert.IsFalse(secondRunCards.SetEquals(thirdRunCards));
        }
        [TestMethod()]
        public async Task CardsNeverLearnt_NotTheSameOrderOnSuccessiveRuns()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user, algorithmId: DefaultHeapingAlgorithm.ID);
            const int cardCount = 100;
            for (int i = 0; i < cardCount; i++)
            {
                var card = await CardHelper.CreateAsync(db, user);
                await DeckHelper.AddNeverLearntCardAsync(db, deck, card.Id);
            }
            using var dbContext = new MemCheckDbContext(db);
            var request = new GetUnknownCardsToLearn.Request(user, deck, Array.Empty<Guid>(), Array.Empty<Guid>(), cardCount);
            var firstRunCards = (await new GetUnknownCardsToLearn(dbContext).RunAsync(request)).Select(c => c.CardId).ToImmutableArray();
            Assert.AreEqual(cardCount, firstRunCards.Length);
            var secondRunCards = (await new GetUnknownCardsToLearn(dbContext).RunAsync(request)).Select(c => c.CardId).ToImmutableArray();
            Assert.AreEqual(cardCount, secondRunCards.Length);
            var thirdRunCards = (await new GetUnknownCardsToLearn(dbContext).RunAsync(request)).Select(c => c.CardId).ToImmutableArray();
            Assert.AreEqual(cardCount, thirdRunCards.Length);
            Assert.IsFalse(firstRunCards.SequenceEqual(secondRunCards));
            Assert.IsFalse(firstRunCards.SequenceEqual(thirdRunCards));
            Assert.IsFalse(secondRunCards.SequenceEqual(thirdRunCards));
            Assert.AreNotEqual(firstRunCards[0], secondRunCards[0]);
            Assert.AreNotEqual(firstRunCards[0], thirdRunCards[0]);
            Assert.AreNotEqual(secondRunCards[0], thirdRunCards[0]);
        }
        [TestMethod()]
        public async Task OneUnknownCardLearnt()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user, algorithmId: DefaultHeapingAlgorithm.ID);
            var card = await CardHelper.CreateAsync(db, user);
            await DeckHelper.AddCardAsync(db, deck, card.Id, 0);

            using var dbContext = new MemCheckDbContext(db);
            var request = new GetUnknownCardsToLearn.Request(user, deck, Array.Empty<Guid>(), Array.Empty<Guid>(), 10);
            var cards = await new GetUnknownCardsToLearn(dbContext).RunAsync(request);
            Assert.AreEqual(1, cards.Count());
        }
        [TestMethod()]
        public async Task UnknownCardsLearnt_CheckOrder()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user, algorithmId: DeveloperHeapingAlgorithm.ID);
            const int cardCount = 100;
            for (int i = 0; i < cardCount; i++)
            {
                var card = await CardHelper.CreateAsync(db, user);
                await DeckHelper.AddCardAsync(db, deck, card.Id, 0);
            }
            using var dbContext = new MemCheckDbContext(db);
            var request = new GetUnknownCardsToLearn.Request(user, deck, Array.Empty<Guid>(), Array.Empty<Guid>(), cardCount);
            var cards = (await new GetUnknownCardsToLearn(dbContext).RunAsync(request)).ToImmutableArray();
            Assert.AreEqual(cardCount, cards.Length);
            for (int i = 1; i < cards.Length; i++)
                Assert.IsTrue(cards[i].LastLearnUtcTime >= cards[i - 1].LastLearnUtcTime);
        }
        [TestMethod()]
        public async Task ComplexCase()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user, algorithmId: DeveloperHeapingAlgorithm.ID);
            const int cardCount = 100;
            for (int i = 0; i < cardCount; i++)
            {
                await DeckHelper.AddNeverLearntCardAsync(db, deck, await CardHelper.CreateIdAsync(db, user));
                await DeckHelper.AddCardAsync(db, deck, await CardHelper.CreateIdAsync(db, user), 0);
            }
            using var dbContext = new MemCheckDbContext(db);
            var request = new GetUnknownCardsToLearn.Request(user, deck, Array.Empty<Guid>(), Array.Empty<Guid>(), cardCount);
            var cards = (await new GetUnknownCardsToLearn(dbContext).RunAsync(request)).ToImmutableArray();
            Assert.AreEqual(cardCount, cards.Length);
            for (int i = 1; i < cardCount / 2; i++)
                Assert.AreEqual(CardInDeck.NeverLearntLastLearnTime, cards[i].LastLearnUtcTime);
            for (int i = cardCount / 2; i < cards.Length; i++)
                Assert.IsTrue(cards[i].LastLearnUtcTime >= cards[i - 1].LastLearnUtcTime);
        }
        [TestMethod()]
        public async Task ComplexCaseWithLessCardsThanRequested()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user, algorithmId: DeveloperHeapingAlgorithm.ID);
            const int cardCount = 10;
            for (int i = 0; i < cardCount / 2; i++)
            {
                await DeckHelper.AddNeverLearntCardAsync(db, deck, await CardHelper.CreateIdAsync(db, user));
                await DeckHelper.AddCardAsync(db, deck, await CardHelper.CreateIdAsync(db, user), 0);
            }
            using var dbContext = new MemCheckDbContext(db);
            var request = new GetUnknownCardsToLearn.Request(user, deck, Array.Empty<Guid>(), Array.Empty<Guid>(), cardCount * 2);
            var cards = (await new GetUnknownCardsToLearn(dbContext).RunAsync(request)).ToImmutableArray();
            Assert.AreEqual(cardCount, cards.Length);
            for (int i = 1; i < cardCount / 2; i++)
                Assert.AreEqual(CardInDeck.NeverLearntLastLearnTime, cards[i].LastLearnUtcTime);
            for (int i = cardCount / 2; i < cards.Length; i++)
                Assert.IsTrue(cards[i].LastLearnUtcTime >= cards[i - 1].LastLearnUtcTime);
        }
    }
}

using MemCheck.Application.Heaping;
using MemCheck.Application.QueryValidation;
using MemCheck.Application.Tests.Helpers;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Loading
{
    [TestClass()]
    public class GetCardsToLearnTests
    {
        [TestMethod()]
        public async Task UserNotLoggedIn()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);

            using var dbContext = new MemCheckDbContext(db);
            var request = new GetCardsToLearn.Request(Guid.Empty, deck, true, Array.Empty<Guid>(), Array.Empty<Guid>(), 10);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new GetCardsToLearn(dbContext).RunAsync(request));
        }
        [TestMethod()]
        public async Task UserDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);

            using var dbContext = new MemCheckDbContext(db);
            var request = new GetCardsToLearn.Request(Guid.NewGuid(), deck, true, Array.Empty<Guid>(), Array.Empty<Guid>(), 10);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new GetCardsToLearn(dbContext).RunAsync(request));
        }
        [TestMethod()]
        public async Task DeckDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            var request = new GetCardsToLearn.Request(user, Guid.NewGuid(), true, Array.Empty<Guid>(), Array.Empty<Guid>(), 10);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetCardsToLearn(dbContext).RunAsync(request));
        }
        [TestMethod()]
        public async Task UserNotOwner()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);
            var otherUser = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            var request = new GetCardsToLearn.Request(otherUser, deck, true, Array.Empty<Guid>(), Array.Empty<Guid>(), 10);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new GetCardsToLearn(dbContext).RunAsync(request));
        }
        [TestMethod()]
        public async Task Repeat_DeckContainsOneCardNonExpired()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user, algorithmId: DefaultHeapingAlgorithm.ID);
            var card = await CardHelper.CreateAsync(db, user);
            await DeckHelper.AddCardAsync(db, user, deck, card.Id, 1, new DateTime(2000, 1, 1));

            using var dbContext = new MemCheckDbContext(db);
            var request = new GetCardsToLearn.Request(user, deck, false, Array.Empty<Guid>(), Array.Empty<Guid>(), 10);
            var cards = await new GetCardsToLearn(dbContext).RunAsync(request, new DateTime(2000, 1, 2));
            Assert.IsFalse(cards.Any());
        }
        [TestMethod()]
        public async Task Repeat_DeckContainsOneCardExpired()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);
            var card = await CardHelper.CreateAsync(db, user);
            await DeckHelper.AddCardAsync(db, user, deck, card.Id, 1, new DateTime(2000, 1, 1));

            using var dbContext = new MemCheckDbContext(db);
            var request = new GetCardsToLearn.Request(user, deck, false, Array.Empty<Guid>(), Array.Empty<Guid>(), 10);
            var cards = await new GetCardsToLearn(dbContext).RunAsync(request, new DateTime(2000, 1, 4));
            Assert.AreEqual(card.Id, cards.Single().CardId);
        }
        [TestMethod()]
        public async Task Repeat_RequestedCount()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user, algorithmId: DeveloperHeapingAlgorithm.ID);
            var loadTime = DateHelper.Random();
            const int cardCount = 50;
            for (int i = 0; i < cardCount; i++)
            {
                var card = await CardHelper.CreateAsync(db, user);
                await DeckHelper.AddCardAsync(db, user, deck, card.Id, RandomHelper.Heap(true), RandomHelper.DateBefore(loadTime));
            }
            using var dbContext = new MemCheckDbContext(db);
            var request = new GetCardsToLearn.Request(user, deck, false, Array.Empty<Guid>(), Array.Empty<Guid>(), cardCount / 2);
            var cards = await new GetCardsToLearn(dbContext).RunAsync(request, loadTime);
            Assert.AreEqual(request.CardsToDownload, cards.Count());

            request = new GetCardsToLearn.Request(user, deck, false, Array.Empty<Guid>(), Array.Empty<Guid>(), cardCount);
            cards = await new GetCardsToLearn(dbContext).RunAsync(request, loadTime);
            Assert.AreEqual(request.CardsToDownload, cards.Count());

            request = new GetCardsToLearn.Request(user, deck, false, Array.Empty<Guid>(), Array.Empty<Guid>(), cardCount * 2);
            cards = await new GetCardsToLearn(dbContext).RunAsync(request, loadTime);
            Assert.AreEqual(cardCount, cards.Count());
        }
        [TestMethod()]
        public async Task Repeat_CheckOrder()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user, algorithmId: DeveloperHeapingAlgorithm.ID);
            var loadTime = DateHelper.Random();
            const int cardCount = 100;
            for (int i = 0; i < cardCount; i++)
            {
                var card = await CardHelper.CreateAsync(db, user);
                await DeckHelper.AddCardAsync(db, user, deck, card.Id, RandomHelper.Heap(true), RandomHelper.DateBefore(loadTime));
            }
            using var dbContext = new MemCheckDbContext(db);
            var request = new GetCardsToLearn.Request(user, deck, false, Array.Empty<Guid>(), Array.Empty<Guid>(), cardCount);
            var cards = (await new GetCardsToLearn(dbContext).RunAsync(request, loadTime)).ToImmutableArray();
            Assert.AreEqual(cardCount, cards.Length);
            for (int i = 1; i < cards.Length; i++)
            {
                Assert.IsTrue(cards[i].Heap <= cards[i - 1].Heap);
                if (cards[i].Heap == cards[i - 1].Heap)
                    Assert.IsTrue(cards[i].LastLearnUtcTime >= cards[i - 1].LastLearnUtcTime);
            }
        }
    }
}

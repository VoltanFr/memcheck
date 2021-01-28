using MemCheck.Application.Heaping;
using MemCheck.Application.QueryValidation;
using MemCheck.Application.Tests.Helpers;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards
{
    [TestClass()]
    public class GetCardsToRepeatTests
    {
        [TestMethod()]
        public async Task UserNotLoggedIn()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);

            using var dbContext = new MemCheckDbContext(db);
            var request = new GetCardsToRepeat.Request(Guid.Empty, deck, Array.Empty<Guid>(), Array.Empty<Guid>(), 10);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetCardsToRepeat(dbContext).RunAsync(request));
        }
        [TestMethod()]
        public async Task UserDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);

            using var dbContext = new MemCheckDbContext(db);
            var request = new GetCardsToRepeat.Request(Guid.NewGuid(), deck, Array.Empty<Guid>(), Array.Empty<Guid>(), 10);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetCardsToRepeat(dbContext).RunAsync(request));
        }
        [TestMethod()]
        public async Task DeckDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            var request = new GetCardsToRepeat.Request(user, Guid.NewGuid(), Array.Empty<Guid>(), Array.Empty<Guid>(), 10);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetCardsToRepeat(dbContext).RunAsync(request));
        }
        [TestMethod()]
        public async Task UserNotOwner()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);
            var otherUser = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            var request = new GetCardsToRepeat.Request(otherUser, deck, Array.Empty<Guid>(), Array.Empty<Guid>(), 10);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetCardsToRepeat(dbContext).RunAsync(request));
        }
        [TestMethod()]
        public async Task DeckContainsOneCardNonExpired()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user, algorithmId: DefaultHeapingAlgorithm.ID);
            var card = await CardHelper.CreateAsync(db, user);
            await DeckHelper.AddCardAsync(db, deck, card.Id, lastLearnUtcTime: new DateTime(2000, 1, 1));

            using var dbContext = new MemCheckDbContext(db);
            var request = new GetCardsToRepeat.Request(user, deck, Array.Empty<Guid>(), Array.Empty<Guid>(), 10);
            var cards = await new GetCardsToRepeat(dbContext).RunAsync(request, new DateTime(2000, 1, 2));
            Assert.IsFalse(cards.Any());
        }
        [TestMethod()]
        public async Task DeckContainsOneCardExpired()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);
            var card = await CardHelper.CreateAsync(db, user);
            await DeckHelper.AddCardAsync(db, deck, card.Id, 1, new DateTime(2000, 1, 1));

            using var dbContext = new MemCheckDbContext(db);
            var request = new GetCardsToRepeat.Request(user, deck, Array.Empty<Guid>(), Array.Empty<Guid>(), 10);
            var cards = await new GetCardsToRepeat(dbContext).RunAsync(request, new DateTime(2000, 1, 4));
            Assert.AreEqual(card.Id, cards.Single().CardId);
        }
        [TestMethod()]
        public async Task RequestedCount()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user, algorithmId: DeveloperHeapingAlgorithm.ID);
            var loadTime = DateHelper.Random();
            const int cardCount = 50;
            for (int i = 0; i < cardCount; i++)
            {
                var card = await CardHelper.CreateAsync(db, user);
                await DeckHelper.AddCardAsync(db, deck, card.Id, RandomHelper.Heap(true), RandomHelper.DateBefore(loadTime));
            }
            using var dbContext = new MemCheckDbContext(db);
            var request = new GetCardsToRepeat.Request(user, deck, Array.Empty<Guid>(), Array.Empty<Guid>(), cardCount / 2);
            var cards = await new GetCardsToRepeat(dbContext).RunAsync(request, loadTime);
            Assert.AreEqual(request.CardsToDownload, cards.Count());

            request = new GetCardsToRepeat.Request(user, deck, Array.Empty<Guid>(), Array.Empty<Guid>(), cardCount);
            cards = await new GetCardsToRepeat(dbContext).RunAsync(request, loadTime);
            Assert.AreEqual(request.CardsToDownload, cards.Count());

            request = new GetCardsToRepeat.Request(user, deck, Array.Empty<Guid>(), Array.Empty<Guid>(), cardCount * 2);
            cards = await new GetCardsToRepeat(dbContext).RunAsync(request, loadTime);
            Assert.AreEqual(cardCount, cards.Count());
        }
        [TestMethod()]
        public async Task CheckOrder()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user, algorithmId: DeveloperHeapingAlgorithm.ID);
            var loadTime = DateHelper.Random();
            const int cardCount = 100;
            for (int i = 0; i < cardCount; i++)
            {
                var card = await CardHelper.CreateAsync(db, user);
                await DeckHelper.AddCardAsync(db, deck, card.Id, RandomHelper.Heap(true), RandomHelper.DateBefore(loadTime));
            }
            using var dbContext = new MemCheckDbContext(db);
            var request = new GetCardsToRepeat.Request(user, deck, Array.Empty<Guid>(), Array.Empty<Guid>(), cardCount);
            var cards = (await new GetCardsToRepeat(dbContext).RunAsync(request, loadTime)).ToImmutableArray();
            Assert.AreEqual(cardCount, cards.Length);
            for (int i = 1; i < cards.Length; i++)
            {
                Assert.IsTrue(cards[i].Heap <= cards[i - 1].Heap);
                if (cards[i].Heap == cards[i - 1].Heap)
                    Assert.IsTrue(cards[i].LastLearnUtcTime >= cards[i - 1].LastLearnUtcTime);
            }
        }
        [TestMethod()]
        public async Task OneCardWithImages()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);
            var image1 = await ImageHelper.CreateAsync(db, user);
            var image2 = await ImageHelper.CreateAsync(db, user);
            var createdCard = await CardHelper.CreateAsync(db, user, frontSideImages: new[] { image1, image2 });
            await DeckHelper.AddCardAsync(db, deck, createdCard.Id, 1, new DateTime(2000, 1, 1));

            using var dbContext = new MemCheckDbContext(db);
            var request = new GetCardsToRepeat.Request(user, deck, Array.Empty<Guid>(), Array.Empty<Guid>(), 10);
            var cards = await new GetCardsToRepeat(dbContext).RunAsync(request, new DateTime(2000, 1, 4));
            var resultImages = cards.Single().Images;
            Assert.AreEqual(2, resultImages.Count());
            Assert.AreEqual(image1, resultImages.First().ImageId);
            Assert.AreEqual(image2, resultImages.Last().ImageId);
        }
    }
}

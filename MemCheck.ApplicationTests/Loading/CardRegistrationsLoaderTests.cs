using MemCheck.Application.CardChanging;
using MemCheck.Application.Tests.Helpers;
using MemCheck.Basics;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Loading
{
    [TestClass()]
    public class CardRegistrationsLoaderTests
    {
        [TestMethod()]
        public async Task CardDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            using var dbContext = new MemCheckDbContext(db);
            var cardId = Guid.NewGuid();
            var result = new CardRegistrationsLoader(dbContext).RunForCardIds(user, new[] { cardId });
            Assert.AreEqual(1, result.Count);
            Assert.IsFalse(result[cardId]);
        }
        [TestMethod()]
        public async Task CardNotRegistered()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var card = await CardHelper.CreateAsync(db, user);
            using var dbContext = new MemCheckDbContext(db);
            var result = new CardRegistrationsLoader(dbContext).RunForCardIds(user, new[] { card.Id });
            Assert.AreEqual(1, result.Count);
            Assert.IsFalse(result[card.Id]);
        }
        [TestMethod()]
        public async Task CardRegisteredForOtherUser()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var card = await CardHelper.CreateAsync(db, user);
            await CardSubscriptionHelper.CreateAsync(db, user, card.Id);
            var otherUser = await UserHelper.CreateInDbAsync(db);
            using var dbContext = new MemCheckDbContext(db);
            var result = new CardRegistrationsLoader(dbContext).RunForCardIds(otherUser, new[] { card.Id });
            Assert.AreEqual(1, result.Count);
            Assert.IsFalse(result[card.Id]);
        }
        [TestMethod()]
        public async Task CardRegistered()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var card = await CardHelper.CreateAsync(db, user);
            await CardSubscriptionHelper.CreateAsync(db, user, card.Id);
            using var dbContext = new MemCheckDbContext(db);
            var result = new CardRegistrationsLoader(dbContext).RunForCardIds(user, new[] { card.Id });
            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result[card.Id]);
        }
        [TestMethod()]
        public async Task CardRegisteredDeleted()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);

            var deletedCard = await CardHelper.CreateAsync(db, user);
            await CardSubscriptionHelper.CreateAsync(db, user, deletedCard.Id);
            using (var dbContext = new MemCheckDbContext(db))
                await new DeleteCards(dbContext, new TestLocalizer()).RunAsync(new DeleteCards.Request(user, deletedCard.Id.ToEnumerable()));

            using (var dbContext = new MemCheckDbContext(db))
            {
                var result = new CardRegistrationsLoader(dbContext).RunForCardIds(user, deletedCard.Id.ToEnumerable());
                Assert.AreEqual(1, result.Count);
                Assert.IsTrue(result[deletedCard.Id]);
            }
        }
        [TestMethod()]
        public async Task ComplexCase()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);

            var registeredCard1 = await CardHelper.CreateAsync(db, user);
            await CardSubscriptionHelper.CreateAsync(db, user, registeredCard1.Id);

            var registeredCard2 = await CardHelper.CreateAsync(db, user);
            await CardSubscriptionHelper.CreateAsync(db, user, registeredCard2.Id);

            var deletedCard = await CardHelper.CreateAsync(db, user);
            await CardSubscriptionHelper.CreateAsync(db, user, deletedCard.Id);
            using (var dbContext = new MemCheckDbContext(db))
                await new DeleteCards(dbContext, new TestLocalizer()).RunAsync(new DeleteCards.Request(user, deletedCard.Id.ToEnumerable()));

            var nonRegisteredCard = await CardHelper.CreateAsync(db, user);

            using (var dbContext = new MemCheckDbContext(db))
            {
                var result = new CardRegistrationsLoader(dbContext).RunForCardIds(user, new[] { registeredCard1.Id, registeredCard2.Id, deletedCard.Id, nonRegisteredCard.Id });
                Assert.AreEqual(4, result.Count);
                Assert.IsTrue(result[registeredCard1.Id]);
                Assert.IsTrue(result[registeredCard2.Id]);
                Assert.IsTrue(result[deletedCard.Id]);
                Assert.IsFalse(result[nonRegisteredCard.Id]);
            }
        }
    }
}

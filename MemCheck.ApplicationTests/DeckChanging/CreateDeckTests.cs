using MemCheck.Application.Heaping;
using MemCheck.Application.QueryValidation;
using MemCheck.Application.Tests.Helpers;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.DeckChanging
{
    [TestClass()]
    public class CreateDeckTests
    {
        [TestMethod()]
        public async Task UserNotLoggedIn()
        {
            using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new CreateDeck(dbContext).RunAsync(new CreateDeck.Request(Guid.Empty, RandomHelper.String(), 0), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task UserDoesNotExist()
        {
            using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new CreateDeck(dbContext).RunAsync(new CreateDeck.Request(Guid.NewGuid(), RandomHelper.String(), 0), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task NameNotTrimmed()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new CreateDeck(dbContext).RunAsync(new CreateDeck.Request(user, RandomHelper.String() + '\t', 0), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task NameTooShort()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new CreateDeck(dbContext).RunAsync(new CreateDeck.Request(user, RandomHelper.String(QueryValidationHelper.DeckMinNameLength - 1), 0), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task NameTooLong()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new CreateDeck(dbContext).RunAsync(new CreateDeck.Request(user, RandomHelper.String(QueryValidationHelper.DeckMaxNameLength + 1), 0), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task DeckWithThisNameExists()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var otherDeckName = RandomHelper.String();
            await DeckHelper.CreateAsync(db, user, otherDeckName);
            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new CreateDeck(dbContext).RunAsync(new CreateDeck.Request(user, otherDeckName, RandomHelper.HeapingAlgorithm()), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task InexistentAlgorithm()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new CreateDeck(dbContext).RunAsync(new CreateDeck.Request(user, RandomHelper.String(), RandomHelper.ValueNotInSet(HeapingAlgorithms.Instance.Ids)), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task Success()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var name = RandomHelper.String();
            int algo = RandomHelper.HeapingAlgorithm();

            using (var dbContext = new MemCheckDbContext(db))
                await new CreateDeck(dbContext).RunAsync(new CreateDeck.Request(user, name, algo), new TestLocalizer());

            using (var dbContext = new MemCheckDbContext(db))
            {
                var deckFromDb = await dbContext.Decks.SingleAsync();
                Assert.AreEqual(name, deckFromDb.Description);
                Assert.AreEqual(algo, deckFromDb.HeapingAlgorithmId);
            }
        }
    }
}

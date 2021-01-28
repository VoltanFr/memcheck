using MemCheck.Application.Decks;
using MemCheck.Application.Tests.Helpers;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace MemCheck.ApplicationTests.Decks
{
    [TestClass()]
    public class DeleteDeckTests
    {
        [TestMethod()]
        public async Task UserNotLoggedIn()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);

            using var dbContext = new MemCheckDbContext(db);
            var request = new DeleteDeck.Request(Guid.Empty, deck);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new DeleteDeck(dbContext).RunAsync(request));
        }
        [TestMethod()]
        public async Task UserDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);

            using var dbContext = new MemCheckDbContext(db);
            var request = new DeleteDeck.Request(Guid.NewGuid(), deck);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new DeleteDeck(dbContext).RunAsync(request));
        }
        [TestMethod()]
        public async Task DeckDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            var request = new DeleteDeck.Request(user, Guid.NewGuid());
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new DeleteDeck(dbContext).RunAsync(request));
        }
        [TestMethod()]
        public async Task UserNotOwner()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);
            var otherUser = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            var request = new DeleteDeck.Request(otherUser, deck);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new DeleteDeck(dbContext).RunAsync(request));
        }
        [TestMethod()]
        public async Task Success()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);
            var otherDeck = await DeckHelper.CreateAsync(db, user);

            var request = new DeleteDeck.Request(user, deck);

            using (var dbContext = new MemCheckDbContext(db))
                await new DeleteDeck(dbContext).RunAsync(request);

            using (var dbContext = new MemCheckDbContext(db))
            {
                Assert.IsFalse(await dbContext.Decks.AnyAsync(d => d.Id == deck));
                Assert.IsTrue(await dbContext.Decks.AnyAsync(d => d.Id == otherDeck));
            }
        }
    }
}

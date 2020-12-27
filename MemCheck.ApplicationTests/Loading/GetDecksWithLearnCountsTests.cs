using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using MemCheck.Database;
using MemCheck.Application.Tests;
using System.Linq;
using MemCheck.Application.Tests.Helpers;
using MemCheck.Application.QueryValidation;

namespace MemCheck.Application.Loading
{
    [TestClass()]
    public class GetDecksWithLearnCountsTests
    {
        [TestMethod()]
        public async Task EmptyDB_UserNotLoggedIn()
        {
            using (var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB()))
                await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new GetDecksWithLearnCounts(dbContext).RunAsync(new GetDecksWithLearnCounts.Request(Guid.Empty)));
        }
        [TestMethod()]
        public async Task EmptyDB_UserDoesNotExist()
        {
            using (var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB()))
                await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new GetDecksWithLearnCounts(dbContext).RunAsync(new GetDecksWithLearnCounts.Request(Guid.NewGuid())));
        }
        [TestMethod()]
        public async Task OneEmptyDeck()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(testDB);
            var description = StringServices.RandomString();
            var deck = await DeckHelper.CreateAsync(testDB, userId, description);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new GetDecksWithLearnCounts.Request(userId);
                var result = await new GetDecksWithLearnCounts(dbContext).RunAsync(request);
                Assert.AreEqual(1, result.Count());
                var loaded = result.First();
                Assert.AreEqual(deck, loaded.Id);
                Assert.AreEqual(description, loaded.Description);
                Assert.AreEqual(0, loaded.UnknownCardCount);
                Assert.AreEqual(0, loaded.ExpiredCardCount);
                Assert.AreEqual(0, loaded.CardCount);
            }
        }
    }
}

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
        [TestMethod()]
        public async Task FullTest()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(testDB);

            var deck1Description = StringServices.RandomString();
            var deck1 = await DeckHelper.CreateAsync(testDB, userId, deck1Description);

            var deck2Description = StringServices.RandomString();
            var deck2 = await DeckHelper.CreateAsync(testDB, userId, deck2Description);

            var jan01 = new DateTime(2030, 01, 01);
            var jan29 = new DateTime(2030, 01, 29);
            var jan31 = new DateTime(2030, 01, 31);

            //Fill deck1
            await DeckHelper.AddCardAsync(testDB, userId, deck1, (await CardHelper.CreateAsync(testDB, userId)).Id, 0, jan31);
            await DeckHelper.AddCardAsync(testDB, userId, deck1, (await CardHelper.CreateAsync(testDB, userId)).Id, 0, jan29);
            await DeckHelper.AddCardAsync(testDB, userId, deck1, (await CardHelper.CreateAsync(testDB, userId)).Id, 1, jan31);
            await DeckHelper.AddCardAsync(testDB, userId, deck1, (await CardHelper.CreateAsync(testDB, userId)).Id, 1, jan29);
            await DeckHelper.AddCardAsync(testDB, userId, deck1, (await CardHelper.CreateAsync(testDB, userId)).Id, 1, jan01);
            await DeckHelper.AddCardAsync(testDB, userId, deck1, (await CardHelper.CreateAsync(testDB, userId)).Id, 4, jan01);
            await DeckHelper.AddCardAsync(testDB, userId, deck1, (await CardHelper.CreateAsync(testDB, userId)).Id, 5, jan01);

            //Fill deck2
            await DeckHelper.AddCardAsync(testDB, userId, deck2, (await CardHelper.CreateAsync(testDB, userId)).Id, 0, jan31);
            await DeckHelper.AddCardAsync(testDB, userId, deck2, (await CardHelper.CreateAsync(testDB, userId)).Id, 8, jan01);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new GetDecksWithLearnCounts.Request(userId);
                var result = await new GetDecksWithLearnCounts(dbContext).RunAsync(request, new DateTime(2030, 02, 01));
                Assert.AreEqual(2, result.Count());

                var loadedDeck1 = result.Single(d => d.Id == deck1);
                Assert.AreEqual(deck1Description, loadedDeck1.Description);
                Assert.AreEqual(2, loadedDeck1.UnknownCardCount);
                Assert.AreEqual(3, loadedDeck1.ExpiredCardCount);
                Assert.AreEqual(7, loadedDeck1.CardCount);

                var loadedDeck2 = result.Single(d => d.Id == deck2);
                Assert.AreEqual(deck2Description, loadedDeck2.Description);
                Assert.AreEqual(1, loadedDeck2.UnknownCardCount);
                Assert.AreEqual(0, loadedDeck2.ExpiredCardCount);
                Assert.AreEqual(2, loadedDeck2.CardCount);
            }
        }
    }
}

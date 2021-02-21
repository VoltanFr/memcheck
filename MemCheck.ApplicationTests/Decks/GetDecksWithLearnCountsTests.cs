using MemCheck.Application.Tests.Helpers;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Decks
{
    [TestClass()]
    public class GetDecksWithLearnCountsTests
    {
        [TestMethod()]
        public async Task EmptyDB_UserNotLoggedIn()
        {
            using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetDecksWithLearnCounts(dbContext).RunAsync(new GetDecksWithLearnCounts.Request(Guid.Empty)));
        }
        [TestMethod()]
        public async Task EmptyDB_UserDoesNotExist()
        {
            using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetDecksWithLearnCounts(dbContext).RunAsync(new GetDecksWithLearnCounts.Request(Guid.NewGuid())));
        }
        [TestMethod()]
        public async Task OneEmptyDeck()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(testDB);
            var description = RandomHelper.String();
            var deck = await DeckHelper.CreateAsync(testDB, userId, description);

            using var dbContext = new MemCheckDbContext(testDB);
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
        [TestMethod()]
        public async Task OneExpiredAndOneToExpireInTheHour()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(testDB);
            var deck1 = await DeckHelper.CreateAsync(testDB, userId, RandomHelper.String());

            var addToDeckDate = RandomHelper.Date();
            await DeckHelper.AddCardAsync(testDB, deck1, (await CardHelper.CreateAsync(testDB, userId)).Id, 1, addToDeckDate);
            await DeckHelper.AddCardAsync(testDB, deck1, (await CardHelper.CreateAsync(testDB, userId)).Id, 1, addToDeckDate.AddHours(2));

            using var dbContext = new MemCheckDbContext(testDB);
            var resultDeck = (await new GetDecksWithLearnCounts(dbContext).RunAsync(new GetDecksWithLearnCounts.Request(userId), addToDeckDate.AddDays(1).AddHours(1.5))).First();
            Assert.AreEqual(1, resultDeck.ExpiredCardCount);
            Assert.AreEqual(1, resultDeck.ExpiringNextHourCount);
            Assert.AreEqual(0, resultDeck.ExpiringFollowing24hCount);
            Assert.AreEqual(0, resultDeck.ExpiringFollowing3DaysCount);
        }
        [TestMethod()]
        public async Task OneCardOnVariousTimes()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(testDB);
            var deck1 = await DeckHelper.CreateAsync(testDB, userId, RandomHelper.String());

            var addToDeckDate = RandomHelper.Date();
            await DeckHelper.AddCardAsync(testDB, deck1, (await CardHelper.CreateAsync(testDB, userId)).Id, 4, addToDeckDate);

            using var dbContext = new MemCheckDbContext(testDB);
            var result = (await new GetDecksWithLearnCounts(dbContext).RunAsync(new GetDecksWithLearnCounts.Request(userId), addToDeckDate.AddDays(4).AddMinutes(-61))).First();
            Assert.AreEqual(0, result.ExpiringNextHourCount);
            Assert.AreEqual(1, result.ExpiringFollowing24hCount);
            Assert.AreEqual(0, result.ExpiringFollowing3DaysCount);

            result = (await new GetDecksWithLearnCounts(dbContext).RunAsync(new GetDecksWithLearnCounts.Request(userId), addToDeckDate.AddDays(4).AddMinutes(-10))).First();
            Assert.AreEqual(1, result.ExpiringNextHourCount);
            Assert.AreEqual(0, result.ExpiringFollowing24hCount);
            Assert.AreEqual(0, result.ExpiringFollowing3DaysCount);

            result = (await new GetDecksWithLearnCounts(dbContext).RunAsync(new GetDecksWithLearnCounts.Request(userId), addToDeckDate.AddDays(3).AddMinutes(1))).First();
            Assert.AreEqual(0, result.ExpiringNextHourCount);
            Assert.AreEqual(1, result.ExpiringFollowing24hCount);
            Assert.AreEqual(0, result.ExpiringFollowing3DaysCount);

            result = (await new GetDecksWithLearnCounts(dbContext).RunAsync(new GetDecksWithLearnCounts.Request(userId), addToDeckDate.AddDays(3).AddMinutes(-61))).First();
            Assert.AreEqual(0, result.ExpiringNextHourCount);
            Assert.AreEqual(0, result.ExpiringFollowing24hCount);
            Assert.AreEqual(1, result.ExpiringFollowing3DaysCount);

            result = (await new GetDecksWithLearnCounts(dbContext).RunAsync(new GetDecksWithLearnCounts.Request(userId), addToDeckDate.AddDays(1))).First();
            Assert.AreEqual(0, result.ExpiringNextHourCount);
            Assert.AreEqual(0, result.ExpiringFollowing24hCount);
            Assert.AreEqual(1, result.ExpiringFollowing3DaysCount);

            result = (await new GetDecksWithLearnCounts(dbContext).RunAsync(new GetDecksWithLearnCounts.Request(userId), addToDeckDate)).First();
            Assert.AreEqual(0, result.ExpiringNextHourCount);
            Assert.AreEqual(0, result.ExpiringFollowing24hCount);
            Assert.AreEqual(1, result.ExpiringFollowing3DaysCount);
        }
        [TestMethod()]
        public async Task FullTestWithOneDeck()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(testDB);

            var deckDescription = RandomHelper.String();
            var deck = await DeckHelper.CreateAsync(testDB, userId, deckDescription, Deck.DefaultHeapingAlgorithmId);

            var jan01 = new DateTime(2030, 01, 01).ToUniversalTime();
            var jan30 = new DateTime(2030, 01, 30, 0, 0, 0).ToUniversalTime();
            var jan31 = new DateTime(2030, 01, 31).ToUniversalTime();
            var jan28 = new DateTime(2030, 01, 28).ToUniversalTime();

            await DeckHelper.AddCardAsync(testDB, deck, (await CardHelper.CreateAsync(testDB, userId)).Id, 0, jan31);
            await DeckHelper.AddCardAsync(testDB, deck, (await CardHelper.CreateAsync(testDB, userId)).Id, 0, jan30);
            await DeckHelper.AddCardAsync(testDB, deck, (await CardHelper.CreateAsync(testDB, userId)).Id, 1, jan31);   //expires in the following 24h
            await DeckHelper.AddCardAsync(testDB, deck, (await CardHelper.CreateAsync(testDB, userId)).Id, 1, jan30);   //expired
            await DeckHelper.AddCardAsync(testDB, deck, (await CardHelper.CreateAsync(testDB, userId)).Id, 1, jan30);   //expired
            await DeckHelper.AddCardAsync(testDB, deck, (await CardHelper.CreateAsync(testDB, userId)).Id, 1, jan01);   //expired
            await DeckHelper.AddCardAsync(testDB, deck, (await CardHelper.CreateAsync(testDB, userId)).Id, 3, jan28);
            await DeckHelper.AddCardAsync(testDB, deck, (await CardHelper.CreateAsync(testDB, userId)).Id, 4, jan01);   //expired
            await DeckHelper.AddCardAsync(testDB, deck, (await CardHelper.CreateAsync(testDB, userId)).Id, 6, jan01);

            using var dbContext = new MemCheckDbContext(testDB);
            var request = new GetDecksWithLearnCounts.Request(userId);
            var result = await new GetDecksWithLearnCounts(dbContext).RunAsync(request, new DateTime(2030, 02, 01, 0, 30, 0));
            var loaded = result.Single();
            Assert.AreEqual(deckDescription, loaded.Description);
            Assert.AreEqual(2, loaded.UnknownCardCount);
            Assert.AreEqual(4, loaded.ExpiredCardCount);
            Assert.AreEqual(0, loaded.ExpiringNextHourCount);
            Assert.AreEqual(1, loaded.ExpiringFollowing24hCount);
            Assert.AreEqual(1, loaded.ExpiringFollowing3DaysCount);
            Assert.AreEqual(9, loaded.CardCount);
        }
        [TestMethod()]
        public async Task FullTestWithTwoDecks()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(testDB);

            var deck1Description = RandomHelper.String();
            var deck1 = await DeckHelper.CreateAsync(testDB, userId, deck1Description, Deck.DefaultHeapingAlgorithmId);

            var deck2Description = RandomHelper.String();
            var deck2 = await DeckHelper.CreateAsync(testDB, userId, deck2Description, Deck.DefaultHeapingAlgorithmId);

            var jan01 = new DateTime(2030, 01, 01).ToUniversalTime();
            var jan28 = new DateTime(2030, 01, 28).ToUniversalTime();
            var jan30 = new DateTime(2030, 01, 30).ToUniversalTime();
            var jan31 = new DateTime(2030, 01, 31).ToUniversalTime();
            var jan30_12h00 = new DateTime(2030, 01, 30, 12, 0, 0).ToUniversalTime();

            //Fill deck1
            await DeckHelper.AddCardAsync(testDB, deck1, (await CardHelper.CreateAsync(testDB, userId)).Id, 0, jan31);
            await DeckHelper.AddCardAsync(testDB, deck1, (await CardHelper.CreateAsync(testDB, userId)).Id, 0, jan30);
            await DeckHelper.AddCardAsync(testDB, deck1, (await CardHelper.CreateAsync(testDB, userId)).Id, 1, jan31);   //expires in the following 24h
            await DeckHelper.AddCardAsync(testDB, deck1, (await CardHelper.CreateAsync(testDB, userId)).Id, 1, jan30);   //expired
            await DeckHelper.AddCardAsync(testDB, deck1, (await CardHelper.CreateAsync(testDB, userId)).Id, 1, jan30_12h00);
            await DeckHelper.AddCardAsync(testDB, deck1, (await CardHelper.CreateAsync(testDB, userId)).Id, 1, jan01);   //expired
            await DeckHelper.AddCardAsync(testDB, deck1, (await CardHelper.CreateAsync(testDB, userId)).Id, 3, jan28);
            await DeckHelper.AddCardAsync(testDB, deck1, (await CardHelper.CreateAsync(testDB, userId)).Id, 4, jan01);   //expired
            await DeckHelper.AddCardAsync(testDB, deck1, (await CardHelper.CreateAsync(testDB, userId)).Id, 6, jan01);

            //Fill deck2
            await DeckHelper.AddCardAsync(testDB, deck2, (await CardHelper.CreateAsync(testDB, userId)).Id, 0, jan31);
            await DeckHelper.AddCardAsync(testDB, deck2, (await CardHelper.CreateAsync(testDB, userId)).Id, 8, jan01);

            using var dbContext = new MemCheckDbContext(testDB);
            var request = new GetDecksWithLearnCounts.Request(userId);
            var result = await new GetDecksWithLearnCounts(dbContext).RunAsync(request, new DateTime(2030, 02, 01, 0, 30, 0));
            Assert.AreEqual(2, result.Count());

            var loadedDeck1 = result.Single(d => d.Id == deck1);
            Assert.AreEqual(deck1Description, loadedDeck1.Description);
            Assert.AreEqual(2, loadedDeck1.UnknownCardCount);
            Assert.AreEqual(3, loadedDeck1.ExpiredCardCount);
            Assert.AreEqual(0, loadedDeck1.ExpiringNextHourCount);
            Assert.AreEqual(2, loadedDeck1.ExpiringFollowing24hCount);
            Assert.AreEqual(1, loadedDeck1.ExpiringFollowing3DaysCount);
            Assert.AreEqual(9, loadedDeck1.CardCount);

            var loadedDeck2 = result.Single(d => d.Id == deck2);
            Assert.AreEqual(deck2Description, loadedDeck2.Description);
            Assert.AreEqual(1, loadedDeck2.UnknownCardCount);
            Assert.AreEqual(0, loadedDeck2.ExpiredCardCount);
            Assert.AreEqual(0, loadedDeck2.ExpiringNextHourCount);
            Assert.AreEqual(0, loadedDeck2.ExpiringFollowing24hCount);
            Assert.AreEqual(0, loadedDeck2.ExpiringFollowing3DaysCount);
            Assert.AreEqual(2, loadedDeck2.CardCount);
        }
    }
}

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
                await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new GetDecksWithLearnCounts(dbContext).RunAsync(new GetDecksWithLearnCounts.Request(Guid.Empty, 0)));
        }
        [TestMethod()]
        public async Task EmptyDB_UserDoesNotExist()
        {
            using (var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB()))
                await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new GetDecksWithLearnCounts(dbContext).RunAsync(new GetDecksWithLearnCounts.Request(Guid.NewGuid(), 0)));
        }
        [TestMethod()]
        public async Task OneEmptyDeck()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(testDB);
            var description = StringHelper.RandomString();
            var deck = await DeckHelper.CreateAsync(testDB, userId, description);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new GetDecksWithLearnCounts.Request(userId, 0);
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
        public async Task OneExpiredAndOneToExpireToday()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(testDB);
            var deck1 = await DeckHelper.CreateAsync(testDB, userId, StringHelper.RandomString());

            await DeckHelper.AddCardAsync(testDB, userId, deck1, (await CardHelper.CreateAsync(testDB, userId)).Id, 1, new DateTime(2030, 01, 30, 0, 0, 0));
            await DeckHelper.AddCardAsync(testDB, userId, deck1, (await CardHelper.CreateAsync(testDB, userId)).Id, 1, new DateTime(2030, 01, 30, 12, 0, 0));

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var resultDeck = (await new GetDecksWithLearnCounts(dbContext).RunAsync(new GetDecksWithLearnCounts.Request(userId, 0), new DateTime(2030, 02, 01, 0, 0, 0))).First();
                Assert.AreEqual(1, resultDeck.ExpiredCardCount);
                Assert.AreEqual(1, resultDeck.ExpiringTodayCount);
            }
        }
        [TestMethod()]
        public async Task OneCardToExpireTomorrow()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(testDB);
            var deck1 = await DeckHelper.CreateAsync(testDB, userId, StringHelper.RandomString());

            await DeckHelper.AddCardAsync(testDB, userId, deck1, (await CardHelper.CreateAsync(testDB, userId)).Id, 1, new DateTime(2030, 01, 10, 12, 0, 0)); //Expires on 2020, 01, 12 at 12:00

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var resultOn10 = (await new GetDecksWithLearnCounts(dbContext).RunAsync(new GetDecksWithLearnCounts.Request(userId, 0), new DateTime(2030, 01, 10))).First();
                Assert.AreEqual(0, resultOn10.ExpiredCardCount);
                Assert.AreEqual(0, resultOn10.ExpiringTodayCount);
                Assert.AreEqual(0, resultOn10.ExpiringTomorrowCount);

                var resultOn11 = (await new GetDecksWithLearnCounts(dbContext).RunAsync(new GetDecksWithLearnCounts.Request(userId, 0), new DateTime(2030, 01, 11))).First();
                Assert.AreEqual(0, resultOn11.ExpiredCardCount);
                Assert.AreEqual(0, resultOn11.ExpiringTodayCount);
                Assert.AreEqual(1, resultOn11.ExpiringTomorrowCount);

                var resultOn12 = (await new GetDecksWithLearnCounts(dbContext).RunAsync(new GetDecksWithLearnCounts.Request(userId, 0), new DateTime(2030, 01, 12, 0, 0, 0))).First();
                Assert.AreEqual(0, resultOn12.ExpiredCardCount);
                Assert.AreEqual(1, resultOn12.ExpiringTodayCount);
                Assert.AreEqual(0, resultOn12.ExpiringTomorrowCount);

                var resultOn12_13 = (await new GetDecksWithLearnCounts(dbContext).RunAsync(new GetDecksWithLearnCounts.Request(userId, 0), new DateTime(2030, 01, 12, 13, 0, 0))).First();
                Assert.AreEqual(1, resultOn12_13.ExpiredCardCount);
                Assert.AreEqual(0, resultOn12_13.ExpiringTodayCount);
                Assert.AreEqual(0, resultOn12_13.ExpiringTomorrowCount);
            }
        }
        [TestMethod()]
        public async Task OneCardToExpireThisWeek()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(testDB);
            var deck1 = await DeckHelper.CreateAsync(testDB, userId, StringHelper.RandomString());

            await DeckHelper.AddCardAsync(testDB, userId, deck1, (await CardHelper.CreateAsync(testDB, userId)).Id, 4, new DateTime(2020, 12, 11, 1, 0, 0)); //Expires on 2020, 12, 27

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var resultOn20 = (await new GetDecksWithLearnCounts(dbContext).RunAsync(new GetDecksWithLearnCounts.Request(userId, 0), new DateTime(2020, 12, 20))).First();
                Assert.AreEqual(0, resultOn20.ExpiringTodayCount);
                Assert.AreEqual(0, resultOn20.ExpiringTomorrowCount);
                Assert.AreEqual(0, resultOn20.Expiring5NextDaysCount);

                var resultOn21 = (await new GetDecksWithLearnCounts(dbContext).RunAsync(new GetDecksWithLearnCounts.Request(userId, 0), new DateTime(2020, 12, 21))).First();
                Assert.AreEqual(0, resultOn21.ExpiringTodayCount);
                Assert.AreEqual(0, resultOn21.ExpiringTomorrowCount);
                Assert.AreEqual(1, resultOn21.Expiring5NextDaysCount);

                var resultOn22 = (await new GetDecksWithLearnCounts(dbContext).RunAsync(new GetDecksWithLearnCounts.Request(userId, 0), new DateTime(2020, 12, 22))).First();
                Assert.AreEqual(0, resultOn22.ExpiringTodayCount);
                Assert.AreEqual(0, resultOn22.ExpiringTomorrowCount);
                Assert.AreEqual(1, resultOn22.Expiring5NextDaysCount);

                var resultOn23 = (await new GetDecksWithLearnCounts(dbContext).RunAsync(new GetDecksWithLearnCounts.Request(userId, 0), new DateTime(2020, 12, 23))).First();
                Assert.AreEqual(0, resultOn23.ExpiringTodayCount);
                Assert.AreEqual(0, resultOn23.ExpiringTomorrowCount);
                Assert.AreEqual(1, resultOn23.Expiring5NextDaysCount);

                var resultOn24 = (await new GetDecksWithLearnCounts(dbContext).RunAsync(new GetDecksWithLearnCounts.Request(userId, 0), new DateTime(2020, 12, 24))).First();
                Assert.AreEqual(0, resultOn24.ExpiringTodayCount);
                Assert.AreEqual(0, resultOn24.ExpiringTomorrowCount);
                Assert.AreEqual(1, resultOn24.Expiring5NextDaysCount);

                var resultOn25 = (await new GetDecksWithLearnCounts(dbContext).RunAsync(new GetDecksWithLearnCounts.Request(userId, 0), new DateTime(2020, 12, 25))).First();
                Assert.AreEqual(0, resultOn25.ExpiringTodayCount);
                Assert.AreEqual(0, resultOn25.ExpiringTomorrowCount);
                Assert.AreEqual(1, resultOn25.Expiring5NextDaysCount);

                var resultOn26 = (await new GetDecksWithLearnCounts(dbContext).RunAsync(new GetDecksWithLearnCounts.Request(userId, 0), new DateTime(2020, 12, 26))).First();
                Assert.AreEqual(0, resultOn26.ExpiringTodayCount);
                Assert.AreEqual(1, resultOn26.ExpiringTomorrowCount);
                Assert.AreEqual(0, resultOn26.Expiring5NextDaysCount);

                var resultOn27 = (await new GetDecksWithLearnCounts(dbContext).RunAsync(new GetDecksWithLearnCounts.Request(userId, 0), new DateTime(2020, 12, 27, 0, 0, 0))).First();
                Assert.AreEqual(1, resultOn27.ExpiringTodayCount);
                Assert.AreEqual(0, resultOn27.ExpiringTomorrowCount);
                Assert.AreEqual(0, resultOn27.Expiring5NextDaysCount);
            }
        }
        [TestMethod()]
        public async Task FullTest()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(testDB);

            var deck1Description = StringHelper.RandomString();
            var deck1 = await DeckHelper.CreateAsync(testDB, userId, deck1Description);

            var deck2Description = StringHelper.RandomString();
            var deck2 = await DeckHelper.CreateAsync(testDB, userId, deck2Description);

            var jan01 = new DateTime(2030, 01, 01);
            var jan28 = new DateTime(2030, 01, 28);
            var jan30_00h00 = new DateTime(2030, 01, 30, 0, 0, 0);
            var jan31 = new DateTime(2030, 01, 31);
            var jan30_12h00 = new DateTime(2030, 01, 30, 12, 0, 0);

            //Fill deck1
            await DeckHelper.AddCardAsync(testDB, userId, deck1, (await CardHelper.CreateAsync(testDB, userId)).Id, 0, jan31);
            await DeckHelper.AddCardAsync(testDB, userId, deck1, (await CardHelper.CreateAsync(testDB, userId)).Id, 0, jan30_00h00);
            await DeckHelper.AddCardAsync(testDB, userId, deck1, (await CardHelper.CreateAsync(testDB, userId)).Id, 1, jan31);
            await DeckHelper.AddCardAsync(testDB, userId, deck1, (await CardHelper.CreateAsync(testDB, userId)).Id, 1, jan30_00h00);
            await DeckHelper.AddCardAsync(testDB, userId, deck1, (await CardHelper.CreateAsync(testDB, userId)).Id, 1, jan30_12h00);
            await DeckHelper.AddCardAsync(testDB, userId, deck1, (await CardHelper.CreateAsync(testDB, userId)).Id, 1, jan01);
            await DeckHelper.AddCardAsync(testDB, userId, deck1, (await CardHelper.CreateAsync(testDB, userId)).Id, 3, jan28);
            await DeckHelper.AddCardAsync(testDB, userId, deck1, (await CardHelper.CreateAsync(testDB, userId)).Id, 4, jan01);
            await DeckHelper.AddCardAsync(testDB, userId, deck1, (await CardHelper.CreateAsync(testDB, userId)).Id, 6, jan01);

            //Fill deck2
            await DeckHelper.AddCardAsync(testDB, userId, deck2, (await CardHelper.CreateAsync(testDB, userId)).Id, 0, jan31);
            await DeckHelper.AddCardAsync(testDB, userId, deck2, (await CardHelper.CreateAsync(testDB, userId)).Id, 8, jan01);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new GetDecksWithLearnCounts.Request(userId, 0);
                var result = await new GetDecksWithLearnCounts(dbContext).RunAsync(request, new DateTime(2030, 02, 01, 0, 0, 0));
                Assert.AreEqual(2, result.Count());

                var loadedDeck1 = result.Single(d => d.Id == deck1);
                Assert.AreEqual(deck1Description, loadedDeck1.Description);
                Assert.AreEqual(2, loadedDeck1.UnknownCardCount);
                Assert.AreEqual(3, loadedDeck1.ExpiredCardCount);
                Assert.AreEqual(1, loadedDeck1.ExpiringTodayCount);
                Assert.AreEqual(1, loadedDeck1.ExpiringTomorrowCount);
                Assert.AreEqual(1, loadedDeck1.Expiring5NextDaysCount);
                Assert.AreEqual(9, loadedDeck1.CardCount);

                var loadedDeck2 = result.Single(d => d.Id == deck2);
                Assert.AreEqual(deck2Description, loadedDeck2.Description);
                Assert.AreEqual(1, loadedDeck2.UnknownCardCount);
                Assert.AreEqual(0, loadedDeck2.ExpiredCardCount);
                Assert.AreEqual(0, loadedDeck2.ExpiringTodayCount);
                Assert.AreEqual(0, loadedDeck2.ExpiringTomorrowCount);
                Assert.AreEqual(0, loadedDeck2.Expiring5NextDaysCount);
                Assert.AreEqual(2, loadedDeck2.CardCount);
            }
        }
        //[TestMethod()]
        //public async Task ClientOnParisWinterTime()
        //{
        //    //Today, tomorrow, and other day considerations need to take care of the time offset on the client side
        //    //We don't have this need foe expiration since we store and use UTC for that
        //    //In this test method, Paris winter time is UTC-1

        //    var testDB = DbHelper.GetEmptyTestDB();
        //    var userId = await UserHelper.CreateInDbAsync(testDB);
        //    var deck1 = await DeckHelper.CreateAsync(testDB, userId, StringHelper.RandomString());

        //    await DeckHelper.AddCardAsync(testDB, userId, deck1, (await CardHelper.CreateAsync(testDB, userId)).Id, 1, new DateTime(2030, 01, 20, 12, 0, 0)); //Expires on 2020/01/22 at 12:00

        //    using (var dbContext = new MemCheckDbContext(testDB))
        //    {
        //        var clientIsOn_21_2259 = (await new GetDecksWithLearnCounts(dbContext).RunAsync(new GetDecksWithLearnCounts.Request(userId, -60), new DateTime(2030, 01, 21, 23, 59, 00))).First();
        //        Assert.AreEqual(0, clientIsOn_21_2259.ExpiringTodayCount);
        //        Assert.AreEqual(1, clientIsOn_21_2259.ExpiringTomorrowCount);

        //        var clientIsOn_21_2359 = (await new GetDecksWithLearnCounts(dbContext).RunAsync(new GetDecksWithLearnCounts.Request(userId, -60), new DateTime(2030, 01, 22, 0, 59, 00))).First();
        //        Assert.AreEqual(0, clientIsOn_21_2359.ExpiringTodayCount);
        //        Assert.AreEqual(1, clientIsOn_21_2359.ExpiringTomorrowCount);

        //        var clientIsOn_22_0001 = (await new GetDecksWithLearnCounts(dbContext).RunAsync(new GetDecksWithLearnCounts.Request(userId, -60), new DateTime(2030, 01, 22, 1, 1, 00))).First();
        //        Assert.AreEqual(1, clientIsOn_22_0001.ExpiringTodayCount);
        //        Assert.AreEqual(0, clientIsOn_22_0001.ExpiringTomorrowCount);
        //    }
        //}
    }
}

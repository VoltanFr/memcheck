using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using MemCheck.Application.Tests.Notifying;
using MemCheck.Database;
using MemCheck.Application.Tests;
using MemCheck.Application.Tests.BasicHelpers;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using MemCheck.Application.Tests.Helpers;
using MemCheck.Domain;
using MemCheck.Application.QueryValidation;

namespace MemCheck.Application.Searching
{
    [TestClass()]
    public class SearchCardsTests
    {
        [TestMethod()]
        public async Task TestEmptyDB_UserNotLoggedIn()
        {
            var testDB = DbHelper.GetEmptyTestDB();

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new SearchCards.Request(Guid.Empty, Guid.Empty, false, null, 1, 10, "", new Guid[0], new Guid[0], SearchCards.Request.VibilityFiltering.Ignore, SearchCards.Request.RatingFilteringMode.Ignore, 0, SearchCards.Request.NotificationFiltering.Ignore);
                var result = await new SearchCards(dbContext).RunAsync(request);
                Assert.AreEqual(0, result.TotalNbCards);
                Assert.AreEqual(0, result.PageCount);
            }
        }
        [TestMethod()]
        public async Task TestEmptyDB_UserLoggedIn()
        {
            var testDB = DbHelper.GetEmptyTestDB();

            var userId = await UserHelper.CreateInDbAsync(testDB);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new SearchCards.Request(userId, Guid.Empty, false, null, 1, 10, "", new Guid[0], new Guid[0], SearchCards.Request.VibilityFiltering.Ignore, SearchCards.Request.RatingFilteringMode.Ignore, 0, SearchCards.Request.NotificationFiltering.Ignore);
                var result = await new SearchCards(dbContext).RunAsync(request);
                Assert.AreEqual(0, result.TotalNbCards);
                Assert.AreEqual(0, result.PageCount);
            }
        }
        [TestMethod()]
        public async Task TestDBWithOnePublicCard_FindAll()
        {
            var testDB = DbHelper.GetEmptyTestDB();

            var userId = await UserHelper.CreateInDbAsync(testDB);
            var card = await CardHelper.CreateAsync(testDB, userId);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var requestWithUser = new SearchCards.Request(userId, Guid.Empty, false, null, 1, 10, "", new Guid[0], new Guid[0], SearchCards.Request.VibilityFiltering.Ignore, SearchCards.Request.RatingFilteringMode.Ignore, 0, SearchCards.Request.NotificationFiltering.Ignore);
                var resultWithUser = await new SearchCards(dbContext).RunAsync(requestWithUser);
                Assert.AreEqual(1, resultWithUser.TotalNbCards);
                Assert.AreEqual(1, resultWithUser.PageCount);
                Assert.AreEqual(card.Id, resultWithUser.Cards.First().CardId);

                var requestWithoutUser = new SearchCards.Request(Guid.Empty, Guid.Empty, false, null, 1, 10, "", new Guid[0], new Guid[0], SearchCards.Request.VibilityFiltering.Ignore, SearchCards.Request.RatingFilteringMode.Ignore, 0, SearchCards.Request.NotificationFiltering.Ignore);
                var resultWithoutUser = await new SearchCards(dbContext).RunAsync(requestWithUser);
                Assert.AreEqual(1, resultWithoutUser.TotalNbCards);
                Assert.AreEqual(1, resultWithoutUser.PageCount);
                Assert.AreEqual(card.Id, resultWithoutUser.Cards.First().CardId);
            }
        }
        [TestMethod()]
        public async Task Test_Privacy()
        {
            var testDB = DbHelper.GetEmptyTestDB();

            var user1Id = await UserHelper.CreateInDbAsync(testDB);
            var user2Id = await UserHelper.CreateInDbAsync(testDB);

            var privateCard_User1 = await CardHelper.CreateAsync(testDB, user1Id, userWithViewIds: new[] { user1Id });
            var privateCard_BothUsers = await CardHelper.CreateAsync(testDB, user1Id, userWithViewIds: new[] { user1Id, user2Id });
            var privateCard_User2 = await CardHelper.CreateAsync(testDB, user2Id, userWithViewIds: new[] { user2Id });
            var publicCard = await CardHelper.CreateAsync(testDB, user1Id);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var requestWithoutUser = new SearchCards.Request(Guid.Empty, Guid.Empty, false, null, 1, 10, "", new Guid[0], new Guid[0], SearchCards.Request.VibilityFiltering.Ignore, SearchCards.Request.RatingFilteringMode.Ignore, 0, SearchCards.Request.NotificationFiltering.Ignore);
                var resultWithoutUser = await new SearchCards(dbContext).RunAsync(requestWithoutUser);
                Assert.AreEqual(1, resultWithoutUser.TotalNbCards);
                Assert.AreEqual(1, resultWithoutUser.PageCount);
                Assert.AreEqual(publicCard.Id, resultWithoutUser.Cards.First().CardId);

                var requestWithUser1 = new SearchCards.Request(user1Id, Guid.Empty, false, null, 1, 10, "", new Guid[0], new Guid[0], SearchCards.Request.VibilityFiltering.Ignore, SearchCards.Request.RatingFilteringMode.Ignore, 0, SearchCards.Request.NotificationFiltering.Ignore);
                var resultWithUser1 = await new SearchCards(dbContext).RunAsync(requestWithUser1);
                Assert.AreEqual(3, resultWithUser1.TotalNbCards);
                Assert.AreEqual(1, resultWithUser1.PageCount);
                Assert.IsTrue(resultWithUser1.Cards.Any(card => card.CardId == privateCard_User1.Id));
                Assert.IsTrue(resultWithUser1.Cards.Any(card => card.CardId == privateCard_BothUsers.Id));
                Assert.IsTrue(resultWithUser1.Cards.Any(card => card.CardId == publicCard.Id));

                var requestWithUser2 = new SearchCards.Request(user2Id, Guid.Empty, false, null, 1, 10, "", new Guid[0], new Guid[0], SearchCards.Request.VibilityFiltering.Ignore, SearchCards.Request.RatingFilteringMode.Ignore, 0, SearchCards.Request.NotificationFiltering.Ignore);
                var resultWithUser2 = await new SearchCards(dbContext).RunAsync(requestWithUser2);
                Assert.AreEqual(3, resultWithUser2.TotalNbCards);
                Assert.AreEqual(1, resultWithUser2.PageCount);
                Assert.IsTrue(resultWithUser2.Cards.Any(card => card.CardId == privateCard_BothUsers.Id));
                Assert.IsTrue(resultWithUser2.Cards.Any(card => card.CardId == privateCard_User2.Id));
                Assert.IsTrue(resultWithUser2.Cards.Any(card => card.CardId == publicCard.Id));
            }
        }
        [TestMethod()]
        public async Task Test_Deck_Inclusive()
        {
            var testDB = DbHelper.GetEmptyTestDB();

            var user1Id = await UserHelper.CreateInDbAsync(testDB);
            var user2Id = await UserHelper.CreateInDbAsync(testDB);

            var card1 = await CardHelper.CreateAsync(testDB, user1Id);
            var card2 = await CardHelper.CreateAsync(testDB, user1Id);
            await CardHelper.CreateAsync(testDB, user1Id);

            var user1Emptydeck = await DeckHelper.CreateAsync(testDB, user1Id);
            var user1deck = await DeckHelper.CreateAsync(testDB, user1Id);

            using (var dbContext = new MemCheckDbContext(testDB))
                new AddCardsInDeck(dbContext).Run(user1deck, new[] { card1.Id, card2.Id });

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var requestOnEmptyDeck = new SearchCards.Request(user1Id, user1Emptydeck, true, null, 1, 10, "", new Guid[0], new Guid[0], SearchCards.Request.VibilityFiltering.Ignore, SearchCards.Request.RatingFilteringMode.Ignore, 0, SearchCards.Request.NotificationFiltering.Ignore);
                Assert.AreEqual(0, (await new SearchCards(dbContext).RunAsync(requestOnEmptyDeck)).TotalNbCards);

                var requestOnDeck = new SearchCards.Request(user1Id, user1deck, true, null, 1, 10, "", new Guid[0], new Guid[0], SearchCards.Request.VibilityFiltering.Ignore, SearchCards.Request.RatingFilteringMode.Ignore, 0, SearchCards.Request.NotificationFiltering.Ignore);
                var resultOnDeck = await new SearchCards(dbContext).RunAsync(requestOnDeck);
                Assert.AreEqual(2, resultOnDeck.TotalNbCards);
                Assert.IsTrue(resultOnDeck.Cards.Any(card => card.CardId == card1.Id));
                Assert.IsTrue(resultOnDeck.Cards.Any(card => card.CardId == card2.Id));

                var requestWithoutUser = new SearchCards.Request(Guid.Empty, user1Emptydeck, true, null, 1, 10, "", new Guid[0], new Guid[0], SearchCards.Request.VibilityFiltering.Ignore, SearchCards.Request.RatingFilteringMode.Ignore, 0, SearchCards.Request.NotificationFiltering.Ignore);
                await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new SearchCards(dbContext).RunAsync(requestWithoutUser));

                var requestWithUser2 = new SearchCards.Request(user2Id, user1deck, true, null, 1, 10, "", new Guid[0], new Guid[0], SearchCards.Request.VibilityFiltering.Ignore, SearchCards.Request.RatingFilteringMode.Ignore, 0, SearchCards.Request.NotificationFiltering.Ignore);
                await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new SearchCards(dbContext).RunAsync(requestWithUser2));
            }
        }
        [TestMethod()]
        public async Task Test_Deck_Exclusive()
        {
            var testDB = DbHelper.GetEmptyTestDB();

            var user1Id = await UserHelper.CreateInDbAsync(testDB);
            var user2Id = await UserHelper.CreateInDbAsync(testDB);

            var card1 = await CardHelper.CreateAsync(testDB, user1Id);
            var card2 = await CardHelper.CreateAsync(testDB, user1Id);
            var card3 = await CardHelper.CreateAsync(testDB, user1Id);

            var user1Emptydeck = await DeckHelper.CreateAsync(testDB, user1Id);
            var user1deck = await DeckHelper.CreateAsync(testDB, user1Id);

            using (var dbContext = new MemCheckDbContext(testDB))
                new AddCardsInDeck(dbContext).Run(user1deck, new[] { card1.Id, card2.Id });

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var requestOnEmptyDeck = new SearchCards.Request(user1Id, user1Emptydeck, false, null, 1, 10, "", new Guid[0], new Guid[0], SearchCards.Request.VibilityFiltering.Ignore, SearchCards.Request.RatingFilteringMode.Ignore, 0, SearchCards.Request.NotificationFiltering.Ignore);
                Assert.AreEqual(3, (await new SearchCards(dbContext).RunAsync(requestOnEmptyDeck)).TotalNbCards);

                var requestOnDeck = new SearchCards.Request(user1Id, user1deck, false, null, 1, 10, "", new Guid[0], new Guid[0], SearchCards.Request.VibilityFiltering.Ignore, SearchCards.Request.RatingFilteringMode.Ignore, 0, SearchCards.Request.NotificationFiltering.Ignore);
                var resultOnDeck = await new SearchCards(dbContext).RunAsync(requestOnDeck);
                Assert.AreEqual(1, resultOnDeck.TotalNbCards);
                Assert.IsTrue(resultOnDeck.Cards.Any(card => card.CardId == card3.Id));

                var requestWithoutUser = new SearchCards.Request(Guid.Empty, user1Emptydeck, false, null, 1, 10, "", new Guid[0], new Guid[0], SearchCards.Request.VibilityFiltering.Ignore, SearchCards.Request.RatingFilteringMode.Ignore, 0, SearchCards.Request.NotificationFiltering.Ignore);
                await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new SearchCards(dbContext).RunAsync(requestWithoutUser));

                var requestWithUser2 = new SearchCards.Request(user2Id, user1deck, false, null, 1, 10, "", new Guid[0], new Guid[0], SearchCards.Request.VibilityFiltering.Ignore, SearchCards.Request.RatingFilteringMode.Ignore, 0, SearchCards.Request.NotificationFiltering.Ignore);
                await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new SearchCards(dbContext).RunAsync(requestWithUser2));
            }
        }
    }
}

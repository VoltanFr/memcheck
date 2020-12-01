using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using MemCheck.Application.Tests.Notifying;
using MemCheck.Database;
using MemCheck.Application.Tests;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MemCheck.Application.Tests.Helpers;

namespace MemCheck.Application.CardChanging
{
    [TestClass()]
    public class PreviousVersionCreatorTests
    {
        [TestMethod()]
        public async Task TestSameUserCreatesVersion()
        {
            var testDB = DbHelper.GetEmptyTestDB();

            var ownerId = await UserHelper.CreateInDbAsync(testDB);
            var card = await CardHelper.CreateAsync(testDB, ownerId);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new PreviousVersionCreator(dbContext);
                await request.RunAsync(card.Id, ownerId, StringServices.RandomString());
                await dbContext.SaveChangesAsync();
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var previousVersion = await dbContext.CardPreviousVersions.Where(previous => previous.Card == card.Id).SingleAsync();
                Assert.AreNotEqual(card.Id, previousVersion.Id);
            }
        }
        [TestMethod()]
        public async Task TestOtherUserCreatesVersion()
        {
            var testDB = DbHelper.GetEmptyTestDB();

            var ownerId = await UserHelper.CreateInDbAsync(testDB);
            var card = await CardHelper.CreateAsync(testDB, ownerId);
            var newVersionCreatorId = await UserHelper.CreateInDbAsync(testDB, subscribeToCardOnEdit: true);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new PreviousVersionCreator(dbContext);
                await request.RunAsync(card.Id, newVersionCreatorId, StringServices.RandomString());
                await dbContext.SaveChangesAsync();
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var previousVersion = await dbContext.CardPreviousVersions.Where(previous => previous.Card == card.Id).SingleAsync();
                Assert.AreNotEqual(card.Id, previousVersion.Id);
                Assert.IsTrue(dbContext.CardNotifications.Any(cardSubscription => cardSubscription.CardId == card.Id && cardSubscription.UserId == newVersionCreatorId));
            }
        }
    }
}

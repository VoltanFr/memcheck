using MemCheck.Application.Tests.Helpers;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;

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
                await request.RunAsync(card.Id, ownerId, StringHelper.RandomString());
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
                await request.RunAsync(card.Id, newVersionCreatorId, StringHelper.RandomString());
                await dbContext.SaveChangesAsync();
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var previousVersion = await dbContext.CardPreviousVersions.Where(previous => previous.Card == card.Id).SingleAsync();
                Assert.AreNotEqual(card.Id, previousVersion.Id);
            }
            Assert.IsTrue(await CardSubscriptionHelper.UserIsSubscribedToCardAsync(testDB, newVersionCreatorId, card.Id));
        }
    }
}

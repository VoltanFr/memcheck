using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using MemCheck.Database;
using System.Linq;
using MemCheck.Application.Tests.Helpers;
using MemCheck.Application.CardChanging;
using MemCheck.Application.QueryValidation;

namespace MemCheck.Application.History
{
    [TestClass()]
    public class GetCardVersionsTests
    {
        [TestMethod()]
        public async Task UserNotLoggedIn()
        {
            using (var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB()))
                await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new GetCardVersions(dbContext, new TestLocalizer()).RunAsync(new GetCardVersions.Request(Guid.Empty, Guid.Empty)));
        }
        [TestMethod()]
        public async Task UserDoesNotExist()
        {
            using (var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB()))
                await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new GetCardVersions(dbContext, new TestLocalizer()).RunAsync(new GetCardVersions.Request(Guid.NewGuid(), Guid.Empty)));
        }
        [TestMethod()]
        public async Task CardDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(db);
            using (var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB()))
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetCardVersions(dbContext, new TestLocalizer()).RunAsync(new GetCardVersions.Request(userId, Guid.NewGuid())));
        }
        [TestMethod()]
        public async Task FailIfUserCanNotViewCurrentVersion()
        {
            var db = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(db);
            var language = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, userId, language: language);    //Created public
            using (var dbContext = new MemCheckDbContext(db))
                await new UpdateCard(dbContext).RunAsync(UpdateCardHelper.RequestForVisibilityChange(card, userWithViewIds: new[] { userId }), new TestLocalizer());    //Now private
            var otherUserId = await UserHelper.CreateInDbAsync(db);
            using (var dbContext = new MemCheckDbContext(db))
            {
                var versions = await new GetCardVersions(dbContext, new TestLocalizer()).RunAsync(new GetCardVersions.Request(userId, card.Id));
                Assert.AreEqual(2, versions.Count());
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetCardVersions(dbContext, new TestLocalizer()).RunAsync(new GetCardVersions.Request(otherUserId, card.Id)));
            }
        }
    }
}

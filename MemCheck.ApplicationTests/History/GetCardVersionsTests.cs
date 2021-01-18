using MemCheck.Application.CardChanging;
using MemCheck.Application.QueryValidation;
using MemCheck.Application.Tests.Helpers;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.History
{
    [TestClass()]
    public class GetCardVersionsTests
    {
        [TestMethod()]
        public async Task UserNotLoggedIn()
        {
            using (var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB()))
                await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new GetCardVersions(dbContext).RunAsync(new GetCardVersions.Request(Guid.Empty, Guid.Empty)));
        }
        [TestMethod()]
        public async Task UserDoesNotExist()
        {
            using (var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB()))
                await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new GetCardVersions(dbContext).RunAsync(new GetCardVersions.Request(Guid.NewGuid(), Guid.Empty)));
        }
        [TestMethod()]
        public async Task CardDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(db);
            using (var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB()))
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetCardVersions(dbContext).RunAsync(new GetCardVersions.Request(userId, Guid.NewGuid())));
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
                var versions = await new GetCardVersions(dbContext).RunAsync(new GetCardVersions.Request(userId, card.Id));
                Assert.AreEqual(2, versions.Count());
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetCardVersions(dbContext).RunAsync(new GetCardVersions.Request(otherUserId, card.Id)));
            }
        }
        [TestMethod()]
        public async Task MultipleVersions()
        {
            var db = DbHelper.GetEmptyTestDB();
            var userName = StringHelper.RandomString();
            var userId = await UserHelper.CreateInDbAsync(db, userName: userName);
            var language = await CardLanguagHelper.CreateAsync(db);
            var oldestDate = DateHelper.Random();
            var oldestDescription = StringHelper.RandomString();
            var card = await CardHelper.CreateAsync(db, userId, language: language, versionDate: oldestDate, versionDescription: oldestDescription);

            var otherUserName = StringHelper.RandomString();
            var otherUserId = await UserHelper.CreateInDbAsync(db, userName: otherUserName);
            var intermediaryDate = DateHelper.Random();
            var intermediaryDescription = StringHelper.RandomString();
            using (var dbContext = new MemCheckDbContext(db))
            {
                var request = UpdateCardHelper.RequestForFrontSideChange(card, StringHelper.RandomString(), versionCreator: otherUserId, versionDescription: intermediaryDescription);
                request = request with { AdditionalInfo = StringHelper.RandomString() };
                await new UpdateCard(dbContext).RunAsync(request, new TestLocalizer(), intermediaryDate);
            }

            var newestDate = DateHelper.Random();
            var newestDescription = StringHelper.RandomString();
            using (var dbContext = new MemCheckDbContext(db))
            {
                var cardFromDb = await dbContext.Cards
                    .Include(c => c.VersionCreator)
                    .Include(c => c.VersionCreator)
                    .Include(c => c.Images)
                    .Include(c => c.CardLanguage)
                    .Include(c => c.TagsInCards)
                    .Include(c => c.UsersWithView)
                    .SingleAsync(c => c.Id == card.Id);
                await new UpdateCard(dbContext).RunAsync(UpdateCardHelper.RequestForBackSideChange(cardFromDb, StringHelper.RandomString(), versionDescription: newestDescription, versionCreator: userId), new TestLocalizer(), newestDate);
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var versions = (await new GetCardVersions(dbContext).RunAsync(new GetCardVersions.Request(otherUserId, card.Id))).ToList();

                Assert.AreEqual((await dbContext.CardPreviousVersions.SingleAsync(c => c.VersionUtcDate == oldestDate)).Id, versions[2].VersionId);
                Assert.AreEqual(oldestDate, versions[2].VersionUtcDate);
                Assert.AreEqual(userName, versions[2].VersionCreator);
                Assert.AreEqual(oldestDescription, versions[2].VersionDescription);
                Assert.AreEqual(5, versions[2].ChangedFieldNames.Count());

                Assert.AreEqual((await dbContext.CardPreviousVersions.SingleAsync(c => c.VersionUtcDate == intermediaryDate)).Id, versions[1].VersionId);
                Assert.AreEqual(intermediaryDate, versions[1].VersionUtcDate);
                Assert.AreEqual(otherUserName, versions[1].VersionCreator);
                Assert.AreEqual(intermediaryDescription, versions[1].VersionDescription);
                Assert.AreEqual(2, versions[1].ChangedFieldNames.Count());
                Assert.IsTrue(versions[1].ChangedFieldNames.Any(f => f == "FrontSide"));
                Assert.IsTrue(versions[1].ChangedFieldNames.Any(f => f == "AdditionalInfo"));

                Assert.IsNull(versions[0].VersionId);
                Assert.AreEqual(newestDate, versions[0].VersionUtcDate);
                Assert.AreEqual(userName, versions[0].VersionCreator);
                Assert.AreEqual(newestDescription, versions[0].VersionDescription);
                Assert.AreEqual(1, versions[0].ChangedFieldNames.Count());
                Assert.AreEqual("BackSide", versions[0].ChangedFieldNames.First());
            }
        }
    }
}

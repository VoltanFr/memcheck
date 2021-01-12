using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using MemCheck.Database;
using System.Linq;
using MemCheck.Application.Tests.Helpers;
using MemCheck.Application.CardChanging;
using Microsoft.EntityFrameworkCore;

namespace MemCheck.Application.History
{
    [TestClass()]
    public class GetCardDiffTests
    {
        [TestMethod()]
        public async Task EmptyDB_UserNotLoggedIn()
        {
            using (var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB()))
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetCardDiff(dbContext).RunAsync(new GetCardDiff.Request(Guid.Empty, Guid.Empty, Guid.Empty)));
        }
        [TestMethod()]
        public async Task EmptyDB_UserDoesNotExist()
        {
            using (var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB()))
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetCardDiff(dbContext).RunAsync(new GetCardDiff.Request(Guid.NewGuid(), Guid.Empty, Guid.Empty)));
        }
        [TestMethod()]
        public async Task EmptyDB_OriginalVersionDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(db);
            var language = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, userId, language: language);
            using (var dbContext = new MemCheckDbContext(db))
                await new UpdateCard(dbContext).RunAsync(UpdateCardHelper.RequestForFrontSideChange(card, StringHelper.RandomString()), new TestLocalizer());
            using (var dbContext = new MemCheckDbContext(db))
                await dbContext.CardPreviousVersions.Where(previous => previous.Card == card.Id).SingleAsync();
            using (var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB()))
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetCardDiff(dbContext).RunAsync(new GetCardDiff.Request(userId, card.Id, Guid.NewGuid())));
        }
        [TestMethod()]
        public async Task EmptyDB_CurrentVersionDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(db);
            var language = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, userId, language: language);
            using (var dbContext = new MemCheckDbContext(db))
                await new UpdateCard(dbContext).RunAsync(UpdateCardHelper.RequestForFrontSideChange(card, StringHelper.RandomString()), new TestLocalizer());
            Guid originalVersionId;
            using (var dbContext = new MemCheckDbContext(db))
                originalVersionId = (await dbContext.CardPreviousVersions.Where(previous => previous.Card == card.Id).SingleAsync()).Id;
            using (var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB()))
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetCardDiff(dbContext).RunAsync(new GetCardDiff.Request(userId, Guid.NewGuid(), originalVersionId)));
        }
        [TestMethod()]
        public async Task FailIfUserCanNotViewOriginalVersion()
        {
            var db = DbHelper.GetEmptyTestDB();
            var userName = StringHelper.RandomString();
            var userId = await UserHelper.CreateInDbAsync(db, userName: userName);
            var originalVersionDate = DateHelper.Random();
            var originalFrontSide = StringHelper.RandomString();
            var originalVersionDescription = StringHelper.RandomString();
            var language = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, userId, originalVersionDate, frontSide: originalFrontSide, language: language, versionDescription: originalVersionDescription, userWithViewIds: new[] { userId });
            var newFrontSide = StringHelper.RandomString();
            var newVersionDate = DateHelper.Random(originalVersionDate);
            var newVersionDescription = StringHelper.RandomString();
            using (var dbContext = new MemCheckDbContext(db))
                await new UpdateCard(dbContext).RunAsync(UpdateCardHelper.RequestForVisibilityChange(card, new Guid[0]), new TestLocalizer(), cardNewVersionUtcDate: newVersionDate);
            Guid previousVersionId;
            using (var dbContext = new MemCheckDbContext(db))
                previousVersionId = (await dbContext.CardPreviousVersions.Where(previous => previous.Card == card.Id).SingleAsync()).Id;
            var otherUserId = await UserHelper.CreateInDbAsync(db);
            using (var dbContext = new MemCheckDbContext(db))
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetCardDiff(dbContext).RunAsync(new GetCardDiff.Request(otherUserId, card.Id, previousVersionId)));
        }
        [TestMethod()]
        public async Task FailIfUserCanNotViewCurrentVersion()
        {
            var db = DbHelper.GetEmptyTestDB();
            var userName = StringHelper.RandomString();
            var userId = await UserHelper.CreateInDbAsync(db, userName: userName);
            var originalVersionDate = DateHelper.Random();
            var originalFrontSide = StringHelper.RandomString();
            var originalVersionDescription = StringHelper.RandomString();
            var language = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, userId, originalVersionDate, frontSide: originalFrontSide, language: language, versionDescription: originalVersionDescription);
            var newFrontSide = StringHelper.RandomString();
            var newVersionDate = DateHelper.Random(originalVersionDate);
            var newVersionDescription = StringHelper.RandomString();
            using (var dbContext = new MemCheckDbContext(db))
                await new UpdateCard(dbContext).RunAsync(UpdateCardHelper.RequestForVisibilityChange(card, userWithViewIds: new[] { userId }), new TestLocalizer(), cardNewVersionUtcDate: newVersionDate);
            Guid previousVersionId;
            using (var dbContext = new MemCheckDbContext(db))
                previousVersionId = (await dbContext.CardPreviousVersions.Where(previous => previous.Card == card.Id).SingleAsync()).Id;
            var otherUserId = await UserHelper.CreateInDbAsync(db);
            using (var dbContext = new MemCheckDbContext(db))
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetCardDiff(dbContext).RunAsync(new GetCardDiff.Request(otherUserId, card.Id, previousVersionId)));
        }
        [TestMethod()]
        public async Task FrontDiff()
        {
            var db = DbHelper.GetEmptyTestDB();
            var userName = StringHelper.RandomString();
            var userId = await UserHelper.CreateInDbAsync(db, userName: userName);
            var originalVersionDate = DateHelper.Random();
            var originalFrontSide = StringHelper.RandomString();
            var originalVersionDescription = StringHelper.RandomString();
            var language = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, userId, originalVersionDate, frontSide: originalFrontSide, language: language, versionDescription: originalVersionDescription);
            var newFrontSide = StringHelper.RandomString();
            var newVersionDate = DateHelper.Random(originalVersionDate);
            var newVersionDescription = StringHelper.RandomString();
            using (var dbContext = new MemCheckDbContext(db))
                await new UpdateCard(dbContext).RunAsync(UpdateCardHelper.RequestForFrontSideChange(card, newFrontSide, versionDescription: newVersionDescription), new TestLocalizer(), cardNewVersionUtcDate: newVersionDate);
            Guid previousVersionId;
            using (var dbContext = new MemCheckDbContext(db))
                previousVersionId = (await dbContext.CardPreviousVersions.Where(previous => previous.Card == card.Id).SingleAsync()).Id;
            using (var dbContext = new MemCheckDbContext(db))
            {
                var diffRequest = new GetCardDiff.Request(userId, card.Id, previousVersionId);
                var result = await new GetCardDiff(dbContext).RunAsync(diffRequest);
                Assert.AreEqual(userName, result.CurrentVersionCreator);
                Assert.AreEqual(userName, result.OriginalVersionCreator);
                Assert.AreEqual(newVersionDate, result.CurrentVersionUtcDate);
                Assert.AreEqual(originalVersionDate, result.OriginalVersionUtcDate);
                Assert.AreEqual(newVersionDescription, result.CurrentVersionDescription);
                Assert.AreEqual(originalVersionDescription, result.OriginalVersionDescription);
                Assert.AreEqual(new(newFrontSide, originalFrontSide), result.FrontSide);
                Assert.IsNull(result.Language);
                Assert.IsNull(result.BackSide);
                Assert.IsNull(result.AdditionalInfo);
                Assert.IsNull(result.Tags);
                Assert.IsNull(result.UsersWithView);
                Assert.IsNull(result.ImagesOnFrontSide);
                Assert.IsNull(result.ImagesOnBackSide);
                Assert.IsNull(result.ImagesOnAdditionalSide);
            }
        }
        [TestMethod()]
        public async Task BackDiff()
        {
            var db = DbHelper.GetEmptyTestDB();
            var userName = StringHelper.RandomString();
            var userId = await UserHelper.CreateInDbAsync(db, userName: userName);
            var originalVersionDate = DateHelper.Random();
            var originalBackSide = StringHelper.RandomString();
            var originalVersionDescription = StringHelper.RandomString();
            var language = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, userId, originalVersionDate, backSide: originalBackSide, language: language, versionDescription: originalVersionDescription);
            var newBackSide = StringHelper.RandomString();
            var newVersionDate = DateHelper.Random(originalVersionDate);
            var newVersionDescription = StringHelper.RandomString();
            using (var dbContext = new MemCheckDbContext(db))
                await new UpdateCard(dbContext).RunAsync(UpdateCardHelper.RequestForBackSideChange(card, newBackSide, versionDescription: newVersionDescription), new TestLocalizer(), cardNewVersionUtcDate: newVersionDate);
            Guid previousVersionId;
            using (var dbContext = new MemCheckDbContext(db))
                previousVersionId = (await dbContext.CardPreviousVersions.Where(previous => previous.Card == card.Id).SingleAsync()).Id;
            using (var dbContext = new MemCheckDbContext(db))
            {
                var diffRequest = new GetCardDiff.Request(userId, card.Id, previousVersionId);
                var result = await new GetCardDiff(dbContext).RunAsync(diffRequest);
                Assert.AreEqual(userName, result.CurrentVersionCreator);
                Assert.AreEqual(userName, result.OriginalVersionCreator);
                Assert.AreEqual(newVersionDate, result.CurrentVersionUtcDate);
                Assert.AreEqual(originalVersionDate, result.OriginalVersionUtcDate);
                Assert.AreEqual(newVersionDescription, result.CurrentVersionDescription);
                Assert.AreEqual(originalVersionDescription, result.OriginalVersionDescription);
                Assert.AreEqual(new(newBackSide, originalBackSide), result.BackSide);
                Assert.IsNull(result.Language);
                Assert.IsNull(result.FrontSide);
                Assert.IsNull(result.AdditionalInfo);
                Assert.IsNull(result.Tags);
                Assert.IsNull(result.UsersWithView);
                Assert.IsNull(result.ImagesOnFrontSide);
                Assert.IsNull(result.ImagesOnBackSide);
                Assert.IsNull(result.ImagesOnAdditionalSide);
            }
        }
        [TestMethod()]
        public async Task LanguageDiff()
        {
            var db = DbHelper.GetEmptyTestDB();
            var userName = StringHelper.RandomString();
            var userId = await UserHelper.CreateInDbAsync(db, userName: userName);
            var originalVersionDate = DateHelper.Random();
            var originalVersionDescription = StringHelper.RandomString();
            var originalLanguageName = StringHelper.RandomString();
            var originalLanguage = await CardLanguagHelper.CreateAsync(db, originalLanguageName);
            var card = await CardHelper.CreateAsync(db, userId, originalVersionDate, language: originalLanguage, versionDescription: originalVersionDescription);
            var newVersionDate = DateHelper.Random(originalVersionDate);
            var newVersionDescription = StringHelper.RandomString();
            var newLanguageName = StringHelper.RandomString();
            var newVersionLanguage = await CardLanguagHelper.CreateAsync(db, newLanguageName);
            using (var dbContext = new MemCheckDbContext(db))
                await new UpdateCard(dbContext).RunAsync(UpdateCardHelper.RequestForLanguageChange(card, newVersionLanguage, versionDescription: newVersionDescription), new TestLocalizer(), cardNewVersionUtcDate: newVersionDate);
            Guid previousVersionId;
            using (var dbContext = new MemCheckDbContext(db))
                previousVersionId = (await dbContext.CardPreviousVersions.Where(previous => previous.Card == card.Id).SingleAsync()).Id;
            using (var dbContext = new MemCheckDbContext(db))
            {
                var diffRequest = new GetCardDiff.Request(userId, card.Id, previousVersionId);
                var result = await new GetCardDiff(dbContext).RunAsync(diffRequest);
                Assert.AreEqual(userName, result.CurrentVersionCreator);
                Assert.AreEqual(userName, result.OriginalVersionCreator);
                Assert.AreEqual(newVersionDate, result.CurrentVersionUtcDate);
                Assert.AreEqual(originalVersionDate, result.OriginalVersionUtcDate);
                Assert.AreEqual(newVersionDescription, result.CurrentVersionDescription);
                Assert.AreEqual(originalVersionDescription, result.OriginalVersionDescription);
                Assert.AreEqual(new(newLanguageName, originalLanguageName), result.Language);
                Assert.IsNull(result.FrontSide);
                Assert.IsNull(result.BackSide);
                Assert.IsNull(result.AdditionalInfo);
                Assert.IsNull(result.Tags);
                Assert.IsNull(result.UsersWithView);
                Assert.IsNull(result.ImagesOnFrontSide);
                Assert.IsNull(result.ImagesOnBackSide);
                Assert.IsNull(result.ImagesOnAdditionalSide);
            }
        }
        [TestMethod()]
        public async Task AdditionalInfoDiff()
        {
            var db = DbHelper.GetEmptyTestDB();
            var userName = StringHelper.RandomString();
            var userId = await UserHelper.CreateInDbAsync(db, userName: userName);
            var originalVersionDate = DateHelper.Random();
            var originalAdditionInfo = StringHelper.RandomString();
            var originalVersionDescription = StringHelper.RandomString();
            var language = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, userId, originalVersionDate, additionalInfo: originalAdditionInfo, language: language, versionDescription: originalVersionDescription);
            var newAdditionalInfo = StringHelper.RandomString();
            var newVersionDate = DateHelper.Random(originalVersionDate);
            var newVersionDescription = StringHelper.RandomString();
            using (var dbContext = new MemCheckDbContext(db))
                await new UpdateCard(dbContext).RunAsync(UpdateCardHelper.RequestForAdditionalInfoChange(card, newAdditionalInfo, versionDescription: newVersionDescription), new TestLocalizer(), cardNewVersionUtcDate: newVersionDate);
            Guid previousVersionId;
            using (var dbContext = new MemCheckDbContext(db))
                previousVersionId = (await dbContext.CardPreviousVersions.Where(previous => previous.Card == card.Id).SingleAsync()).Id;
            using (var dbContext = new MemCheckDbContext(db))
            {
                var diffRequest = new GetCardDiff.Request(userId, card.Id, previousVersionId);
                var result = await new GetCardDiff(dbContext).RunAsync(diffRequest);
                Assert.AreEqual(userName, result.CurrentVersionCreator);
                Assert.AreEqual(userName, result.OriginalVersionCreator);
                Assert.AreEqual(newVersionDate, result.CurrentVersionUtcDate);
                Assert.AreEqual(originalVersionDate, result.OriginalVersionUtcDate);
                Assert.AreEqual(newVersionDescription, result.CurrentVersionDescription);
                Assert.AreEqual(originalVersionDescription, result.OriginalVersionDescription);
                Assert.AreEqual(new(newAdditionalInfo, originalAdditionInfo), result.AdditionalInfo);
                Assert.IsNull(result.FrontSide);
                Assert.IsNull(result.BackSide);
                Assert.IsNull(result.Language);
                Assert.IsNull(result.Tags);
                Assert.IsNull(result.UsersWithView);
                Assert.IsNull(result.ImagesOnFrontSide);
                Assert.IsNull(result.ImagesOnBackSide);
                Assert.IsNull(result.ImagesOnAdditionalSide);
            }
        }
        [TestMethod()]
        public async Task TagsDiff()
        {
            var db = DbHelper.GetEmptyTestDB();
            var userName = StringHelper.RandomString();
            var userId = await UserHelper.CreateInDbAsync(db, userName: userName);
            var originalVersionDate = DateHelper.Random();
            var originalVersionDescription = StringHelper.RandomString();
            var originalTagName1 = StringHelper.RandomString();
            var originalTagId1 = await TagHelper.CreateAsync(db, originalTagName1);
            var originalTagName2 = StringHelper.RandomString();
            var originalTagId2 = await TagHelper.CreateAsync(db, originalTagName2);
            var language = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, userId, originalVersionDate, tagIds: new[] { originalTagId1, originalTagId2 }, language: language, versionDescription: originalVersionDescription);
            var newVersionDate = DateHelper.Random(originalVersionDate);
            var newVersionDescription = StringHelper.RandomString();
            using (var dbContext = new MemCheckDbContext(db))
                await new UpdateCard(dbContext).RunAsync(UpdateCardHelper.RequestForTagChange(card, new[] { originalTagId1 }, versionDescription: newVersionDescription), new TestLocalizer(), cardNewVersionUtcDate: newVersionDate);
            Guid previousVersionId;
            using (var dbContext = new MemCheckDbContext(db))
                previousVersionId = (await dbContext.CardPreviousVersions.Where(previous => previous.Card == card.Id).SingleAsync()).Id;
            using (var dbContext = new MemCheckDbContext(db))
            {
                var diffRequest = new GetCardDiff.Request(userId, card.Id, previousVersionId);
                var result = await new GetCardDiff(dbContext).RunAsync(diffRequest);
                Assert.AreEqual(userName, result.CurrentVersionCreator);
                Assert.AreEqual(userName, result.OriginalVersionCreator);
                Assert.AreEqual(newVersionDate, result.CurrentVersionUtcDate);
                Assert.AreEqual(originalVersionDate, result.OriginalVersionUtcDate);
                Assert.AreEqual(newVersionDescription, result.CurrentVersionDescription);
                Assert.AreEqual(originalVersionDescription, result.OriginalVersionDescription);
                var expectedTagDiffString = string.Join(",", new[] { originalTagName1, originalTagName2 }.OrderBy(s => s));
                Assert.AreEqual(new(originalTagName1, expectedTagDiffString), result.Tags);
                Assert.IsNull(result.FrontSide);
                Assert.IsNull(result.BackSide);
                Assert.IsNull(result.Language);
                Assert.IsNull(result.AdditionalInfo);
                Assert.IsNull(result.UsersWithView);
                Assert.IsNull(result.ImagesOnFrontSide);
                Assert.IsNull(result.ImagesOnBackSide);
                Assert.IsNull(result.ImagesOnAdditionalSide);
            }
        }
    }
}

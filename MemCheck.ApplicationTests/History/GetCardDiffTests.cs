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
                await new UpdateCard(dbContext).RunAsync(UpdateCardHelper.RequestForVisibilityChange(card, Array.Empty<Guid>()), new TestLocalizer(), cardNewVersionUtcDate: newVersionDate);
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
        [TestMethod()]
        public async Task VisibilityDiff()
        {
            var db = DbHelper.GetEmptyTestDB();
            var userName = StringHelper.RandomString();
            var userId = await UserHelper.CreateInDbAsync(db, userName: userName);
            var originalVersionDate = DateHelper.Random();
            var originalVersionDescription = StringHelper.RandomString();
            var language = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, userId, originalVersionDate, userWithViewIds: new[] { userId }, language: language, versionDescription: originalVersionDescription);
            var newVersionDate = DateHelper.Random(originalVersionDate);
            var newVersionDescription = StringHelper.RandomString();
            using (var dbContext = new MemCheckDbContext(db))
                await new UpdateCard(dbContext).RunAsync(UpdateCardHelper.RequestForVisibilityChange(card, Array.Empty<Guid>(), versionDescription: newVersionDescription), new TestLocalizer(), cardNewVersionUtcDate: newVersionDate);
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
                Assert.AreEqual(new("", userName), result.UsersWithView);
                Assert.IsNull(result.FrontSide);
                Assert.IsNull(result.BackSide);
                Assert.IsNull(result.Language);
                Assert.IsNull(result.AdditionalInfo);
                Assert.IsNull(result.Tags);
                Assert.IsNull(result.ImagesOnFrontSide);
                Assert.IsNull(result.ImagesOnBackSide);
                Assert.IsNull(result.ImagesOnAdditionalSide);
            }
        }
        [TestMethod()]
        public async Task FrontSideImageDiff()
        {
            var db = DbHelper.GetEmptyTestDB();
            var userName = StringHelper.RandomString();
            var userId = await UserHelper.CreateInDbAsync(db, userName: userName);
            var originalVersionDate = DateHelper.Random();
            var originalVersionDescription = StringHelper.RandomString();
            var language = await CardLanguagHelper.CreateAsync(db);
            var imageName = StringHelper.RandomString();
            var image = await ImageHelper.CreateAsync(db, userId, imageName);
            var card = await CardHelper.CreateAsync(db, userId, originalVersionDate, language: language, frontSideImages: new[] { image }, versionDescription: originalVersionDescription);
            var newVersionDate = DateHelper.Random(originalVersionDate);
            var newVersionDescription = StringHelper.RandomString();
            using (var dbContext = new MemCheckDbContext(db))
                await new UpdateCard(dbContext).RunAsync(UpdateCardHelper.RequestForFrontSideImageChange(card, Array.Empty<Guid>(), versionDescription: newVersionDescription), new TestLocalizer(), cardNewVersionUtcDate: newVersionDate);
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
                Assert.AreEqual(new("", imageName), result.ImagesOnFrontSide);
                Assert.IsNull(result.FrontSide);
                Assert.IsNull(result.BackSide);
                Assert.IsNull(result.Language);
                Assert.IsNull(result.AdditionalInfo);
                Assert.IsNull(result.Tags);
                Assert.IsNull(result.UsersWithView);
                Assert.IsNull(result.ImagesOnBackSide);
                Assert.IsNull(result.ImagesOnAdditionalSide);
            }
        }
        [TestMethod()]
        public async Task BackSideImageDiff()
        {
            var db = DbHelper.GetEmptyTestDB();
            var userName = StringHelper.RandomString();
            var userId = await UserHelper.CreateInDbAsync(db, userName: userName);
            var originalVersionDate = DateHelper.Random();
            var originalVersionDescription = StringHelper.RandomString();
            var language = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, userId, originalVersionDate, language: language, versionDescription: originalVersionDescription);
            var newVersionDate = DateHelper.Random(originalVersionDate);
            var newVersionDescription = StringHelper.RandomString();
            var imageName = StringHelper.RandomString();
            var image = await ImageHelper.CreateAsync(db, userId, imageName);
            using (var dbContext = new MemCheckDbContext(db))
                await new UpdateCard(dbContext).RunAsync(UpdateCardHelper.RequestForBackSideImageChange(card, new[] { image }, versionDescription: newVersionDescription), new TestLocalizer(), cardNewVersionUtcDate: newVersionDate);
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
                Assert.AreEqual(new(imageName, ""), result.ImagesOnBackSide);
                Assert.IsNull(result.FrontSide);
                Assert.IsNull(result.BackSide);
                Assert.IsNull(result.Language);
                Assert.IsNull(result.AdditionalInfo);
                Assert.IsNull(result.Tags);
                Assert.IsNull(result.UsersWithView);
                Assert.IsNull(result.ImagesOnFrontSide);
                Assert.IsNull(result.ImagesOnAdditionalSide);
            }
        }
        [TestMethod()]
        public async Task AdditionalSideImageDiff()
        {
            var db = DbHelper.GetEmptyTestDB();
            var userName = StringHelper.RandomString();
            var userId = await UserHelper.CreateInDbAsync(db, userName: userName);
            var originalVersionDate = DateHelper.Random();
            var originalVersionDescription = StringHelper.RandomString();
            var language = await CardLanguagHelper.CreateAsync(db);
            var originalVersionImageName = StringHelper.RandomString();
            var originalVersionImage = await ImageHelper.CreateAsync(db, userId, originalVersionImageName);
            var card = await CardHelper.CreateAsync(db, userId, originalVersionDate, language: language, additionalSideImages: new[] { originalVersionImage }, versionDescription: originalVersionDescription);
            var newVersionDate = DateHelper.Random(originalVersionDate);
            var newVersionDescription = StringHelper.RandomString();
            var newVersionImageName = StringHelper.RandomString();
            var newVersionImage = await ImageHelper.CreateAsync(db, userId, newVersionImageName);
            using (var dbContext = new MemCheckDbContext(db))
                await new UpdateCard(dbContext).RunAsync(UpdateCardHelper.RequestForAdditionalSideImageChange(card, new[] { newVersionImage }, versionDescription: newVersionDescription), new TestLocalizer(), cardNewVersionUtcDate: newVersionDate);
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
                Assert.AreEqual(new(newVersionImageName, originalVersionImageName), result.ImagesOnAdditionalSide);
                Assert.IsNull(result.FrontSide);
                Assert.IsNull(result.BackSide);
                Assert.IsNull(result.Language);
                Assert.IsNull(result.AdditionalInfo);
                Assert.IsNull(result.Tags);
                Assert.IsNull(result.UsersWithView);
                Assert.IsNull(result.ImagesOnFrontSide);
                Assert.IsNull(result.ImagesOnBackSide);
            }
        }
        [TestMethod()]
        public async Task MultipleDiffs()
        {
            var db = DbHelper.GetEmptyTestDB();
            var userName = StringHelper.RandomString();
            var userId = await UserHelper.CreateInDbAsync(db, userName: userName);
            var originalVersionDate = DateHelper.Random();
            var originalVersionDescription = StringHelper.RandomString();
            var language = await CardLanguagHelper.CreateAsync(db);
            var originalVersionImageName = StringHelper.RandomString();
            var originalVersionImage = await ImageHelper.CreateAsync(db, userId, originalVersionImageName);
            var originalFrontSide = StringHelper.RandomString();
            var originalBackSide = StringHelper.RandomString();
            var originalTagName1 = StringHelper.RandomString();
            var originalTagId1 = await TagHelper.CreateAsync(db, originalTagName1);
            var originalTagName2 = StringHelper.RandomString();
            var originalTagId2 = await TagHelper.CreateAsync(db, originalTagName2);
            var card = await CardHelper.CreateAsync(db, userId, originalVersionDate, language: language, frontSide: originalFrontSide, backSide: originalBackSide, tagIds: new[] { originalTagId1, originalTagId2 }, additionalSideImages: new[] { originalVersionImage }, versionDescription: originalVersionDescription);

            var newVersionDescription = StringHelper.RandomString();
            var newVersionDate = DateHelper.Random(originalVersionDate);
            var newFrontSide = StringHelper.RandomString();
            var newBackSide = StringHelper.RandomString();
            var newVersionImageName = StringHelper.RandomString();
            var newVersionImage = await ImageHelper.CreateAsync(db, userId, newVersionImageName);
            using (var dbContext = new MemCheckDbContext(db))
            {
                var r = UpdateCardHelper.RequestForFrontSideChange(card, newFrontSide, versionDescription: newVersionDescription);
                r = r with { BackSide = newBackSide };
                r = r with { AdditionalInfoImageList = new[] { newVersionImage } };
                r = r with { Tags = new[] { originalTagId1 } };
                await new UpdateCard(dbContext).RunAsync(r, new TestLocalizer(), cardNewVersionUtcDate: newVersionDate);
            }

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
                Assert.AreEqual(new(newVersionImageName, originalVersionImageName), result.ImagesOnAdditionalSide);
                Assert.AreEqual(new(newFrontSide, originalFrontSide), result.FrontSide);
                Assert.AreEqual(new(newBackSide, originalBackSide), result.BackSide);
                var expectedTagDiffString = string.Join(",", new[] { originalTagName1, originalTagName2 }.OrderBy(s => s));
                Assert.AreEqual(new(originalTagName1, expectedTagDiffString), result.Tags);
                Assert.IsNull(result.Language);
                Assert.IsNull(result.AdditionalInfo);
                Assert.IsNull(result.UsersWithView);
                Assert.IsNull(result.ImagesOnFrontSide);
                Assert.IsNull(result.ImagesOnBackSide);
            }
        }
        [TestMethod()]
        public async Task MultipleVersions()
        {
            var db = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(db);
            var initialVersionDate = DateHelper.Random();
            var intialVersionDescription = StringHelper.RandomString();
            var language = await CardLanguagHelper.CreateAsync(db);
            var initialFrontSide = StringHelper.RandomString();
            var card = await CardHelper.CreateAsync(db, userId, initialVersionDate, language: language, frontSide: initialFrontSide, versionDescription: intialVersionDescription);

            var intermediaryVersionDescription = StringHelper.RandomString();
            var intermediaryVersionDate = DateHelper.Random(initialVersionDate);
            var intermediaryFrontSide = StringHelper.RandomString();
            using (var dbContext = new MemCheckDbContext(db))
                await new UpdateCard(dbContext).RunAsync(UpdateCardHelper.RequestForFrontSideChange(card, intermediaryFrontSide, versionDescription: intermediaryVersionDescription), new TestLocalizer(), cardNewVersionUtcDate: intermediaryVersionDate);

            Guid initialVersionId;
            using (var dbContext = new MemCheckDbContext(db))
                initialVersionId = (await dbContext.CardPreviousVersions.SingleAsync()).Id;

            var currentVersionDescription = StringHelper.RandomString();
            var currentVersionDate = DateHelper.Random(initialVersionDate);
            using (var dbContext = new MemCheckDbContext(db))
                await new UpdateCard(dbContext).RunAsync(UpdateCardHelper.RequestForFrontSideChange(card, initialFrontSide, versionDescription: currentVersionDescription), new TestLocalizer(), cardNewVersionUtcDate: currentVersionDate);

            Guid intermediaryVersionId;
            using (var dbContext = new MemCheckDbContext(db))
                intermediaryVersionId = (await dbContext.CardPreviousVersions.SingleAsync(c => c.Id != initialVersionId)).Id;

            using (var dbContext = new MemCheckDbContext(db))
            {
                var diffRequest = new GetCardDiff.Request(userId, card.Id, intermediaryVersionId);
                var result = await new GetCardDiff(dbContext).RunAsync(diffRequest);
                Assert.AreEqual(currentVersionDate, result.CurrentVersionUtcDate);
                Assert.AreEqual(intermediaryVersionDate, result.OriginalVersionUtcDate);
                Assert.AreEqual(currentVersionDescription, result.CurrentVersionDescription);
                Assert.AreEqual(intermediaryVersionDescription, result.OriginalVersionDescription);
                Assert.AreEqual(new(initialFrontSide, intermediaryFrontSide), result.FrontSide);
                Assert.IsNull(result.Language);
                Assert.IsNull(result.BackSide);
                Assert.IsNull(result.Tags);
                Assert.IsNull(result.AdditionalInfo);
                Assert.IsNull(result.UsersWithView);
                Assert.IsNull(result.ImagesOnFrontSide);
                Assert.IsNull(result.ImagesOnBackSide);
                Assert.IsNull(result.ImagesOnAdditionalSide);
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var diffRequest = new GetCardDiff.Request(userId, card.Id, initialVersionId);
                var result = await new GetCardDiff(dbContext).RunAsync(diffRequest);
                Assert.AreEqual(currentVersionDate, result.CurrentVersionUtcDate);
                Assert.AreEqual(initialVersionDate, result.OriginalVersionUtcDate);
                Assert.AreEqual(currentVersionDescription, result.CurrentVersionDescription);
                Assert.AreEqual(intialVersionDescription, result.OriginalVersionDescription);
                Assert.IsNull(result.Language);
                Assert.IsNull(result.FrontSide);
                Assert.IsNull(result.BackSide);
                Assert.IsNull(result.Tags);
                Assert.IsNull(result.AdditionalInfo);
                Assert.IsNull(result.UsersWithView);
                Assert.IsNull(result.ImagesOnFrontSide);
                Assert.IsNull(result.ImagesOnBackSide);
                Assert.IsNull(result.ImagesOnAdditionalSide);
            }
        }
    }
}

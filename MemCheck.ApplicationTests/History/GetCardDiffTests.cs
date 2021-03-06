﻿using MemCheck.Application.Cards;
using MemCheck.Application.Tests.Helpers;
using MemCheck.Basics;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.History
{
    [TestClass()]
    public class GetCardDiffTests
    {
        [TestMethod()]
        public async Task EmptyDB_UserNotLoggedIn()
        {
            using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetCardDiff(dbContext).RunAsync(new GetCardDiff.Request(Guid.Empty, Guid.Empty, Guid.Empty)));
        }
        [TestMethod()]
        public async Task EmptyDB_UserDoesNotExist()
        {
            using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
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
                await new UpdateCard(dbContext).RunAsync(UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String()), new TestLocalizer());
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
                await new UpdateCard(dbContext).RunAsync(UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String()), new TestLocalizer());
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
            var userName = RandomHelper.String();
            var userId = await UserHelper.CreateInDbAsync(db, userName: userName);
            var originalVersionDate = RandomHelper.Date();
            var originalFrontSide = RandomHelper.String();
            var originalVersionDescription = RandomHelper.String();
            var language = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, userId, originalVersionDate, frontSide: originalFrontSide, language: language, versionDescription: originalVersionDescription, userWithViewIds: userId.AsArray());
            var newFrontSide = RandomHelper.String();
            var newVersionDate = RandomHelper.Date(originalVersionDate);
            var newVersionDescription = RandomHelper.String();
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
            var userName = RandomHelper.String();
            var userId = await UserHelper.CreateInDbAsync(db, userName: userName);
            var originalVersionDate = RandomHelper.Date();
            var originalFrontSide = RandomHelper.String();
            var originalVersionDescription = RandomHelper.String();
            var language = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, userId, originalVersionDate, frontSide: originalFrontSide, language: language, versionDescription: originalVersionDescription);
            var newFrontSide = RandomHelper.String();
            var newVersionDate = RandomHelper.Date(originalVersionDate);
            var newVersionDescription = RandomHelper.String();
            using (var dbContext = new MemCheckDbContext(db))
                await new UpdateCard(dbContext).RunAsync(UpdateCardHelper.RequestForVisibilityChange(card, userWithViewIds: userId.AsArray()), new TestLocalizer(), cardNewVersionUtcDate: newVersionDate);
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
            var userName = RandomHelper.String();
            var userId = await UserHelper.CreateInDbAsync(db, userName: userName);
            var originalVersionDate = RandomHelper.Date();
            var originalFrontSide = RandomHelper.String();
            var originalVersionDescription = RandomHelper.String();
            var language = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, userId, originalVersionDate, frontSide: originalFrontSide, language: language, versionDescription: originalVersionDescription);
            var newFrontSide = RandomHelper.String();
            var newVersionDate = RandomHelper.Date(originalVersionDate);
            var newVersionDescription = RandomHelper.String();
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
            var userName = RandomHelper.String();
            var userId = await UserHelper.CreateInDbAsync(db, userName: userName);
            var originalVersionDate = RandomHelper.Date();
            var originalBackSide = RandomHelper.String();
            var originalVersionDescription = RandomHelper.String();
            var language = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, userId, originalVersionDate, backSide: originalBackSide, language: language, versionDescription: originalVersionDescription);
            var newBackSide = RandomHelper.String();
            var newVersionDate = RandomHelper.Date(originalVersionDate);
            var newVersionDescription = RandomHelper.String();
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
            var userName = RandomHelper.String();
            var userId = await UserHelper.CreateInDbAsync(db, userName: userName);
            var originalVersionDate = RandomHelper.Date();
            var originalVersionDescription = RandomHelper.String();
            var originalLanguageName = RandomHelper.String();
            var originalLanguage = await CardLanguagHelper.CreateAsync(db, originalLanguageName);
            var card = await CardHelper.CreateAsync(db, userId, originalVersionDate, language: originalLanguage, versionDescription: originalVersionDescription);
            var newVersionDate = RandomHelper.Date(originalVersionDate);
            var newVersionDescription = RandomHelper.String();
            var newLanguageName = RandomHelper.String();
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
            var userName = RandomHelper.String();
            var userId = await UserHelper.CreateInDbAsync(db, userName: userName);
            var originalVersionDate = RandomHelper.Date();
            var originalAdditionInfo = RandomHelper.String();
            var originalVersionDescription = RandomHelper.String();
            var language = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, userId, originalVersionDate, additionalInfo: originalAdditionInfo, language: language, versionDescription: originalVersionDescription);
            var newAdditionalInfo = RandomHelper.String();
            var newVersionDate = RandomHelper.Date(originalVersionDate);
            var newVersionDescription = RandomHelper.String();
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
            var userName = RandomHelper.String();
            var userId = await UserHelper.CreateInDbAsync(db, userName: userName);
            var originalVersionDate = RandomHelper.Date();
            var originalVersionDescription = RandomHelper.String();
            var originalTagName1 = RandomHelper.String();
            var originalTagId1 = await TagHelper.CreateAsync(db, originalTagName1);
            var originalTagName2 = RandomHelper.String();
            var originalTagId2 = await TagHelper.CreateAsync(db, originalTagName2);
            var language = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, userId, originalVersionDate, tagIds: new[] { originalTagId1, originalTagId2 }, language: language, versionDescription: originalVersionDescription);
            var newVersionDate = RandomHelper.Date(originalVersionDate);
            var newVersionDescription = RandomHelper.String();
            using (var dbContext = new MemCheckDbContext(db))
                await new UpdateCard(dbContext).RunAsync(UpdateCardHelper.RequestForTagChange(card, originalTagId1.AsArray(), versionDescription: newVersionDescription), new TestLocalizer(), cardNewVersionUtcDate: newVersionDate);
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
            var userName = RandomHelper.String();
            var userId = await UserHelper.CreateInDbAsync(db, userName: userName);
            var originalVersionDate = RandomHelper.Date();
            var originalVersionDescription = RandomHelper.String();
            var language = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, userId, originalVersionDate, userWithViewIds: userId.AsArray(), language: language, versionDescription: originalVersionDescription);
            var newVersionDate = RandomHelper.Date(originalVersionDate);
            var newVersionDescription = RandomHelper.String();
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
            var userName = RandomHelper.String();
            var userId = await UserHelper.CreateInDbAsync(db, userName: userName);
            var originalVersionDate = RandomHelper.Date();
            var originalVersionDescription = RandomHelper.String();
            var language = await CardLanguagHelper.CreateAsync(db);
            var imageName = RandomHelper.String();
            var image = await ImageHelper.CreateAsync(db, userId, imageName);
            var card = await CardHelper.CreateAsync(db, userId, originalVersionDate, language: language, frontSideImages: image.AsArray(), versionDescription: originalVersionDescription);
            var newVersionDate = RandomHelper.Date(originalVersionDate);
            var newVersionDescription = RandomHelper.String();
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
            var userName = RandomHelper.String();
            var userId = await UserHelper.CreateInDbAsync(db, userName: userName);
            var originalVersionDate = RandomHelper.Date();
            var originalVersionDescription = RandomHelper.String();
            var language = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, userId, originalVersionDate, language: language, versionDescription: originalVersionDescription);
            var newVersionDate = RandomHelper.Date(originalVersionDate);
            var newVersionDescription = RandomHelper.String();
            var imageName = RandomHelper.String();
            var image = await ImageHelper.CreateAsync(db, userId, imageName);
            using (var dbContext = new MemCheckDbContext(db))
                await new UpdateCard(dbContext).RunAsync(UpdateCardHelper.RequestForBackSideImageChange(card, image.AsArray(), versionDescription: newVersionDescription), new TestLocalizer(), cardNewVersionUtcDate: newVersionDate);
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
            var userName = RandomHelper.String();
            var userId = await UserHelper.CreateInDbAsync(db, userName: userName);
            var originalVersionDate = RandomHelper.Date();
            var originalVersionDescription = RandomHelper.String();
            var language = await CardLanguagHelper.CreateAsync(db);
            var originalVersionImageName = RandomHelper.String();
            var originalVersionImage = await ImageHelper.CreateAsync(db, userId, originalVersionImageName);
            var card = await CardHelper.CreateAsync(db, userId, originalVersionDate, language: language, additionalSideImages: originalVersionImage.AsArray(), versionDescription: originalVersionDescription);
            var newVersionDate = RandomHelper.Date(originalVersionDate);
            var newVersionDescription = RandomHelper.String();
            var newVersionImageName = RandomHelper.String();
            var newVersionImage = await ImageHelper.CreateAsync(db, userId, newVersionImageName);
            using (var dbContext = new MemCheckDbContext(db))
                await new UpdateCard(dbContext).RunAsync(UpdateCardHelper.RequestForAdditionalSideImageChange(card, newVersionImage.AsArray(), versionDescription: newVersionDescription), new TestLocalizer(), cardNewVersionUtcDate: newVersionDate);
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
            var userName = RandomHelper.String();
            var userId = await UserHelper.CreateInDbAsync(db, userName: userName);
            var originalVersionDate = RandomHelper.Date();
            var originalVersionDescription = RandomHelper.String();
            var language = await CardLanguagHelper.CreateAsync(db);
            var originalVersionImageName = RandomHelper.String();
            var originalVersionImage = await ImageHelper.CreateAsync(db, userId, originalVersionImageName);
            var originalFrontSide = RandomHelper.String();
            var originalBackSide = RandomHelper.String();
            var originalTagName1 = RandomHelper.String();
            var originalTagId1 = await TagHelper.CreateAsync(db, originalTagName1);
            var originalTagName2 = RandomHelper.String();
            var originalTagId2 = await TagHelper.CreateAsync(db, originalTagName2);
            var card = await CardHelper.CreateAsync(db, userId, originalVersionDate, language: language, frontSide: originalFrontSide, backSide: originalBackSide, tagIds: new[] { originalTagId1, originalTagId2 }, additionalSideImages: originalVersionImage.AsArray(), versionDescription: originalVersionDescription);

            var newVersionDescription = RandomHelper.String();
            var newVersionDate = RandomHelper.Date(originalVersionDate);
            var newFrontSide = RandomHelper.String();
            var newBackSide = RandomHelper.String();
            var newVersionImageName = RandomHelper.String();
            var newVersionImage = await ImageHelper.CreateAsync(db, userId, newVersionImageName);
            using (var dbContext = new MemCheckDbContext(db))
            {
                var r = UpdateCardHelper.RequestForFrontSideChange(card, newFrontSide, versionDescription: newVersionDescription);
                r = r with { BackSide = newBackSide };
                r = r with { AdditionalInfoImageList = newVersionImage.AsArray() };
                r = r with { Tags = originalTagId1.AsArray() };
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
            var initialVersionDate = RandomHelper.Date();
            var intialVersionDescription = RandomHelper.String();
            var language = await CardLanguagHelper.CreateAsync(db);
            var initialFrontSide = RandomHelper.String();
            var card = await CardHelper.CreateAsync(db, userId, initialVersionDate, language: language, frontSide: initialFrontSide, versionDescription: intialVersionDescription);

            var intermediaryVersionDescription = RandomHelper.String();
            var intermediaryVersionDate = RandomHelper.Date(initialVersionDate);
            var intermediaryFrontSide = RandomHelper.String();
            using (var dbContext = new MemCheckDbContext(db))
                await new UpdateCard(dbContext).RunAsync(UpdateCardHelper.RequestForFrontSideChange(card, intermediaryFrontSide, versionDescription: intermediaryVersionDescription), new TestLocalizer(), cardNewVersionUtcDate: intermediaryVersionDate);

            Guid initialVersionId;
            using (var dbContext = new MemCheckDbContext(db))
                initialVersionId = (await dbContext.CardPreviousVersions.SingleAsync()).Id;

            var currentVersionDescription = RandomHelper.String();
            var currentVersionDate = RandomHelper.Date(initialVersionDate);
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

﻿using MemCheck.Application.Cards;
using MemCheck.Application.Helpers;
using MemCheck.Basics;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.History;

[TestClass()]
public class GetCardDiffTests
{
    [TestMethod()]
    public async Task EmptyDB_UserNotLoggedIn()
    {
        using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetCardDiff(dbContext.AsCallContext()).RunAsync(new GetCardDiff.Request(Guid.Empty, Guid.Empty, Guid.Empty)));
    }
    [TestMethod()]
    public async Task EmptyDB_UserDoesNotExist()
    {
        using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetCardDiff(dbContext.AsCallContext()).RunAsync(new GetCardDiff.Request(Guid.NewGuid(), Guid.Empty, Guid.Empty)));
    }
    [TestMethod()]
    public async Task EmptyDB_OriginalVersionDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var language = await CardLanguageHelper.CreateAsync(db);
        var card = await CardHelper.CreateAsync(db, userId, language: language);
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateCard(dbContext.AsCallContext()).RunAsync(UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String()));
        using (var dbContext = new MemCheckDbContext(db))
            await dbContext.CardPreviousVersions.Where(previous => previous.Card == card.Id).SingleAsync();
        using (var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB()))
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetCardDiff(dbContext.AsCallContext()).RunAsync(new GetCardDiff.Request(userId, card.Id, Guid.NewGuid())));
    }
    [TestMethod()]
    public async Task EmptyDB_CurrentVersionDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var language = await CardLanguageHelper.CreateAsync(db);
        var card = await CardHelper.CreateAsync(db, userId, language: language);
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateCard(dbContext.AsCallContext()).RunAsync(UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String()));
        Guid originalVersionId;
        using (var dbContext = new MemCheckDbContext(db))
            originalVersionId = (await dbContext.CardPreviousVersions.Where(previous => previous.Card == card.Id).SingleAsync()).Id;
        using (var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB()))
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetCardDiff(dbContext.AsCallContext()).RunAsync(new GetCardDiff.Request(userId, Guid.NewGuid(), originalVersionId)));
    }
    [TestMethod()]
    public async Task FailIfUserCanNotViewOriginalVersion()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var originalVersionDate = RandomHelper.Date();
        var originalFrontSide = RandomHelper.String();
        var originalVersionDescription = RandomHelper.String();
        var language = await CardLanguageHelper.CreateAsync(db);
        var card = await CardHelper.CreateAsync(db, user.Id, originalVersionDate, frontSide: originalFrontSide, language: language, versionDescription: originalVersionDescription, userWithViewIds: user.Id.AsArray());
        var newFrontSide = RandomHelper.String();
        var newVersionDate = RandomHelper.Date(originalVersionDate);
        var newVersionDescription = RandomHelper.String();
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateCard(dbContext.AsCallContext(), newVersionDate).RunAsync(UpdateCardHelper.RequestForVisibilityChange(card, Array.Empty<Guid>()));
        Guid previousVersionId;
        using (var dbContext = new MemCheckDbContext(db))
            previousVersionId = (await dbContext.CardPreviousVersions.Where(previous => previous.Card == card.Id).SingleAsync()).Id;
        var otherUserId = await UserHelper.CreateInDbAsync(db);
        using (var dbContext = new MemCheckDbContext(db))
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetCardDiff(dbContext.AsCallContext()).RunAsync(new GetCardDiff.Request(otherUserId, card.Id, previousVersionId)));
    }
    [TestMethod()]
    public async Task FailIfUserCanNotViewCurrentVersion()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var originalVersionDate = RandomHelper.Date();
        var originalFrontSide = RandomHelper.String();
        var originalVersionDescription = RandomHelper.String();
        var language = await CardLanguageHelper.CreateAsync(db);
        var card = await CardHelper.CreateAsync(db, user, originalVersionDate, frontSide: originalFrontSide, language: language, versionDescription: originalVersionDescription);
        var newFrontSide = RandomHelper.String();
        var newVersionDate = RandomHelper.Date(originalVersionDate);
        var newVersionDescription = RandomHelper.String();
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateCard(dbContext.AsCallContext(), newVersionDate).RunAsync(UpdateCardHelper.RequestForVisibilityChange(card, userWithViewIds: user.AsArray()));
        Guid previousVersionId;
        using (var dbContext = new MemCheckDbContext(db))
            previousVersionId = (await dbContext.CardPreviousVersions.Where(previous => previous.Card == card.Id).SingleAsync()).Id;
        var otherUserId = await UserHelper.CreateInDbAsync(db);
        using (var dbContext = new MemCheckDbContext(db))
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetCardDiff(dbContext.AsCallContext()).RunAsync(new GetCardDiff.Request(otherUserId, card.Id, previousVersionId)));
    }
    [TestMethod()]
    public async Task FrontDiff_OriginalIsPublic()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var originalVersionDate = RandomHelper.Date();
        var originalFrontSide = RandomHelper.String();
        var originalVersionDescription = RandomHelper.String();
        var language = await CardLanguageHelper.CreateAsync(db);
        var card = await CardHelper.CreateAsync(db, user.Id, originalVersionDate, frontSide: originalFrontSide, language: language, versionDescription: originalVersionDescription);
        var newFrontSide = RandomHelper.String();
        var newVersionDate = RandomHelper.Date(originalVersionDate);
        var newVersionDescription = RandomHelper.String();
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateCard(dbContext.AsCallContext(), newVersionDate).RunAsync(UpdateCardHelper.RequestForFrontSideChange(card, newFrontSide, versionDescription: newVersionDescription));
        Guid previousVersionId;
        using (var dbContext = new MemCheckDbContext(db))
            previousVersionId = (await dbContext.CardPreviousVersions.Where(previous => previous.Card == card.Id).SingleAsync()).Id;
        using (var dbContext = new MemCheckDbContext(db))
        {
            var diffRequest = new GetCardDiff.Request(user.Id, card.Id, previousVersionId);
            var result = await new GetCardDiff(dbContext.AsCallContext()).RunAsync(diffRequest);
            Assert.AreEqual(user.UserName, result.CurrentVersionCreator);
            Assert.AreEqual(user.UserName, result.OriginalVersionCreator);
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
        }
    }
    [TestMethod()]
    public async Task FrontDiff_OriginalCardHasVisibilityList()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var allowedUserId = await UserHelper.CreateInDbAsync(db);
        var originalVersionDate = RandomHelper.Date();
        var originalFrontSide = RandomHelper.String();
        var originalVersionDescription = RandomHelper.String();
        var language = await CardLanguageHelper.CreateAsync(db);
        var card = await CardHelper.CreateAsync(db, user.Id, originalVersionDate, frontSide: originalFrontSide, language: language, versionDescription: originalVersionDescription, userWithViewIds: new[] { user.Id, allowedUserId });
        var newFrontSide = RandomHelper.String();
        var newVersionDate = RandomHelper.Date(originalVersionDate);
        var newVersionDescription = RandomHelper.String();
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateCard(dbContext.AsCallContext(), newVersionDate).RunAsync(UpdateCardHelper.RequestForFrontSideChange(card, newFrontSide, versionDescription: newVersionDescription));
        Guid previousVersionId;
        using (var dbContext = new MemCheckDbContext(db))
            previousVersionId = (await dbContext.CardPreviousVersions.Where(previous => previous.Card == card.Id).SingleAsync()).Id;
        using (var dbContext = new MemCheckDbContext(db))
        {
            var diffRequest = new GetCardDiff.Request(user.Id, card.Id, previousVersionId);
            var result = await new GetCardDiff(dbContext.AsCallContext()).RunAsync(diffRequest);
            Assert.AreEqual(user.UserName, result.CurrentVersionCreator);
            Assert.AreEqual(user.UserName, result.OriginalVersionCreator);
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
        }
    }
    [TestMethod()]
    public async Task BackDiff()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var originalVersionDate = RandomHelper.Date();
        var originalBackSide = RandomHelper.String();
        var originalVersionDescription = RandomHelper.String();
        var language = await CardLanguageHelper.CreateAsync(db);
        var card = await CardHelper.CreateAsync(db, user.Id, originalVersionDate, backSide: originalBackSide, language: language, versionDescription: originalVersionDescription);
        var newBackSide = RandomHelper.String();
        var newVersionDate = RandomHelper.Date(originalVersionDate);
        var newVersionDescription = RandomHelper.String();
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateCard(dbContext.AsCallContext(), newVersionDate).RunAsync(UpdateCardHelper.RequestForBackSideChange(card, newBackSide, versionDescription: newVersionDescription));
        Guid previousVersionId;
        using (var dbContext = new MemCheckDbContext(db))
            previousVersionId = (await dbContext.CardPreviousVersions.Where(previous => previous.Card == card.Id).SingleAsync()).Id;
        using (var dbContext = new MemCheckDbContext(db))
        {
            var diffRequest = new GetCardDiff.Request(user.Id, card.Id, previousVersionId);
            var result = await new GetCardDiff(dbContext.AsCallContext()).RunAsync(diffRequest);
            Assert.AreEqual(user.UserName, result.CurrentVersionCreator);
            Assert.AreEqual(user.UserName, result.OriginalVersionCreator);
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
        }
    }
    [TestMethod()]
    public async Task LanguageDiff()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var originalVersionDate = RandomHelper.Date();
        var originalVersionDescription = RandomHelper.String();
        var originalLanguageName = RandomHelper.String();
        var originalLanguage = await CardLanguageHelper.CreateAsync(db, originalLanguageName);
        var card = await CardHelper.CreateAsync(db, user.Id, originalVersionDate, language: originalLanguage, versionDescription: originalVersionDescription);
        var newVersionDate = RandomHelper.Date(originalVersionDate);
        var newVersionDescription = RandomHelper.String();
        var newLanguageName = RandomHelper.String();
        var newVersionLanguage = await CardLanguageHelper.CreateAsync(db, newLanguageName);
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateCard(dbContext.AsCallContext(), newVersionDate).RunAsync(UpdateCardHelper.RequestForLanguageChange(card, newVersionLanguage, versionDescription: newVersionDescription));
        Guid previousVersionId;
        using (var dbContext = new MemCheckDbContext(db))
            previousVersionId = (await dbContext.CardPreviousVersions.Where(previous => previous.Card == card.Id).SingleAsync()).Id;
        using (var dbContext = new MemCheckDbContext(db))
        {
            var diffRequest = new GetCardDiff.Request(user.Id, card.Id, previousVersionId);
            var result = await new GetCardDiff(dbContext.AsCallContext()).RunAsync(diffRequest);
            Assert.AreEqual(user.UserName, result.CurrentVersionCreator);
            Assert.AreEqual(user.UserName, result.OriginalVersionCreator);
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
        }
    }
    [TestMethod()]
    public async Task AdditionalInfoDiff()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var originalVersionDate = RandomHelper.Date();
        var originalAdditionInfo = RandomHelper.String();
        var originalVersionDescription = RandomHelper.String();
        var language = await CardLanguageHelper.CreateAsync(db);
        var card = await CardHelper.CreateAsync(db, user.Id, originalVersionDate, additionalInfo: originalAdditionInfo, language: language, versionDescription: originalVersionDescription);
        var newAdditionalInfo = RandomHelper.String();
        var newVersionDate = RandomHelper.Date(originalVersionDate);
        var newVersionDescription = RandomHelper.String();
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateCard(dbContext.AsCallContext(), newVersionDate).RunAsync(UpdateCardHelper.RequestForAdditionalInfoChange(card, newAdditionalInfo, versionDescription: newVersionDescription));
        Guid previousVersionId;
        using (var dbContext = new MemCheckDbContext(db))
            previousVersionId = (await dbContext.CardPreviousVersions.Where(previous => previous.Card == card.Id).SingleAsync()).Id;
        using (var dbContext = new MemCheckDbContext(db))
        {
            var diffRequest = new GetCardDiff.Request(user.Id, card.Id, previousVersionId);
            var result = await new GetCardDiff(dbContext.AsCallContext()).RunAsync(diffRequest);
            Assert.AreEqual(user.UserName, result.CurrentVersionCreator);
            Assert.AreEqual(user.UserName, result.OriginalVersionCreator);
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
        }
    }
    [TestMethod()]
    public async Task ReferencesDiff()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var originalVersionDate = RandomHelper.Date();
        var originalReferences = RandomHelper.String();
        var originalVersionDescription = RandomHelper.String();
        var language = await CardLanguageHelper.CreateAsync(db);
        var card = await CardHelper.CreateAsync(db, user.Id, originalVersionDate, references: originalReferences, language: language, versionDescription: originalVersionDescription);
        var additionalInfo = RandomHelper.String();
        var newReferences = RandomHelper.String();
        var newVersionDate = RandomHelper.Date(originalVersionDate);
        var newVersionDescription = RandomHelper.String();
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateCard(dbContext.AsCallContext(), newVersionDate).RunAsync(UpdateCardHelper.RequestForReferencesChange(card, newReferences, versionDescription: newVersionDescription));
        Guid previousVersionId;
        using (var dbContext = new MemCheckDbContext(db))
            previousVersionId = (await dbContext.CardPreviousVersions.Where(previous => previous.Card == card.Id).SingleAsync()).Id;
        using (var dbContext = new MemCheckDbContext(db))
        {
            var diffRequest = new GetCardDiff.Request(user.Id, card.Id, previousVersionId);
            var result = await new GetCardDiff(dbContext.AsCallContext()).RunAsync(diffRequest);
            Assert.AreEqual(user.UserName, result.CurrentVersionCreator);
            Assert.AreEqual(user.UserName, result.OriginalVersionCreator);
            Assert.AreEqual(newVersionDate, result.CurrentVersionUtcDate);
            Assert.AreEqual(originalVersionDate, result.OriginalVersionUtcDate);
            Assert.AreEqual(newVersionDescription, result.CurrentVersionDescription);
            Assert.AreEqual(originalVersionDescription, result.OriginalVersionDescription);
            Assert.AreEqual(new(newReferences, originalReferences), result.References);
            Assert.IsNull(result.FrontSide);
            Assert.IsNull(result.BackSide);
            Assert.IsNull(result.Language);
            Assert.IsNull(result.AdditionalInfo);
            Assert.IsNull(result.Tags);
            Assert.IsNull(result.UsersWithView);
        }
    }
    [TestMethod()]
    public async Task TagsDiff()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var originalVersionDate = RandomHelper.Date();
        var originalVersionDescription = RandomHelper.String();
        var originalTagName1 = RandomHelper.String();
        var originalTagId1 = await TagHelper.CreateAsync(db, user, originalTagName1);
        var originalTagName2 = RandomHelper.String();
        var originalTagId2 = await TagHelper.CreateAsync(db, user, originalTagName2);
        var language = await CardLanguageHelper.CreateAsync(db);
        var card = await CardHelper.CreateAsync(db, user.Id, originalVersionDate, tagIds: new[] { originalTagId1, originalTagId2 }, language: language, versionDescription: originalVersionDescription);
        var newVersionDate = RandomHelper.Date(originalVersionDate);
        var newVersionDescription = RandomHelper.String();
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateCard(dbContext.AsCallContext(), newVersionDate).RunAsync(UpdateCardHelper.RequestForTagChange(card, originalTagId1.AsArray(), versionDescription: newVersionDescription));
        Guid previousVersionId;
        using (var dbContext = new MemCheckDbContext(db))
            previousVersionId = (await dbContext.CardPreviousVersions.Where(previous => previous.Card == card.Id).SingleAsync()).Id;
        using (var dbContext = new MemCheckDbContext(db))
        {
            var diffRequest = new GetCardDiff.Request(user.Id, card.Id, previousVersionId);
            var result = await new GetCardDiff(dbContext.AsCallContext()).RunAsync(diffRequest);
            Assert.AreEqual(user.UserName, result.CurrentVersionCreator);
            Assert.AreEqual(user.UserName, result.OriginalVersionCreator);
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
        }
    }
    [TestMethod()]
    public async Task VisibilityDiff()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var originalVersionDate = RandomHelper.Date();
        var originalVersionDescription = RandomHelper.String();
        var language = await CardLanguageHelper.CreateAsync(db);
        var card = await CardHelper.CreateAsync(db, user.Id, originalVersionDate, userWithViewIds: user.Id.AsArray(), language: language, versionDescription: originalVersionDescription);
        var newVersionDate = RandomHelper.Date(originalVersionDate);
        var newVersionDescription = RandomHelper.String();
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateCard(dbContext.AsCallContext(), newVersionDate).RunAsync(UpdateCardHelper.RequestForVisibilityChange(card, Array.Empty<Guid>(), versionDescription: newVersionDescription));
        Guid previousVersionId;
        using (var dbContext = new MemCheckDbContext(db))
            previousVersionId = (await dbContext.CardPreviousVersions.Where(previous => previous.Card == card.Id).SingleAsync()).Id;
        using (var dbContext = new MemCheckDbContext(db))
        {
            var diffRequest = new GetCardDiff.Request(user.Id, card.Id, previousVersionId);
            var result = await new GetCardDiff(dbContext.AsCallContext()).RunAsync(diffRequest);
            Assert.AreEqual(user.UserName, result.CurrentVersionCreator);
            Assert.AreEqual(user.UserName, result.OriginalVersionCreator);
            Assert.AreEqual(newVersionDate, result.CurrentVersionUtcDate);
            Assert.AreEqual(originalVersionDate, result.OriginalVersionUtcDate);
            Assert.AreEqual(newVersionDescription, result.CurrentVersionDescription);
            Assert.AreEqual(originalVersionDescription, result.OriginalVersionDescription);
            Assert.AreEqual(new("", user.GetUserName()), result.UsersWithView);
            Assert.IsNull(result.FrontSide);
            Assert.IsNull(result.BackSide);
            Assert.IsNull(result.Language);
            Assert.IsNull(result.AdditionalInfo);
            Assert.IsNull(result.Tags);
        }
    }
    [TestMethod()]
    public async Task MultipleDiffs()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var originalVersionDate = RandomHelper.Date();
        var originalVersionDescription = RandomHelper.String();
        var language = await CardLanguageHelper.CreateAsync(db);
        var originalVersionImageName = RandomHelper.String();
        var originalVersionImage = await ImageHelper.CreateAsync(db, user.Id, originalVersionImageName);
        var originalFrontSide = RandomHelper.String();
        var originalBackSide = RandomHelper.String();
        var originalReferences = RandomHelper.String();
        var originalTagName1 = RandomHelper.String();
        var originalTagId1 = await TagHelper.CreateAsync(db, user, originalTagName1);
        var originalTagName2 = RandomHelper.String();
        var originalTagId2 = await TagHelper.CreateAsync(db, user, originalTagName2);
        var card = await CardHelper.CreateAsync(db, user.Id, originalVersionDate, language: language, frontSide: originalFrontSide, backSide: originalBackSide, references: originalReferences, tagIds: new[] { originalTagId1, originalTagId2 }, versionDescription: originalVersionDescription);

        var newVersionDescription = RandomHelper.String();
        var newVersionDate = RandomHelper.Date(originalVersionDate);
        var newFrontSide = RandomHelper.String();
        var newBackSide = RandomHelper.String();
        var newReferences = RandomHelper.String();
        var newVersionImageName = RandomHelper.String();
        var newVersionImage = await ImageHelper.CreateAsync(db, user.Id, newVersionImageName);
        using (var dbContextForUpdate = new MemCheckDbContext(db))
        {
            var request = UpdateCardHelper.RequestForFrontSideChange(card, newFrontSide, versionDescription: newVersionDescription)
                with
            { BackSide = newBackSide }
                with
            { Tags = originalTagId1.AsArray() }
                with
            { References = newReferences };
            await new UpdateCard(dbContextForUpdate.AsCallContext(), newVersionDate).RunAsync(request);
        }

        using var dbContext = new MemCheckDbContext(db);
        var previousVersionId = (await dbContext.CardPreviousVersions.Where(previous => previous.Card == card.Id).SingleAsync()).Id;

        var diffRequest = new GetCardDiff.Request(user.Id, card.Id, previousVersionId);
        var result = await new GetCardDiff(dbContext.AsCallContext()).RunAsync(diffRequest);
        Assert.AreEqual(user.UserName, result.CurrentVersionCreator);
        Assert.AreEqual(user.UserName, result.OriginalVersionCreator);
        Assert.AreEqual(newVersionDate, result.CurrentVersionUtcDate);
        Assert.AreEqual(originalVersionDate, result.OriginalVersionUtcDate);
        Assert.AreEqual(newVersionDescription, result.CurrentVersionDescription);
        Assert.AreEqual(originalVersionDescription, result.OriginalVersionDescription);
        Assert.AreEqual(new(newFrontSide, originalFrontSide), result.FrontSide);
        Assert.AreEqual(new(newBackSide, originalBackSide), result.BackSide);
        Assert.AreEqual(new(newReferences, originalReferences), result.References);
        var expectedTagDiffString = string.Join(",", new[] { originalTagName1, originalTagName2 }.OrderBy(s => s));
        Assert.AreEqual(new(originalTagName1, expectedTagDiffString), result.Tags);
        Assert.IsNull(result.Language);
        Assert.IsNull(result.AdditionalInfo);
        Assert.IsNull(result.UsersWithView);
    }
    [TestMethod()]
    public async Task MultipleVersions()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var initialVersionDate = RandomHelper.Date();
        var intialVersionDescription = RandomHelper.String();
        var language = await CardLanguageHelper.CreateAsync(db);
        var initialFrontSide = RandomHelper.String();
        var initialReferences = RandomHelper.String();
        var card = await CardHelper.CreateAsync(db, userId, initialVersionDate, language: language, frontSide: initialFrontSide, references: initialReferences, versionDescription: intialVersionDescription);

        var intermediaryVersionDescription = RandomHelper.String();
        var intermediaryVersionDate = RandomHelper.Date(initialVersionDate);
        var intermediaryFrontSide = RandomHelper.String();
        var intermediaryReferences = RandomHelper.String();
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateCard(dbContext.AsCallContext(), intermediaryVersionDate).RunAsync(UpdateCardHelper.RequestForFrontSideChange(card, intermediaryFrontSide, versionDescription: intermediaryVersionDescription) with { References = intermediaryReferences });

        Guid initialVersionId;
        using (var dbContext = new MemCheckDbContext(db))
            initialVersionId = (await dbContext.CardPreviousVersions.SingleAsync()).Id;

        var currentVersionDescription = RandomHelper.String();
        var currentVersionDate = RandomHelper.Date(initialVersionDate);
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateCard(dbContext.AsCallContext(), currentVersionDate).RunAsync(UpdateCardHelper.RequestForFrontSideChange(card, initialFrontSide, versionDescription: currentVersionDescription) with { References = initialReferences });

        Guid intermediaryVersionId;
        using (var dbContext = new MemCheckDbContext(db))
            intermediaryVersionId = (await dbContext.CardPreviousVersions.SingleAsync(c => c.Id != initialVersionId)).Id;

        using (var dbContext = new MemCheckDbContext(db))
        {
            var diffRequest = new GetCardDiff.Request(userId, card.Id, intermediaryVersionId);
            var result = await new GetCardDiff(dbContext.AsCallContext()).RunAsync(diffRequest);
            Assert.AreEqual(currentVersionDate, result.CurrentVersionUtcDate);
            Assert.AreEqual(intermediaryVersionDate, result.OriginalVersionUtcDate);
            Assert.AreEqual(currentVersionDescription, result.CurrentVersionDescription);
            Assert.AreEqual(intermediaryVersionDescription, result.OriginalVersionDescription);
            Assert.AreEqual(new(initialFrontSide, intermediaryFrontSide), result.FrontSide);
            Assert.AreEqual(new(initialReferences, intermediaryReferences), result.References);
            Assert.IsNull(result.Language);
            Assert.IsNull(result.BackSide);
            Assert.IsNull(result.Tags);
            Assert.IsNull(result.AdditionalInfo);
            Assert.IsNull(result.UsersWithView);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var diffRequest = new GetCardDiff.Request(userId, card.Id, initialVersionId);
            var result = await new GetCardDiff(dbContext.AsCallContext()).RunAsync(diffRequest);
            Assert.AreEqual(currentVersionDate, result.CurrentVersionUtcDate);
            Assert.AreEqual(initialVersionDate, result.OriginalVersionUtcDate);
            Assert.AreEqual(currentVersionDescription, result.CurrentVersionDescription);
            Assert.AreEqual(intialVersionDescription, result.OriginalVersionDescription);
            Assert.IsNull(result.Language);
            Assert.IsNull(result.FrontSide);
            Assert.IsNull(result.BackSide);
            Assert.IsNull(result.References);
            Assert.IsNull(result.Tags);
            Assert.IsNull(result.AdditionalInfo);
            Assert.IsNull(result.UsersWithView);
        }
    }
}

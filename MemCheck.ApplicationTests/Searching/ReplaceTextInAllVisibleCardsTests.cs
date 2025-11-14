using MemCheck.Application.Helpers;
using MemCheck.Application.QueryValidation;
using MemCheck.Basics;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Searching;

[TestClass()]
public class ReplaceTextInAllVisibleCardsTests
{
    [TestMethod()]
    public async Task UserNotLoggedIn()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId);

        using var dbContext = new MemCheckDbContext(db);
        var request = new ReplaceTextInAllVisibleCards.Request(Guid.Empty, RandomHelper.String(), RandomHelper.String(), RandomHelper.String());
        var exception = await Assert.ThrowsExactlyAsync<NonexistentUserException>(async () => await new ReplaceTextInAllVisibleCards(dbContext.AsCallContext()).RunAsync(request));
        Assert.AreEqual(QueryValidationHelper.ExceptionMesg_UserDoesNotExist, exception.Message);
    }
    [TestMethod()]
    public async Task UserDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId);

        using var dbContext = new MemCheckDbContext(db);
        var request = new ReplaceTextInAllVisibleCards.Request(Guid.NewGuid(), RandomHelper.String(), RandomHelper.String(), RandomHelper.String());
        var exception = await Assert.ThrowsExactlyAsync<NonexistentUserException>(async () => await new ReplaceTextInAllVisibleCards(dbContext.AsCallContext()).RunAsync(request));
        Assert.AreEqual(QueryValidationHelper.ExceptionMesg_UserDoesNotExist, exception.Message);
    }
    [TestMethod()]
    public async Task TextToReplaceTooShort()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId);

        using var dbContext = new MemCheckDbContext(db);
        var request = new ReplaceTextInAllVisibleCards.Request(userId, RandomHelper.String(ReplaceTextInAllVisibleCards.Request.MinTextToReplaceLength - 1), RandomHelper.String(), RandomHelper.String());
        var exception = await Assert.ThrowsExactlyAsync<RequestInputException>(async () => await new ReplaceTextInAllVisibleCards(dbContext.AsCallContext()).RunAsync(request));
        StringAssert.StartsWith(exception.Message, ReplaceTextInAllVisibleCards.Request.ExceptionMesgPrefix_TextToReplaceTooShort);
    }
    [TestMethod()]
    public async Task VersionDescriptionTooShort()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId);

        using var dbContext = new MemCheckDbContext(db);
        var request = new ReplaceTextInAllVisibleCards.Request(userId, RandomHelper.String(), RandomHelper.String(), RandomHelper.String(CardInputValidator.MinVersionDescriptionLength - 1));
        var exception = await Assert.ThrowsExactlyAsync<RequestInputException>(async () => await new ReplaceTextInAllVisibleCards(dbContext.AsCallContext()).RunAsync(request));
        StringAssert.StartsWith(exception.Message, ReplaceTextInAllVisibleCards.Request.ExceptionMesgPrefix_InvalidVersionDescriptionLength);
    }
    [TestMethod()]
    public async Task VersionDescriptionTooLong()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId);

        using var dbContext = new MemCheckDbContext(db);
        var request = new ReplaceTextInAllVisibleCards.Request(userId, RandomHelper.String(), RandomHelper.String(), RandomHelper.String(CardInputValidator.MaxVersionDescriptionLength + 1));
        var exception = await Assert.ThrowsExactlyAsync<RequestInputException>(async () => await new ReplaceTextInAllVisibleCards(dbContext.AsCallContext()).RunAsync(request));
        StringAssert.StartsWith(exception.Message, ReplaceTextInAllVisibleCards.Request.ExceptionMesgPrefix_InvalidVersionDescriptionLength);
    }
    [TestMethod()]
    public async Task VersionDescriptionNotTrimmedAtStart()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId);

        using var dbContext = new MemCheckDbContext(db);
        var request = new ReplaceTextInAllVisibleCards.Request(userId, RandomHelper.String(), RandomHelper.String(), " " + RandomHelper.String());
        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await new ReplaceTextInAllVisibleCards(dbContext.AsCallContext()).RunAsync(request));
        StringAssert.StartsWith(exception.Message, CardInputValidator.ExceptionMesg_VersionDescriptionNotTrimmed);
    }
    [TestMethod()]
    public async Task VersionDescriptionNotTrimmedAtEnd()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId);

        using var dbContext = new MemCheckDbContext(db);
        var request = new ReplaceTextInAllVisibleCards.Request(userId, RandomHelper.String(), RandomHelper.String(), RandomHelper.String() + '\t');
        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await new ReplaceTextInAllVisibleCards(dbContext.AsCallContext()).RunAsync(request));
        StringAssert.StartsWith(exception.Message, CardInputValidator.ExceptionMesg_VersionDescriptionNotTrimmed);
    }
    [TestMethod()]
    public async Task UserHasNoVisibilityOnTheOnlyCard()
    {
        var db = DbHelper.GetEmptyTestDB();
        var cardOwner = await UserHelper.CreateUserInDbAsync(db);
        var userWithCardInDeckId = await UserHelper.CreateInDbAsync(db);
        var tagId = await TagHelper.CreateAsync(db, cardOwner);
        var textToReplace = RandomHelper.String();
        var card = await CardHelper.CreateAsync(
            db,
            cardOwner.Id,
            userWithViewIds: new[] { cardOwner.Id, userWithCardInDeckId },
            frontSide: textToReplace + RandomHelper.String(),
            backSide: RandomHelper.String() + textToReplace,
            additionalInfo: RandomHelper.String() + textToReplace + RandomHelper.String(),
            references: RandomHelper.String() + textToReplace,
            tagIds: tagId.AsArray(),
            versionDate: RandomHelper.Date());

        var deckId = await DeckHelper.CreateAsync(db, userWithCardInDeckId);
        await DeckHelper.AddCardAsync(db, deckId, card.Id);

        card = await CardHelper.GetCardFromDbWithAllfieldsAsync(db, card.Id);

        var modifyingUserId = await UserHelper.CreateInDbAsync(db);

        using (var dbContext = new MemCheckDbContext(db))
        {
            var request = new ReplaceTextInAllVisibleCards.Request(modifyingUserId, textToReplace, RandomHelper.String(), RandomHelper.String());
            await new ReplaceTextInAllVisibleCards(dbContext.AsCallContext()).RunAsync(request);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            Assert.IsFalse(dbContext.CardPreviousVersions.Any());
            var cardFromDb = await CardHelper.GetCardFromDbWithAllfieldsAsync(db, card.Id);
            CardComparisonHelper.AssertSameContent(card, cardFromDb);
        }
    }
    [TestMethod()]
    public async Task UserIsOwnerOfTheOnlyCard_CheckAllDetails()
    {
        var db = DbHelper.GetEmptyTestDB();
        var cardOwner = await UserHelper.CreateUserInDbAsync(db);
        var userWithCardInDeckId = await UserHelper.CreateInDbAsync(db);
        var tagId = await TagHelper.CreateAsync(db, cardOwner);
        var textToReplace = RandomHelper.String();
        var frontSideSecondPart = RandomHelper.String();
        var backSideFirstPart = RandomHelper.String();
        var additionalInfoFirstPart = RandomHelper.String();
        var additionalInfoSecondPart = RandomHelper.String();
        var referencesFirstPart = RandomHelper.String();
        var initialVersionDescription = RandomHelper.String();
        var initialVersionDate = RandomHelper.Date();
        var card = await CardHelper.CreateAsync(
            db,
            cardOwner.Id,
            userWithViewIds: new[] { cardOwner.Id, userWithCardInDeckId },
            frontSide: textToReplace + frontSideSecondPart,
            backSide: backSideFirstPart + textToReplace,
            additionalInfo: additionalInfoFirstPart + textToReplace + additionalInfoSecondPart,
            references: referencesFirstPart + textToReplace,
            tagIds: tagId.AsArray(),
            versionDate: initialVersionDate,
            versionDescription: initialVersionDescription);

        var deckId = await DeckHelper.CreateAsync(db, userWithCardInDeckId);
        await DeckHelper.AddCardAsync(db, deckId, card.Id);

        card = await CardHelper.GetCardFromDbWithAllfieldsAsync(db, card.Id);
        var replacementText = RandomHelper.String();
        var newVersionDescription = RandomHelper.String();
        var newVersionDate = RandomHelper.Date(initialVersionDate);

        using (var dbContext = new MemCheckDbContext(db))
        {
            var request = new ReplaceTextInAllVisibleCards.Request(cardOwner.Id, textToReplace, replacementText, newVersionDescription);
            await new ReplaceTextInAllVisibleCards(dbContext.AsCallContext(), newVersionDate).RunAsync(request);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            Assert.IsTrue(dbContext.CardPreviousVersions.Any());
            var previousVersion = await dbContext.CardPreviousVersions.SingleAsync();
            Assert.AreEqual(card.Id, previousVersion.Card);
            Assert.AreEqual(initialVersionDescription, previousVersion.VersionDescription);
            Assert.AreEqual(initialVersionDate, previousVersion.VersionUtcDate);

            var cardFromDb = await CardHelper.GetCardFromDbWithAllfieldsAsync(db, card.Id);
            Assert.AreEqual(newVersionDescription, cardFromDb.VersionDescription);
            Assert.AreEqual(newVersionDate, cardFromDb.VersionUtcDate);
            Assert.AreEqual(CardVersionType.Changes, cardFromDb.VersionType);
            Assert.IsNotNull(cardFromDb.PreviousVersion);
            Assert.AreEqual(previousVersion.Id, cardFromDb.PreviousVersion.Id);
            Assert.AreEqual(replacementText + frontSideSecondPart, cardFromDb.FrontSide);
            Assert.AreEqual(backSideFirstPart + replacementText, cardFromDb.BackSide);
            Assert.AreEqual(additionalInfoFirstPart + replacementText + additionalInfoSecondPart, cardFromDb.AdditionalInfo);
            Assert.AreEqual(referencesFirstPart + replacementText, cardFromDb.References);
            CardComparisonHelper.AssertSameContent(card, cardFromDb, frontSide: false, backSide: false, additionalInfo: false, references: false, versionDate: false, versionDescription: false, versionType: false, previousVersion: false);
        }
    }
    [TestMethod()]
    public async Task UserNotOwnerAndOnlyCardPublic()
    {
        var db = DbHelper.GetEmptyTestDB();
        var cardOwnerId = await UserHelper.CreateInDbAsync(db);

        var card = await CardHelper.CreateAsync(db, cardOwnerId, additionalInfo: "[Wikipédia : Barbotin](https://fr.m.wikipedia.org/wiki/Barbotin)");
        card = await CardHelper.GetCardFromDbWithAllfieldsAsync(db, card.Id);

        var modifyingUserId = await UserHelper.CreateInDbAsync(db);
        var textToReplace = "https://fr.m.wikipedia.org/wiki/";
        var replacementText = "https://fr.wikipedia.org/wiki/";

        using (var dbContext = new MemCheckDbContext(db))
        {
            var request = new ReplaceTextInAllVisibleCards.Request(modifyingUserId, textToReplace, replacementText, RandomHelper.String());
            await new ReplaceTextInAllVisibleCards(dbContext.AsCallContext()).RunAsync(request);
        }

        var cardFromDb = await CardHelper.GetCardFromDbWithAllfieldsAsync(db, card.Id);
        Assert.AreEqual(modifyingUserId, cardFromDb.VersionCreator.Id);
        Assert.AreEqual("[Wikipédia : Barbotin](https://fr.wikipedia.org/wiki/Barbotin)", cardFromDb.AdditionalInfo);
        CardComparisonHelper.AssertSameContent(card, cardFromDb, versionCreator: false, additionalInfo: false, versionDate: false, versionDescription: false, versionType: false, previousVersion: false);
    }
    [TestMethod()]
    public async Task UserNotOwnerAndAllowedOnOnlyCard()
    {
        var db = DbHelper.GetEmptyTestDB();
        var cardOwnerId = await UserHelper.CreateInDbAsync(db);
        var modifyingUserId = await UserHelper.CreateInDbAsync(db);
        var textToReplace = RandomHelper.String();
        var referencesFirstPart = RandomHelper.String();
        var referencesSecondPart = RandomHelper.String();
        var card = await CardHelper.CreateAsync(
            db,
            cardOwnerId,
            userWithViewIds: new[] { cardOwnerId, modifyingUserId },
            references: referencesFirstPart + textToReplace + referencesSecondPart);

        card = await CardHelper.GetCardFromDbWithAllfieldsAsync(db, card.Id);
        var replacementText = RandomHelper.String();

        using (var dbContext = new MemCheckDbContext(db))
        {
            var request = new ReplaceTextInAllVisibleCards.Request(modifyingUserId, textToReplace, replacementText, RandomHelper.String());
            await new ReplaceTextInAllVisibleCards(dbContext.AsCallContext()).RunAsync(request);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            Assert.IsTrue(dbContext.CardPreviousVersions.Any());
            var previousVersion = await dbContext.CardPreviousVersions.SingleAsync();
            Assert.AreEqual(card.Id, previousVersion.Card);
        }
        var cardFromDb = await CardHelper.GetCardFromDbWithAllfieldsAsync(db, card.Id);
        Assert.AreEqual(modifyingUserId, cardFromDb.VersionCreator.Id);
        Assert.AreEqual(referencesFirstPart + replacementText + referencesSecondPart, cardFromDb.References);
        CardComparisonHelper.AssertSameContent(card, cardFromDb, versionCreator: false, references: false, versionDate: false, versionDescription: false, versionType: false, previousVersion: false);
    }
    [TestMethod()]
    public async Task ReplaceWithEmptyString()
    {
        var db = DbHelper.GetEmptyTestDB();
        var cardOwnerId = await UserHelper.CreateInDbAsync(db);
        var textToReplace = RandomHelper.String();
        var frontSideFirstPart = RandomHelper.String();
        var frontSideInfoSecondPart = RandomHelper.String();
        var card = await CardHelper.CreateAsync(
            db,
            cardOwnerId,
            frontSide: frontSideFirstPart + textToReplace + frontSideInfoSecondPart);

        var modifyingUserId = await UserHelper.CreateInDbAsync(db);

        card = await CardHelper.GetCardFromDbWithAllfieldsAsync(db, card.Id);

        using (var dbContext = new MemCheckDbContext(db))
        {
            var request = new ReplaceTextInAllVisibleCards.Request(modifyingUserId, textToReplace, "", RandomHelper.String());
            await new ReplaceTextInAllVisibleCards(dbContext.AsCallContext()).RunAsync(request);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            Assert.IsTrue(dbContext.CardPreviousVersions.Any());
            var previousVersion = await dbContext.CardPreviousVersions.SingleAsync();
            Assert.AreEqual(card.Id, previousVersion.Card);
        }
        var cardFromDb = await CardHelper.GetCardFromDbWithAllfieldsAsync(db, card.Id);
        Assert.AreEqual(modifyingUserId, cardFromDb.VersionCreator.Id);
        Assert.AreEqual(frontSideFirstPart + frontSideInfoSecondPart, cardFromDb.FrontSide);
        CardComparisonHelper.AssertSameContent(card, cardFromDb, versionCreator: false, frontSide: false, versionDate: false, versionDescription: false, versionType: false, previousVersion: false);
    }
    [TestMethod()]
    public async Task ComplexCase()
    {
        //card1 is public, but does not contain the text to replace [Must not change]
        //card2 is public, and contains the text to replace [Must change]
        //card3 has restricted access with modifyingUser allowed, but does not contain the text to replace [Must not change]
        //card4 has restricted access with modifyingUser allowed, and contains the text to replace [Must change]
        //card5 is not allowed for modifyingUser and does not contain the text to replace [Must not change]
        //card6 is not allowed for modifyingUser and contains the text to replace [Must not change]

        var db = DbHelper.GetEmptyTestDB();

        var cardOwnerId = await UserHelper.CreateInDbAsync(db);

        var textToReplace = RandomHelper.String();

        //Create card1
        var card1 = await CardHelper.CreateAsync(db, cardOwnerId);
        card1 = await CardHelper.GetCardFromDbWithAllfieldsAsync(db, card1.Id);

        //Create card2
        var card2FrontSideSecondPart = RandomHelper.String();
        var card2 = await CardHelper.CreateAsync(db, cardOwnerId, frontSide: textToReplace + card2FrontSideSecondPart);
        card2 = await CardHelper.GetCardFromDbWithAllfieldsAsync(db, card2.Id);

        //Create card5
        var card5 = await CardHelper.CreateAsync(db, cardOwnerId, userWithViewIds: cardOwnerId.AsArray());
        card5 = await CardHelper.GetCardFromDbWithAllfieldsAsync(db, card5.Id);

        //Create card6
        var card6 = await CardHelper.CreateAsync(db, cardOwnerId, userWithViewIds: cardOwnerId.AsArray(), additionalInfo: RandomHelper.String() + textToReplace + RandomHelper.String());
        card6 = await CardHelper.GetCardFromDbWithAllfieldsAsync(db, card6.Id);

        var modifyingUser = await UserHelper.CreateInDbAsync(db);

        //Create card3
        var card3 = await CardHelper.CreateAsync(db, cardOwnerId, userWithViewIds: new[] { cardOwnerId, modifyingUser });
        card3 = await CardHelper.GetCardFromDbWithAllfieldsAsync(db, card3.Id);

        //Create card4
        var card4BackSideFirstPart = RandomHelper.String();
        var card4 = await CardHelper.CreateAsync(db, cardOwnerId, userWithViewIds: new[] { cardOwnerId, modifyingUser }, backSide: card4BackSideFirstPart + textToReplace);
        card4 = await CardHelper.GetCardFromDbWithAllfieldsAsync(db, card4.Id);

        var replacementText = RandomHelper.String();

        using (var dbContext = new MemCheckDbContext(db))
        {
            var request = new ReplaceTextInAllVisibleCards.Request(modifyingUser, textToReplace, replacementText, RandomHelper.String());
            await new ReplaceTextInAllVisibleCards(dbContext.AsCallContext()).RunAsync(request);
        }

        using (var dbContext = new MemCheckDbContext(db))
            Assert.AreEqual(2, dbContext.CardPreviousVersions.Count());

        //The curly braces below are meant to reduce the visibility of local variables and prevent errors

        {
            var card1FromDb = await CardHelper.GetCardFromDbWithAllfieldsAsync(db, card1.Id);
            CardComparisonHelper.AssertSameContent(card1, card1FromDb);
        }

        {
            var card2FromDb = await CardHelper.GetCardFromDbWithAllfieldsAsync(db, card2.Id);
            Assert.AreEqual(replacementText + card2FrontSideSecondPart, card2FromDb.FrontSide);
            Assert.AreEqual(modifyingUser, card2FromDb.VersionCreator.Id);
            CardComparisonHelper.AssertSameContent(card2, card2FromDb, frontSide: false, versionCreator: false, versionDate: false, versionDescription: false, versionType: false, previousVersion: false);
        }

        {
            var card3FromDb = await CardHelper.GetCardFromDbWithAllfieldsAsync(db, card3.Id);
            CardComparisonHelper.AssertSameContent(card3, card3FromDb);
        }

        {
            var card4FromDb = await CardHelper.GetCardFromDbWithAllfieldsAsync(db, card4.Id);
            Assert.AreEqual(card4BackSideFirstPart + replacementText, card4FromDb.BackSide);
            Assert.AreEqual(modifyingUser, card4FromDb.VersionCreator.Id);
            CardComparisonHelper.AssertSameContent(card4, card4FromDb, backSide: false, versionCreator: false, versionDate: false, versionDescription: false, versionType: false, previousVersion: false);
        }

        {
            var card5FromDb = await CardHelper.GetCardFromDbWithAllfieldsAsync(db, card5.Id);
            CardComparisonHelper.AssertSameContent(card5, card5FromDb);
        }

        {
            var card6FromDb = await CardHelper.GetCardFromDbWithAllfieldsAsync(db, card6.Id);
            CardComparisonHelper.AssertSameContent(card6, card6FromDb);
        }
    }
}

using MemCheck.Application.Helpers;
using MemCheck.Application.QueryValidation;
using MemCheck.Application.Ratings;
using MemCheck.Basics;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards;

[TestClass()]
public class UpdateCardTests
{
    [TestMethod()]
    public async Task UserNotLoggedIn()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var languageId = await CardLanguageHelper.CreateAsync(db);
        var card = await CardHelper.CreateAsync(db, user, language: languageId);

        using var dbContext = new MemCheckDbContext(db);
        var request = UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String(), Guid.Empty);
        await Assert.ThrowsExactlyAsync<NonexistentUserException>(async () => await new UpdateCard(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task UserDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var languageId = await CardLanguageHelper.CreateAsync(db);
        var frontSide = RandomHelper.String();
        var card = await CardHelper.CreateAsync(db, user, language: languageId, frontSide: frontSide);

        using var dbContext = new MemCheckDbContext(db);
        var r = UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String(), Guid.NewGuid());
        await Assert.ThrowsExactlyAsync<NonexistentUserException>(async () => await new UpdateCard(dbContext.AsCallContext()).RunAsync(r));
        await CardHelper.AssertCardHasFrontSide(db, card.Id, frontSide);
    }
    [TestMethod()]
    public async Task CardDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        var r = new UpdateCard.Request(Guid.NewGuid(), user, RandomHelper.String(), RandomHelper.String(), RandomHelper.String(), RandomHelper.String(), Guid.NewGuid(), Array.Empty<Guid>(), Array.Empty<Guid>(), RandomHelper.String());
        await Assert.ThrowsExactlyAsync<ArgumentException>(async () => await new UpdateCard(dbContext.AsCallContext()).RunAsync(r));
    }
    [TestMethod()]
    public async Task UserNotAllowedToViewCard()
    {
        var db = DbHelper.GetEmptyTestDB();
        var cardCreator = await UserHelper.CreateInDbAsync(db);
        var languageId = await CardLanguageHelper.CreateAsync(db);
        var frontSide = RandomHelper.String();
        var card = await CardHelper.CreateAsync(db, cardCreator, language: languageId, userWithViewIds: cardCreator.AsArray(), frontSide: frontSide);
        var otherUser = await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        var r = UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String(), otherUser);
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await new UpdateCard(dbContext.AsCallContext()).RunAsync(r));
        await CardHelper.AssertCardHasFrontSide(db, card.Id, frontSide);
    }
    [TestMethod()]
    public async Task PublicCard()
    {
        var db = DbHelper.GetEmptyTestDB();
        var cardCreator = await UserHelper.CreateInDbAsync(db);
        var languageId = await CardLanguageHelper.CreateAsync(db);
        var card = await CardHelper.CreateAsync(db, cardCreator, language: languageId, userWithViewIds: Array.Empty<Guid>());
        var otherUser = await UserHelper.CreateInDbAsync(db);
        var newFrontSide = RandomHelper.String();

        using (var dbContext = new MemCheckDbContext(db))
        {
            var r = UpdateCardHelper.RequestForFrontSideChange(card, newFrontSide, otherUser);
            await new UpdateCard(dbContext.AsCallContext()).RunAsync(r);
        }

        await CardHelper.AssertCardHasFrontSide(db, card.Id, newFrontSide);
    }
    [TestMethod()]
    public async Task UserNotInNewVisibilityList()
    {
        var db = DbHelper.GetEmptyTestDB();
        var cardCreator = await UserHelper.CreateInDbAsync(db);
        var languageId = await CardLanguageHelper.CreateAsync(db);
        var card = await CardHelper.CreateAsync(db, cardCreator, language: languageId, userWithViewIds: Array.Empty<Guid>());
        var newVersionCreator = await UserHelper.CreateInDbAsync(db);

        var r = UpdateCardHelper.RequestForVisibilityChange(card, cardCreator.AsArray()) with { VersionCreatorId = newVersionCreator };

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await new UpdateCard(dbContext.AsCallContext()).RunAsync(r));
    }
    [TestMethod()]
    public async Task DescriptionTooShort()
    {
        var db = DbHelper.GetEmptyTestDB();
        var cardCreator = await UserHelper.CreateInDbAsync(db);
        var languageId = await CardLanguageHelper.CreateAsync(db);
        var card = await CardHelper.CreateAsync(db, cardCreator, language: languageId, userWithViewIds: Array.Empty<Guid>());
        var request = UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String(), versionDescription: RandomHelper.String(CardInputValidator.MinVersionDescriptionLength - 1));
        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExactlyAsync<RequestInputException>(async () => await new UpdateCard(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task DescriptionTooLong()
    {
        var db = DbHelper.GetEmptyTestDB();
        var cardCreator = await UserHelper.CreateInDbAsync(db);
        var languageId = await CardLanguageHelper.CreateAsync(db);
        var card = await CardHelper.CreateAsync(db, cardCreator, language: languageId, userWithViewIds: Array.Empty<Guid>());
        var request = UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String(), versionDescription: RandomHelper.String(CardInputValidator.MaxVersionDescriptionLength + 1));
        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExactlyAsync<RequestInputException>(async () => await new UpdateCard(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task UserInVisibilityList()
    {
        var db = DbHelper.GetEmptyTestDB();
        var cardCreator = await UserHelper.CreateInDbAsync(db);
        var languageId = await CardLanguageHelper.CreateAsync(db);
        var otherUser = await UserHelper.CreateInDbAsync(db);
        var card = await CardHelper.CreateAsync(db, cardCreator, language: languageId, userWithViewIds: new[] { cardCreator, otherUser });
        var newFrontSide = RandomHelper.String();

        using (var dbContext = new MemCheckDbContext(db))
        {
            var r = UpdateCardHelper.RequestForFrontSideChange(card, newFrontSide, otherUser);
            await new UpdateCard(dbContext.AsCallContext()).RunAsync(r);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var cardFromDb = dbContext.Cards.Single(c => c.Id == card.Id);
            Assert.AreEqual(newFrontSide, cardFromDb.FrontSide);
        }
    }
    [TestMethod()]
    public async Task UpdateAllFields()
    {
        var db = DbHelper.GetEmptyTestDB();
        var cardCreator = await UserHelper.CreateUserInDbAsync(db);
        var languageId = await CardLanguageHelper.CreateAsync(db);
        var originalCard = await CardHelper.CreateAsync(db, cardCreator, language: languageId, usersWithView: Array.Empty<MemCheckUser>());

        var newVersionCreator = await UserHelper.CreateInDbAsync(db);
        var frontSide = RandomHelper.String();
        var backSide = RandomHelper.String();
        var additionalInfo = RandomHelper.String();
        var references = RandomHelper.String();
        var versionDescription = RandomHelper.String();
        var newLanguageId = await CardLanguageHelper.CreateAsync(db);
        var imageOnFrontSideId = await ImageHelper.CreateAsync(db, cardCreator.Id);
        var imageOnBackSide1Id = await ImageHelper.CreateAsync(db, cardCreator.Id);
        var imageOnBackSide2Id = await ImageHelper.CreateAsync(db, cardCreator.Id);
        var imageOnAdditionalInfoId = await ImageHelper.CreateAsync(db, cardCreator.Id);
        var tagId = await TagHelper.CreateAsync(db, cardCreator);

        using (var dbContext = new MemCheckDbContext(db))
        {
            var request = new UpdateCard.Request(
                originalCard.Id,
                newVersionCreator,
                frontSide,
                backSide,
                additionalInfo,
                references,
                languageId,
                new Guid[] { tagId },
                new Guid[] { cardCreator.Id, newVersionCreator },
                versionDescription);
            await new UpdateCard(dbContext.AsCallContext()).RunAsync(request);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var updatedCard = dbContext.Cards
                .Include(c => c.VersionCreator)
                .Include(c => c.CardLanguage)
                .Include(c => c.UsersWithView)
                .Include(c => c.TagsInCards)
                .Single(c => c.Id == originalCard.Id);
            Assert.AreEqual(newVersionCreator, updatedCard.VersionCreator.Id);
            Assert.AreEqual(frontSide, updatedCard.FrontSide);
            Assert.AreEqual(backSide, updatedCard.BackSide);
            Assert.AreEqual(additionalInfo, updatedCard.AdditionalInfo);
            Assert.AreEqual(references, updatedCard.References);
            Assert.AreEqual(versionDescription, updatedCard.VersionDescription);
            Assert.AreEqual(languageId, updatedCard.CardLanguage.Id);
            Assert.IsTrue(updatedCard.TagsInCards.Any(t => t.TagId == tagId));
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            CardVisibilityHelper.CheckUserIsAllowedToViewCard(dbContext, cardCreator.Id, originalCard.Id);
            CardVisibilityHelper.CheckUserIsAllowedToViewCard(dbContext, newVersionCreator, originalCard.Id);
        }
    }
    [TestMethod()]
    public async Task UpdateNothing()
    {
        var db = DbHelper.GetEmptyTestDB();
        var cardCreator = await UserHelper.CreateInDbAsync(db);
        var languageId = await CardLanguageHelper.CreateAsync(db);
        var card = await CardHelper.CreateAsync(db, cardCreator, language: languageId, userWithViewIds: Array.Empty<Guid>());

        using var dbContext = new MemCheckDbContext(db);
        var request = new UpdateCard.Request(
            card.Id,
            card.VersionCreator.Id,
            card.FrontSide,
            card.BackSide,
            card.AdditionalInfo,
            card.References,
            card.CardLanguage.Id,
            card.TagsInCards.Select(t => t.TagId),
            card.UsersWithView.Select(uwv => uwv.UserId),
            RandomHelper.String());
        await Assert.ThrowsExactlyAsync<RequestInputException>(async () => await new UpdateCard(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task UserSubscribingToCardOnEdit()
    {
        var db = DbHelper.GetEmptyTestDB();

        var cardCreator = await UserHelper.CreateInDbAsync(db);
        var languageId = await CardLanguageHelper.CreateAsync(db);
        var card = await CardHelper.CreateAsync(db, cardCreator, language: languageId);

        var newVersionCreator = await UserHelper.CreateInDbAsync(db, subscribeToCardOnEdit: true);

        using (var dbContext = new MemCheckDbContext(db))
        {
            var r = UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String(), newVersionCreator);
            await new UpdateCard(dbContext.AsCallContext()).RunAsync(r);
        }

        Assert.IsTrue(await CardSubscriptionHelper.UserIsSubscribedToCardAsync(db, newVersionCreator, card.Id));
    }
    [TestMethod()]
    public async Task UserNotSubscribingToCardOnEdit()
    {
        var db = DbHelper.GetEmptyTestDB();

        var cardCreator = await UserHelper.CreateInDbAsync(db);
        var languageId = await CardLanguageHelper.CreateAsync(db);
        var card = await CardHelper.CreateAsync(db, cardCreator, language: languageId);

        var newVersionCreator = await UserHelper.CreateInDbAsync(db, subscribeToCardOnEdit: false);

        using (var dbContext = new MemCheckDbContext(db))
        {
            var r = UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String(), newVersionCreator);
            await new UpdateCard(dbContext.AsCallContext()).RunAsync(r);
        }

        Assert.IsFalse(await CardSubscriptionHelper.UserIsSubscribedToCardAsync(db, newVersionCreator, card.Id));
    }
    [TestMethod()]
    public async Task ReduceVisibility_OnlyUserWithView_NoOtherUserHasInDeck_OnlyAuthor()
    {
        var db = DbHelper.GetEmptyTestDB();
        var cardCreator = await UserHelper.CreateInDbAsync(db);
        var languageId = await CardLanguageHelper.CreateAsync(db);
        var card = await CardHelper.CreateAsync(db, cardCreator, language: languageId, userWithViewIds: Array.Empty<Guid>());
        var deck = await DeckHelper.CreateAsync(db, cardCreator);
        await DeckHelper.AddCardAsync(db, deck, card.Id, 0);

        var otherUser = await UserHelper.CreateInDbAsync(db);

        using (var dbContext = new MemCheckDbContext(db))
        {
            CardVisibilityHelper.CheckUserIsAllowedToViewCard(dbContext, cardCreator, card.Id);
            CardVisibilityHelper.CheckUserIsAllowedToViewCard(dbContext, otherUser, card.Id);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var r = UpdateCardHelper.RequestForVisibilityChange(card, cardCreator.AsArray());
            await new UpdateCard(dbContext.AsCallContext()).RunAsync(r);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            CardVisibilityHelper.CheckUserIsAllowedToViewCard(dbContext, cardCreator, card.Id);
            Assert.ThrowsExactly<UserNotAllowedToAccessCardException>(() => CardVisibilityHelper.CheckUserIsAllowedToViewCard(dbContext, otherUser, card.Id));
        }
    }
    [TestMethod()]
    public async Task ReduceVisibility_OtherUserHasView_NoUserHasInDeck_OnlyAuthor()
    {
        var db = DbHelper.GetEmptyTestDB();
        var cardCreatorId = await UserHelper.CreateInDbAsync(db);
        var languageId = await CardLanguageHelper.CreateAsync(db);
        var otherUserId = await UserHelper.CreateInDbAsync(db);

        // Card is created with visibility for the two users
        var card = await CardHelper.CreateAsync(db, cardCreatorId, userWithViewIds: new[] { cardCreatorId, otherUserId });

        // We check the visibility
        TestCardVisibilityHelper.CheckUserIsAllowedToViewCard(db, cardCreatorId, card.Id);
        TestCardVisibilityHelper.CheckUserIsAllowedToViewCard(db, otherUserId, card.Id);

        // We update the card, private to the creator
        using (var dbContext = new MemCheckDbContext(db))
        {
            var r = UpdateCardHelper.RequestForVisibilityChange(card, cardCreatorId.AsArray());
            await new UpdateCard(dbContext.AsCallContext()).RunAsync(r);
        }

        // We check the visibility
        TestCardVisibilityHelper.CheckUserIsAllowedToViewCard(db, cardCreatorId, card.Id);
        Assert.ThrowsExactly<UserNotAllowedToAccessCardException>(() => TestCardVisibilityHelper.CheckUserIsAllowedToViewCard(db, otherUserId, card.Id));

        // We check that the previous version is correct
        var previousVersion = await CardPreviousVersionHelper.GetPreviousVersionAsync(db, card.Id);
        Assert.IsNotNull(previousVersion);
        Assert.IsTrue(CardPreviousVersionVisibilityHelper.CardIsVisibleToUser(cardCreatorId, previousVersion));
        Assert.IsTrue(CardPreviousVersionVisibilityHelper.CardIsVisibleToUser(otherUserId, previousVersion));
    }
    [TestMethod()]
    public async Task ReduceVisibility_OtherUserHasInDeck_OnlyAuthor()
    {
        var db = DbHelper.GetEmptyTestDB();
        var cardCreator = await UserHelper.CreateUserInDbAsync(db);
        var languageId = await CardLanguageHelper.CreateAsync(db);
        var card = await CardHelper.CreateAsync(db, cardCreator.Id, language: languageId, userWithViewIds: Array.Empty<Guid>());

        var otherUser = await UserHelper.CreateUserInDbAsync(db);
        var otherUserDeck = await DeckHelper.CreateAsync(db, otherUser.Id);
        await DeckHelper.AddCardAsync(db, otherUserDeck, card.Id, 0);

        using (var dbContext = new MemCheckDbContext(db))
        {
            var r = UpdateCardHelper.RequestForVisibilityChange(card, cardCreator.Id.AsArray());
            var e = await Assert.ThrowsExactlyAsync<RequestInputException>(async () => await new UpdateCard(dbContext.AsCallContext()).RunAsync(r));
            Assert.Contains(otherUser.GetUserName(), e.Message);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var r = UpdateCardHelper.RequestForVisibilityChange(card, otherUser.Id.AsArray(), otherUser.Id);
            var e = await Assert.ThrowsExactlyAsync<RequestInputException>(async () => await new UpdateCard(dbContext.AsCallContext()).RunAsync(r));
            Assert.IsTrue(e.Message.Contains(cardCreator.GetUserName()));
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var r = UpdateCardHelper.RequestForVisibilityChange(card, new[] { cardCreator.Id, otherUser.Id });
            await new UpdateCard(dbContext.AsCallContext()).RunAsync(r);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            CardVisibilityHelper.CheckUserIsAllowedToViewCard(dbContext, cardCreator.Id, card.Id);
            CardVisibilityHelper.CheckUserIsAllowedToViewCard(dbContext, otherUser.Id, card.Id);
        }
    }
    [TestMethod()]
    public async Task ReduceVisibility_NoUserHasInDeck_OtherAuthor()
    {
        var db = DbHelper.GetEmptyTestDB();
        var cardCreator = await UserHelper.CreateUserInDbAsync(db);
        var languageId = await CardLanguageHelper.CreateAsync(db);
        var card = await CardHelper.CreateAsync(db, cardCreator.Id, language: languageId, userWithViewIds: Array.Empty<Guid>());

        var newVersionCreator = await UserHelper.CreateUserInDbAsync(db);
        using (var dbContext = new MemCheckDbContext(db))
        {
            var r = UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String(), newVersionCreator.Id);
            await new UpdateCard(dbContext.AsCallContext()).RunAsync(r);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var r = UpdateCardHelper.RequestForVisibilityChange(card, cardCreator.Id.AsArray(), cardCreator.Id);
            var e = await Assert.ThrowsExactlyAsync<RequestInputException>(async () => await new UpdateCard(dbContext.AsCallContext()).RunAsync(r));
            Assert.Contains(newVersionCreator.GetUserName(), e.Message);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var r = UpdateCardHelper.RequestForVisibilityChange(card, new[] { newVersionCreator.Id }, newVersionCreator.Id);
            var e = await Assert.ThrowsExactlyAsync<RequestInputException>(async () => await new UpdateCard(dbContext.AsCallContext()).RunAsync(r));
            Assert.IsTrue(e.Message.Contains(cardCreator.GetUserName()));
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var r = UpdateCardHelper.RequestForVisibilityChange(card, new[] { cardCreator.Id, newVersionCreator.Id });
            await new UpdateCard(dbContext.AsCallContext()).RunAsync(r);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            CardVisibilityHelper.CheckUserIsAllowedToViewCard(dbContext, cardCreator.Id, card.Id);
            CardVisibilityHelper.CheckUserIsAllowedToViewCard(dbContext, newVersionCreator.Id, card.Id);
        }
    }
    [TestMethod()]
    public async Task ReduceVisibility_OtherUserHasInDeck_OtherAuthor()
    {
        var db = DbHelper.GetEmptyTestDB();
        var cardCreator = await UserHelper.CreateInDbAsync(db);
        var languageId = await CardLanguageHelper.CreateAsync(db);
        var card = await CardHelper.CreateAsync(db, cardCreator, language: languageId, userWithViewIds: Array.Empty<Guid>());

        var userWithCardInDeck = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, userWithCardInDeck);
        await DeckHelper.AddCardAsync(db, deck, card.Id, 0);

        var otherUser = await UserHelper.CreateInDbAsync(db);
        using (var dbContext = new MemCheckDbContext(db))
        {
            var r = UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String(), otherUser);
            await new UpdateCard(dbContext.AsCallContext()).RunAsync(r);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var r = UpdateCardHelper.RequestForVisibilityChange(card, cardCreator.AsArray());
            await Assert.ThrowsExactlyAsync<RequestInputException>(async () => await new UpdateCard(dbContext.AsCallContext()).RunAsync(r));
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var r = UpdateCardHelper.RequestForVisibilityChange(card, otherUser.AsArray(), otherUser);
            await Assert.ThrowsExactlyAsync<RequestInputException>(async () => await new UpdateCard(dbContext.AsCallContext()).RunAsync(r));
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var r = UpdateCardHelper.RequestForVisibilityChange(card, new[] { userWithCardInDeck }, userWithCardInDeck);
            await Assert.ThrowsExactlyAsync<RequestInputException>(async () => await new UpdateCard(dbContext.AsCallContext()).RunAsync(r));
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var r = UpdateCardHelper.RequestForVisibilityChange(card, new[] { cardCreator, otherUser, userWithCardInDeck });
            await new UpdateCard(dbContext.AsCallContext()).RunAsync(r);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            CardVisibilityHelper.CheckUserIsAllowedToViewCard(dbContext, cardCreator, card.Id);
            CardVisibilityHelper.CheckUserIsAllowedToViewCard(dbContext, otherUser, card.Id);
            CardVisibilityHelper.CheckUserIsAllowedToViewCard(dbContext, userWithCardInDeck, card.Id);
        }
    }
    [TestMethod()]
    public async Task UpdateDoesNotAlterRatings()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var languageId = await CardLanguageHelper.CreateAsync(db);
        var card = await CardHelper.CreateAsync(db, user, language: languageId);
        var rating = RandomHelper.Rating();

        using (var dbContext = new MemCheckDbContext(db))
            await new SetCardRating(dbContext.AsCallContext()).RunAsync(new SetCardRating.Request(user, card.Id, rating));

        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateCard(dbContext.AsCallContext()).RunAsync(UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String()));

        using (var dbContext = new MemCheckDbContext(db))
        {
            var loaded = dbContext.Cards.Single();
            Assert.AreEqual(1, loaded.RatingCount);
            Assert.AreEqual(rating, loaded.AverageRating);
        }
    }
    [TestMethod()]
    public async Task ReferencesTooLong()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var creatorId = await UserHelper.CreateInDbAsync(testDB);
        var originalCard = await CardHelper.CreateAsync(testDB, creatorId);

        var request = UpdateCardHelper.RequestForReferencesChange(originalCard, RandomHelper.String(CardInputValidator.MaxReferencesLength + 1));

        using var dbContext = new MemCheckDbContext(testDB);
        var exception = await Assert.ThrowsExactlyAsync<RequestInputException>(async () => await new UpdateCard(dbContext.AsCallContext()).RunAsync(request));
        Assert.Contains(CardInputValidator.MinReferencesLength.ToString(), exception.Message);
        Assert.Contains(CardInputValidator.MaxReferencesLength.ToString(), exception.Message);
        Assert.Contains((CardInputValidator.MaxReferencesLength + 1).ToString(), exception.Message);
    }
    [TestMethod()]
    public async Task UpdateFrontSideWithValueNotTrimmed()
    {
        var db = DbHelper.GetEmptyTestDB();
        var cardCreator = await UserHelper.CreateInDbAsync(db);
        var languageId = await CardLanguageHelper.CreateAsync(db);
        var originalCard = await CardHelper.CreateAsync(db, cardCreator, language: languageId);

        var newFrontSide = RandomHelper.String();

        using (var dbContext = new MemCheckDbContext(db))
        {
            var request = new UpdateCard.Request(
                originalCard.Id,
                cardCreator,
                newFrontSide + ' ',
                originalCard.BackSide,
                originalCard.AdditionalInfo,
                originalCard.References,
                languageId,
                Array.Empty<Guid>(),
                Array.Empty<Guid>(),
                RandomHelper.String());
            await new UpdateCard(dbContext.AsCallContext()).RunAsync(request);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var updatedCard = dbContext.Cards
                .Include(c => c.VersionCreator)
                .Include(c => c.CardLanguage)
                .Include(c => c.UsersWithView)
                .Include(c => c.TagsInCards)
                .Single();
            Assert.AreEqual(cardCreator, updatedCard.VersionCreator.Id);
            Assert.AreEqual(newFrontSide, updatedCard.FrontSide);
            Assert.AreEqual(originalCard.BackSide, updatedCard.BackSide);
            Assert.AreEqual(originalCard.AdditionalInfo, updatedCard.AdditionalInfo);
            Assert.AreEqual(originalCard.References, updatedCard.References);
            Assert.AreEqual(languageId, updatedCard.CardLanguage.Id);
            Assert.IsFalse(updatedCard.TagsInCards.Any());
            Assert.IsFalse(updatedCard.UsersWithView.Any());
        }
    }
    [TestMethod()]
    public async Task UpdateReferencesWithValueNotTrimmed()
    {
        var db = DbHelper.GetEmptyTestDB();
        var cardCreator = await UserHelper.CreateInDbAsync(db);
        var languageId = await CardLanguageHelper.CreateAsync(db);
        var originalCard = await CardHelper.CreateAsync(db, cardCreator, language: languageId);

        var newReferences = RandomHelper.String();

        using (var dbContext = new MemCheckDbContext(db))
        {
            var request = new UpdateCard.Request(
                originalCard.Id,
                cardCreator,
                originalCard.FrontSide,
                originalCard.BackSide,
                originalCard.AdditionalInfo,
                " " + newReferences,
                languageId,
                Array.Empty<Guid>(),
                Array.Empty<Guid>(),
                RandomHelper.String());
            await new UpdateCard(dbContext.AsCallContext()).RunAsync(request);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var updatedCard = dbContext.Cards
                .Include(c => c.VersionCreator)
                .Include(c => c.CardLanguage)
                .Include(c => c.UsersWithView)
                .Include(c => c.TagsInCards)
                .Single();
            Assert.AreEqual(cardCreator, updatedCard.VersionCreator.Id);
            Assert.AreEqual(originalCard.FrontSide, updatedCard.FrontSide);
            Assert.AreEqual(originalCard.BackSide, updatedCard.BackSide);
            Assert.AreEqual(originalCard.AdditionalInfo, updatedCard.AdditionalInfo);
            Assert.AreEqual(newReferences, updatedCard.References);
            Assert.AreEqual(languageId, updatedCard.CardLanguage.Id);
            Assert.IsFalse(updatedCard.TagsInCards.Any());
            Assert.IsFalse(updatedCard.UsersWithView.Any());
        }
    }
    [TestMethod()]
    public async Task AddingPersoTagToPublicCardMustFail()
    {
        var db = DbHelper.GetEmptyTestDB();
        var creator = await UserHelper.CreateUserInDbAsync(db);
        var languageId = await CardLanguageHelper.CreateAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, creator.Id, language: languageId, userWithViewIds: Array.Empty<Guid>());

        var persoTagId = await TagHelper.CreateAsync(db, creator, name: Tag.Perso);

        var request = new UpdateCard.Request(
            cardId,
            creator.Id,
            RandomHelper.String(),
            RandomHelper.String(),
            RandomHelper.String(),
            RandomHelper.String(),
            languageId,
            persoTagId.AsArray(),
            Array.Empty<Guid>(),
            RandomHelper.String());

        var errorMesg = RandomHelper.String();
        var localizer = new TestLocalizer("PersoTagAllowedOnlyOnPrivateCards".PairedWith(errorMesg));
        using var dbContext = new MemCheckDbContext(db);
        var exception = await Assert.ThrowsExactlyAsync<PersoTagAllowedOnlyOnPrivateCardsException>(async () => await new UpdateCard(dbContext.AsCallContext(localizer)).RunAsync(request));
        Assert.AreEqual(errorMesg, exception.Message);
    }
    [TestMethod()]
    public async Task AddingPersoTagToCardVisibleToOtherMustFail()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var creator = await UserHelper.CreateUserInDbAsync(testDB);
        var otherUser = await UserHelper.CreateUserInDbAsync(testDB);
        var languageId = await CardLanguageHelper.CreateAsync(testDB);
        var cardId = await CardHelper.CreateIdAsync(testDB, creator, language: languageId, usersWithView: new[] { creator, otherUser });

        var persoTagId = await TagHelper.CreateAsync(testDB, creator, name: Tag.Perso);

        var request = new UpdateCard.Request(
            cardId,
            creator.Id,
            RandomHelper.String(),
            RandomHelper.String(),
            RandomHelper.String(),
            RandomHelper.String(),
            languageId,
            persoTagId.AsArray(),
            new[] { creator.Id, otherUser.Id },
            RandomHelper.String());

        var errorMesg = RandomHelper.String();
        var localizer = new TestLocalizer("PersoTagAllowedOnlyOnPrivateCards".PairedWith(errorMesg));
        using var dbContext = new MemCheckDbContext(testDB);
        var exception = await Assert.ThrowsExactlyAsync<PersoTagAllowedOnlyOnPrivateCardsException>(async () => await new UpdateCard(dbContext.AsCallContext(localizer)).RunAsync(request));
        Assert.AreEqual(errorMesg, exception.Message);
    }
    [TestMethod()]
    public async Task AddingPersoTagToPrivateCardMustSucceed()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var creator = await UserHelper.CreateUserInDbAsync(testDB);
        var languageId = await CardLanguageHelper.CreateAsync(testDB);
        var cardId = await CardHelper.CreateIdAsync(testDB, creator, language: languageId, usersWithView: creator.AsArray());

        var persoTagId = await TagHelper.CreateAsync(testDB, creator, name: Tag.Perso);

        var request = new UpdateCard.Request(
            cardId,
            creator.Id,
            RandomHelper.String(),
            RandomHelper.String(),
            RandomHelper.String(),
            RandomHelper.String(),
            languageId,
            persoTagId.AsArray(),
            creator.Id.AsArray(),
            RandomHelper.String());

        using (var dbContext = new MemCheckDbContext(testDB))
            await new UpdateCard(dbContext.AsCallContext()).RunAsync(request);

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var card = dbContext.Cards.Include(card => card.VersionCreator).Single();
            Assert.AreEqual(creator.Id, card.VersionCreator.Id);
        }
    }
    [TestMethod()]
    public async Task ChangingAPersoCardToPublicAndAddingPersoTagMustFail()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var creator = await UserHelper.CreateUserInDbAsync(testDB);
        var languageId = await CardLanguageHelper.CreateAsync(testDB);
        var aTagId = await TagHelper.CreateAsync(testDB, creator);
        var cardId = await CardHelper.CreateIdAsync(testDB, creator, language: languageId, usersWithView: creator.AsArray(), tagIds: aTagId.AsArray());

        var persoTagId = await TagHelper.CreateAsync(testDB, creator, name: Tag.Perso);

        var request = new UpdateCard.Request(
            cardId,
            creator.Id,
            RandomHelper.String(),
            RandomHelper.String(),
            RandomHelper.String(),
            RandomHelper.String(),
            languageId,
            persoTagId.AsArray(),
            Array.Empty<Guid>(),
            RandomHelper.String());

        var errorMesg = RandomHelper.String();
        var localizer = new TestLocalizer("PersoTagAllowedOnlyOnPrivateCards".PairedWith(errorMesg));
        using var dbContext = new MemCheckDbContext(testDB);
        var exception = await Assert.ThrowsExactlyAsync<PersoTagAllowedOnlyOnPrivateCardsException>(async () => await new UpdateCard(dbContext.AsCallContext(localizer)).RunAsync(request));
        Assert.AreEqual(errorMesg, exception.Message);
    }
    [TestMethod()]
    public async Task ChangingAPersoCardToCardVisibleToOtherAndAddingPersoTagMustFail()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var creator = await UserHelper.CreateUserInDbAsync(testDB);
        var languageId = await CardLanguageHelper.CreateAsync(testDB);
        var aTagId = await TagHelper.CreateAsync(testDB, creator);
        var cardId = await CardHelper.CreateIdAsync(testDB, creator.Id, language: languageId, userWithViewIds: creator.Id.AsArray(), tagIds: aTagId.AsArray());

        var persoTagId = await TagHelper.CreateAsync(testDB, creator, name: Tag.Perso);
        var otherUserId = await UserHelper.CreateInDbAsync(testDB);

        var request = new UpdateCard.Request(
            cardId,
            creator.Id,
            RandomHelper.String(),
            RandomHelper.String(),
            RandomHelper.String(),
            RandomHelper.String(),
            languageId,
            persoTagId.AsArray(),
            new[] { creator.Id, otherUserId },
            RandomHelper.String());

        var errorMesg = RandomHelper.String();
        var localizer = new TestLocalizer("PersoTagAllowedOnlyOnPrivateCards".PairedWith(errorMesg));
        using var dbContext = new MemCheckDbContext(testDB);
        var exception = await Assert.ThrowsExactlyAsync<PersoTagAllowedOnlyOnPrivateCardsException>(async () => await new UpdateCard(dbContext.AsCallContext(localizer)).RunAsync(request));
        Assert.AreEqual(errorMesg, exception.Message);
    }
    [TestMethod()]
    public async Task ChangingAPublicCardToPrivateAndAddingPersoTagMustSucceed()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var creator = await UserHelper.CreateUserInDbAsync(testDB);
        var languageId = await CardLanguageHelper.CreateAsync(testDB);
        var cardId = await CardHelper.CreateIdAsync(testDB, creator.Id, language: languageId, userWithViewIds: Array.Empty<Guid>());

        var persoTagId = await TagHelper.CreateAsync(testDB, creator, name: Tag.Perso);

        var request = new UpdateCard.Request(
            cardId,
            creator.Id,
            RandomHelper.String(),
            RandomHelper.String(),
            RandomHelper.String(),
            RandomHelper.String(),
            languageId,
            persoTagId.AsArray(),
            creator.Id.AsArray(),
            RandomHelper.String());

        using (var dbContext = new MemCheckDbContext(testDB))
            await new UpdateCard(dbContext.AsCallContext()).RunAsync(request);

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var card = dbContext.Cards.Include(card => card.VersionCreator).Single();
            Assert.AreEqual(creator.Id, card.VersionCreator.Id);
        }
    }
    [TestMethod()]
    public async Task ChangingAPersoCardWithPersoTagToPublicMustFail()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var creator = await UserHelper.CreateUserInDbAsync(testDB);
        var languageId = await CardLanguageHelper.CreateAsync(testDB);
        var aTagId = await TagHelper.CreateAsync(testDB, creator);
        var persoTagId = await TagHelper.CreateAsync(testDB, creator, name: Tag.Perso);
        var cardId = await CardHelper.CreateIdAsync(testDB, creator.Id, language: languageId, userWithViewIds: creator.Id.AsArray(), tagIds: new[] { aTagId, persoTagId });

        var request = new UpdateCard.Request(
            cardId,
            creator.Id,
            RandomHelper.String(),
            RandomHelper.String(),
            RandomHelper.String(),
            RandomHelper.String(),
            languageId,
            new[] { aTagId, persoTagId },
            Array.Empty<Guid>(),
            RandomHelper.String());

        var errorMesg = RandomHelper.String();
        var localizer = new TestLocalizer("PersoTagAllowedOnlyOnPrivateCards".PairedWith(errorMesg));
        using var dbContext = new MemCheckDbContext(testDB);
        var exception = await Assert.ThrowsExactlyAsync<PersoTagAllowedOnlyOnPrivateCardsException>(async () => await new UpdateCard(dbContext.AsCallContext(localizer)).RunAsync(request));
        Assert.AreEqual(errorMesg, exception.Message);
    }
    [TestMethod()]
    public async Task ChangingAPersoCardWithPersoTagToLimitedVisibilityMustFail()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var creator = await UserHelper.CreateUserInDbAsync(testDB);
        var languageId = await CardLanguageHelper.CreateAsync(testDB);
        var aTagId = await TagHelper.CreateAsync(testDB, creator);
        var persoTagId = await TagHelper.CreateAsync(testDB, creator, name: Tag.Perso);
        var cardId = await CardHelper.CreateIdAsync(testDB, creator, language: languageId, usersWithView: creator.AsArray(), tagIds: new[] { aTagId, persoTagId });

        var otherUserId = await UserHelper.CreateInDbAsync(testDB);

        var request = new UpdateCard.Request(
            cardId,
            creator.Id,
            RandomHelper.String(),
            RandomHelper.String(),
            RandomHelper.String(),
            RandomHelper.String(),
            languageId,
            new[] { aTagId, persoTagId },
            new[] { creator.Id, otherUserId },
            RandomHelper.String());

        var errorMesg = RandomHelper.String();
        var localizer = new TestLocalizer("PersoTagAllowedOnlyOnPrivateCards".PairedWith(errorMesg));
        using var dbContext = new MemCheckDbContext(testDB);
        var exception = await Assert.ThrowsExactlyAsync<PersoTagAllowedOnlyOnPrivateCardsException>(async () => await new UpdateCard(dbContext.AsCallContext(localizer)).RunAsync(request));
        Assert.AreEqual(errorMesg, exception.Message);
    }
    [TestMethod()]
    public async Task AddImageToCard_DidNotContainAny()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var card = await CardHelper.CreateAsync(db, userId);

        var imageName = RandomHelper.String();
        var imageId = await ImageHelper.CreateAsync(db, userId, imageName);


        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateCard(dbContext.AsCallContext()).RunAsync(UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String() + $"![Mnesios:{imageName},size=small] " + RandomHelper.String()));

        using (var dbContext = new MemCheckDbContext(db))
        {
            Assert.AreEqual(1, dbContext.ImagesInCards.Count());
            Assert.AreEqual(card.Id, dbContext.ImagesInCards.Single().CardId);
            Assert.AreEqual(imageId, dbContext.ImagesInCards.Single().ImageId);
        }
    }
    [TestMethod()]
    public async Task AddImageToCard_ContainedOne()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);

        var image1Name = RandomHelper.String();
        var image1Id = await ImageHelper.CreateAsync(db, userId, image1Name);

        var card = await CardHelper.CreateAsync(db, userId, backSide: $"\t![Mnesios:{image1Name},size=small] " + RandomHelper.String());

        var image2Name = RandomHelper.String();
        var image2Id = await ImageHelper.CreateAsync(db, userId, image2Name);

        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateCard(dbContext.AsCallContext()).RunAsync(UpdateCardHelper.RequestForFrontSideChange(card, $"\t![Mnesios:{image1Name},size=small] ![Mnesios:{image2Name},size=big]"));

        using (var dbContext = new MemCheckDbContext(db))
        {
            Assert.AreEqual(2, dbContext.ImagesInCards.Count());

            Assert.AreEqual(1, dbContext.ImagesInCards.Count(imageInCard => imageInCard.ImageId == image1Id));
            Assert.AreEqual(card.Id, dbContext.ImagesInCards.Single(imageInCard => imageInCard.ImageId == image1Id).CardId);

            Assert.AreEqual(1, dbContext.ImagesInCards.Count(imageInCard => imageInCard.ImageId == image2Id));
            Assert.AreEqual(card.Id, dbContext.ImagesInCards.Single(imageInCard => imageInCard.ImageId == image2Id).CardId);
        }
    }
    [TestMethod()]
    public async Task AddImageToCard_ContainedMany()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);

        var image1Name = RandomHelper.String();
        var image1Id = await ImageHelper.CreateAsync(db, userId, image1Name);
        var image2Name = RandomHelper.String();
        var image2Id = await ImageHelper.CreateAsync(db, userId, image2Name);
        var image3Name = RandomHelper.String();
        var image3Id = await ImageHelper.CreateAsync(db, userId, image3Name);

        var card = await CardHelper.CreateAsync(db, userId, frontSide: $"![Mnesios:{image1Name}]", backSide: $"![Mnesios:{image2Name}]", additionalInfo: $"![Mnesios:{image3Name}]");

        var addedImageName = RandomHelper.String();
        var addedImageId = await ImageHelper.CreateAsync(db, userId, addedImageName);

        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateCard(dbContext.AsCallContext()).RunAsync(UpdateCardHelper.RequestForAdditionalInfoChange(card, $"![Mnesios:{image3Name}]![Mnesios:{addedImageName}]"));

        using (var dbContext = new MemCheckDbContext(db))
        {
            Assert.AreEqual(4, dbContext.ImagesInCards.Count());

            Assert.AreEqual(1, dbContext.ImagesInCards.Count(imageInCard => imageInCard.ImageId == image1Id));
            Assert.AreEqual(card.Id, dbContext.ImagesInCards.Single(imageInCard => imageInCard.ImageId == image1Id).CardId);

            Assert.AreEqual(1, dbContext.ImagesInCards.Count(imageInCard => imageInCard.ImageId == image2Id));
            Assert.AreEqual(card.Id, dbContext.ImagesInCards.Single(imageInCard => imageInCard.ImageId == image2Id).CardId);

            Assert.AreEqual(1, dbContext.ImagesInCards.Count(imageInCard => imageInCard.ImageId == image3Id));
            Assert.AreEqual(card.Id, dbContext.ImagesInCards.Single(imageInCard => imageInCard.ImageId == image3Id).CardId);

            Assert.AreEqual(1, dbContext.ImagesInCards.Count(imageInCard => imageInCard.ImageId == addedImageId));
            Assert.AreEqual(card.Id, dbContext.ImagesInCards.Single(imageInCard => imageInCard.ImageId == addedImageId).CardId);
        }
    }
    [TestMethod()]
    public async Task AddImagesToCard_DidNotContainAny()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var card = await CardHelper.CreateAsync(db, userId);

        var image1Name = RandomHelper.String();
        var image1Id = await ImageHelper.CreateAsync(db, userId, image1Name);
        var image2Name = RandomHelper.String();
        var image2Id = await ImageHelper.CreateAsync(db, userId, image2Name);

        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateCard(dbContext.AsCallContext()).RunAsync(UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String() + $"![Mnesios:{image1Name},size=small] ![Mnesios:{image2Name},size=medium]"));

        using (var dbContext = new MemCheckDbContext(db))
        {
            Assert.AreEqual(2, dbContext.ImagesInCards.Count());

            Assert.AreEqual(1, dbContext.ImagesInCards.Count(imageInCard => imageInCard.ImageId == image1Id));
            Assert.AreEqual(card.Id, dbContext.ImagesInCards.Single(imageInCard => imageInCard.ImageId == image1Id).CardId);

            Assert.AreEqual(1, dbContext.ImagesInCards.Count(imageInCard => imageInCard.ImageId == image2Id));
            Assert.AreEqual(card.Id, dbContext.ImagesInCards.Single(imageInCard => imageInCard.ImageId == image2Id).CardId);
        }
    }
    [TestMethod()]
    public async Task AddImagesToCard_ContainedNone()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);

        var card = await CardHelper.CreateAsync(db, userId);

        var image1Name = RandomHelper.String();
        var image1Id = await ImageHelper.CreateAsync(db, userId, image1Name);

        var image2Name = RandomHelper.String();
        var image2Id = await ImageHelper.CreateAsync(db, userId, image2Name);

        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateCard(dbContext.AsCallContext()).RunAsync(UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String() + $"![Mnesios:{image1Name},size=small] ![Mnesios:{image2Name},size=medium]"));

        using (var dbContext = new MemCheckDbContext(db))
        {
            Assert.AreEqual(2, dbContext.ImagesInCards.Count());

            Assert.AreEqual(1, dbContext.ImagesInCards.Count(imageInCard => imageInCard.ImageId == image1Id));
            Assert.AreEqual(card.Id, dbContext.ImagesInCards.Single(imageInCard => imageInCard.ImageId == image1Id).CardId);

            Assert.AreEqual(1, dbContext.ImagesInCards.Count(imageInCard => imageInCard.ImageId == image2Id));
            Assert.AreEqual(card.Id, dbContext.ImagesInCards.Single(imageInCard => imageInCard.ImageId == image2Id).CardId);
        }
    }
    [TestMethod()]
    public async Task AddImagesToCard_ContainedOne()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);

        var image1Name = RandomHelper.String();
        var image1Id = await ImageHelper.CreateAsync(db, userId, image1Name);

        var card = await CardHelper.CreateAsync(db, userId, additionalInfo: $"![Mnesios:{image1Name}]");

        var image2Name = RandomHelper.String();
        var image2Id = await ImageHelper.CreateAsync(db, userId, image2Name);

        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateCard(dbContext.AsCallContext()).RunAsync(UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String() + $"![Mnesios:{image1Name},size=small] ![Mnesios:{image2Name},size=medium]"));

        using (var dbContext = new MemCheckDbContext(db))
        {
            Assert.AreEqual(2, dbContext.ImagesInCards.Count());

            Assert.AreEqual(1, dbContext.ImagesInCards.Count(imageInCard => imageInCard.ImageId == image1Id));
            Assert.AreEqual(card.Id, dbContext.ImagesInCards.Single(imageInCard => imageInCard.ImageId == image1Id).CardId);

            Assert.AreEqual(1, dbContext.ImagesInCards.Count(imageInCard => imageInCard.ImageId == image2Id));
            Assert.AreEqual(card.Id, dbContext.ImagesInCards.Single(imageInCard => imageInCard.ImageId == image2Id).CardId);
        }
    }
    [TestMethod()]
    public async Task AddImagesToCard_ContainedMany()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);

        var image1Name = RandomHelper.String();
        var image1Id = await ImageHelper.CreateAsync(db, userId, image1Name);
        var image2Name = RandomHelper.String();
        var image2Id = await ImageHelper.CreateAsync(db, userId, image2Name);

        var card = await CardHelper.CreateAsync(db, userId, backSide: $"![Mnesios:{image2Name}]", additionalInfo: $"![Mnesios:{image1Name}]");

        var image3Name = RandomHelper.String();
        var image3Id = await ImageHelper.CreateAsync(db, userId, image3Name);

        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateCard(dbContext.AsCallContext()).RunAsync(UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String() + $"![Mnesios:{image3Name},size=big] ![Mnesios:{image2Name},size=medium]"));

        using (var dbContext = new MemCheckDbContext(db))
        {
            Assert.AreEqual(3, dbContext.ImagesInCards.Count());

            Assert.AreEqual(1, dbContext.ImagesInCards.Count(imageInCard => imageInCard.ImageId == image1Id));
            Assert.AreEqual(card.Id, dbContext.ImagesInCards.Single(imageInCard => imageInCard.ImageId == image1Id).CardId);

            Assert.AreEqual(1, dbContext.ImagesInCards.Count(imageInCard => imageInCard.ImageId == image2Id));
            Assert.AreEqual(card.Id, dbContext.ImagesInCards.Single(imageInCard => imageInCard.ImageId == image2Id).CardId);

            Assert.AreEqual(1, dbContext.ImagesInCards.Count(imageInCard => imageInCard.ImageId == image3Id));
            Assert.AreEqual(card.Id, dbContext.ImagesInCards.Single(imageInCard => imageInCard.ImageId == image3Id).CardId);
        }
    }
    [TestMethod()]
    public async Task RemoveOnlyImageFromCard()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);

        var imageName = RandomHelper.String();
        await ImageHelper.CreateAsync(db, userId, imageName);

        var card = await CardHelper.CreateAsync(db, userId, frontSide: $"![Mnesios:{imageName}]");

        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateCard(dbContext.AsCallContext()).RunAsync(UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String()));

        using (var dbContext = new MemCheckDbContext(db))
            Assert.IsFalse(dbContext.ImagesInCards.Any());
    }
    [TestMethod()]
    public async Task RemoveImageFromCardContainingMore()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);

        var image1Name = RandomHelper.String();
        var image1Id = await ImageHelper.CreateAsync(db, userId, image1Name);
        var image2Name = RandomHelper.String();
        var image2Id = await ImageHelper.CreateAsync(db, userId, image2Name);
        var image3Name = RandomHelper.String();
        var image3Id = await ImageHelper.CreateAsync(db, userId, image3Name);

        var card = await CardHelper.CreateAsync(db, userId, frontSide: $"![Mnesios:{image3Name}]", backSide: $"![Mnesios:{image2Name}]", additionalInfo: $"![Mnesios:{image1Name}]");

        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateCard(dbContext.AsCallContext()).RunAsync(UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String()));

        using (var dbContext = new MemCheckDbContext(db))
        {
            Assert.AreEqual(2, dbContext.ImagesInCards.Count());

            Assert.AreEqual(1, dbContext.ImagesInCards.Count(imageInCard => imageInCard.ImageId == image1Id));
            Assert.AreEqual(card.Id, dbContext.ImagesInCards.Single(imageInCard => imageInCard.ImageId == image1Id).CardId);

            Assert.AreEqual(1, dbContext.ImagesInCards.Count(imageInCard => imageInCard.ImageId == image2Id));
            Assert.AreEqual(card.Id, dbContext.ImagesInCards.Single(imageInCard => imageInCard.ImageId == image2Id).CardId);
        }
    }
    [TestMethod()]
    public async Task RemoveAllImagesFromCard()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);

        var image1Name = RandomHelper.String();
        var image1Id = await ImageHelper.CreateAsync(db, userId, image1Name);
        var image2Name = RandomHelper.String();
        var image2Id = await ImageHelper.CreateAsync(db, userId, image2Name);
        var image3Name = RandomHelper.String();
        var image3Id = await ImageHelper.CreateAsync(db, userId, image3Name);

        var card = await CardHelper.CreateAsync(db, userId, frontSide: $"![Mnesios:{image3Name}]", backSide: $"![Mnesios:{image2Name}]", additionalInfo: $"![Mnesios:{image1Name}]");

        using (var dbContext = new MemCheckDbContext(db))
        {
            var request = new UpdateCard.Request(card.Id, card.VersionCreator.Id, RandomHelper.String(), RandomHelper.String(), RandomHelper.String(), card.References, card.CardLanguage.Id, card.TagsInCards.Select(t => t.TagId), card.UsersWithView.Select(uwv => uwv.UserId), RandomHelper.String());
            await new UpdateCard(dbContext.AsCallContext()).RunAsync(request);
        }

        using (var dbContext = new MemCheckDbContext(db))
            Assert.IsFalse(dbContext.ImagesInCards.Any());
    }
    [TestMethod()]
    public async Task RemoveImagesFromCardContainingMore()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);

        var image1Name = RandomHelper.String();
        var image1Id = await ImageHelper.CreateAsync(db, userId, image1Name);
        var image2Name = RandomHelper.String();
        await ImageHelper.CreateAsync(db, userId, image2Name);
        var image3Name = RandomHelper.String();
        await ImageHelper.CreateAsync(db, userId, image3Name);

        var card = await CardHelper.CreateAsync(db, userId, frontSide: $"![Mnesios:{image3Name}] ![Mnesios:{image2Name}]", additionalInfo: $"![Mnesios:{image1Name}]");

        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateCard(dbContext.AsCallContext()).RunAsync(UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String()));

        using (var dbContext = new MemCheckDbContext(db))
        {
            Assert.AreEqual(1, dbContext.ImagesInCards.Count());

            Assert.AreEqual(1, dbContext.ImagesInCards.Count(imageInCard => imageInCard.ImageId == image1Id));
            Assert.AreEqual(card.Id, dbContext.ImagesInCards.Single(imageInCard => imageInCard.ImageId == image1Id).CardId);
        }
    }
    [TestMethod()]
    public async Task ReplaceOnlyImageInCard()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);

        var image1Name = RandomHelper.String();
        await ImageHelper.CreateAsync(db, userId, image1Name);

        var card = await CardHelper.CreateAsync(db, userId, frontSide: $"![Mnesios:{image1Name}]");

        var image2Name = RandomHelper.String();
        var image2Id = await ImageHelper.CreateAsync(db, userId, image2Name);

        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateCard(dbContext.AsCallContext()).RunAsync(UpdateCardHelper.RequestForFrontSideChange(card, $"![Mnesios:{image2Name}]"));

        using (var dbContext = new MemCheckDbContext(db))
        {
            Assert.AreEqual(1, dbContext.ImagesInCards.Count());

            Assert.AreEqual(1, dbContext.ImagesInCards.Count(imageInCard => imageInCard.ImageId == image2Id));
            Assert.AreEqual(card.Id, dbContext.ImagesInCards.Single(imageInCard => imageInCard.ImageId == image2Id).CardId);
        }
    }
    [TestMethod()]
    public async Task ReplaceImageInCardContainingMore()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);

        var image1Name = RandomHelper.String();
        var image1Id = await ImageHelper.CreateAsync(db, userId, image1Name);
        var image2Name = RandomHelper.String();
        var image2Id = await ImageHelper.CreateAsync(db, userId, image2Name);
        var image3Name = RandomHelper.String();
        await ImageHelper.CreateAsync(db, userId, image3Name);

        var card = await CardHelper.CreateAsync(db, userId, frontSide: $"![Mnesios:{image3Name}] ![Mnesios:{image2Name}]", backSide: $" ![Mnesios:{image2Name}]", additionalInfo: $"![Mnesios:{image1Name}]");

        var image4Name = RandomHelper.String();
        var image4Id = await ImageHelper.CreateAsync(db, userId, image4Name);

        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateCard(dbContext.AsCallContext()).RunAsync(UpdateCardHelper.RequestForFrontSideChange(card, $"![Mnesios:{image4Name}]"));

        using (var dbContext = new MemCheckDbContext(db))
        {
            Assert.AreEqual(3, dbContext.ImagesInCards.Count());

            Assert.AreEqual(1, dbContext.ImagesInCards.Count(imageInCard => imageInCard.ImageId == image1Id));
            Assert.AreEqual(card.Id, dbContext.ImagesInCards.Single(imageInCard => imageInCard.ImageId == image1Id).CardId);

            Assert.AreEqual(1, dbContext.ImagesInCards.Count(imageInCard => imageInCard.ImageId == image2Id));
            Assert.AreEqual(card.Id, dbContext.ImagesInCards.Single(imageInCard => imageInCard.ImageId == image2Id).CardId);

            Assert.AreEqual(1, dbContext.ImagesInCards.Count(imageInCard => imageInCard.ImageId == image4Id));
            Assert.AreEqual(card.Id, dbContext.ImagesInCards.Single(imageInCard => imageInCard.ImageId == image4Id).CardId);
        }
    }
    [TestMethod()]
    public async Task ReplaceImagesInCardContainingMore()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);

        var image1Name = RandomHelper.String();
        var image1Id = await ImageHelper.CreateAsync(db, userId, image1Name);
        var image2Name = RandomHelper.String();
        var image2Id = await ImageHelper.CreateAsync(db, userId, image2Name);
        var image3Name = RandomHelper.String();
        await ImageHelper.CreateAsync(db, userId, image3Name);

        var card = await CardHelper.CreateAsync(db, userId, frontSide: $"![Mnesios:{image3Name}] ![Mnesios:{image2Name}]", additionalInfo: $"![Mnesios:{image1Name}]");

        var image4Name = RandomHelper.String();
        var image4Id = await ImageHelper.CreateAsync(db, userId, image4Name);

        var image5Name = RandomHelper.String();
        var image5Id = await ImageHelper.CreateAsync(db, userId, image5Name);

        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateCard(dbContext.AsCallContext()).RunAsync(UpdateCardHelper.RequestForFrontSideChange(card, $"![Mnesios:{image4Name}]![Mnesios:{image5Name}]"));

        using (var dbContext = new MemCheckDbContext(db))
        {
            Assert.AreEqual(3, dbContext.ImagesInCards.Count());

            Assert.AreEqual(1, dbContext.ImagesInCards.Count(imageInCard => imageInCard.ImageId == image1Id));
            Assert.AreEqual(card.Id, dbContext.ImagesInCards.Single(imageInCard => imageInCard.ImageId == image1Id).CardId);

            Assert.AreEqual(1, dbContext.ImagesInCards.Count(imageInCard => imageInCard.ImageId == image5Id));
            Assert.AreEqual(card.Id, dbContext.ImagesInCards.Single(imageInCard => imageInCard.ImageId == image5Id).CardId);

            Assert.AreEqual(1, dbContext.ImagesInCards.Count(imageInCard => imageInCard.ImageId == image4Id));
            Assert.AreEqual(card.Id, dbContext.ImagesInCards.Single(imageInCard => imageInCard.ImageId == image4Id).CardId);
        }
    }
    [TestMethod()]
    public async Task AddNonExistingImageToCard()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);

        var imageName = RandomHelper.String();
        var imageId = await ImageHelper.CreateAsync(db, userId, imageName);

        var card = await CardHelper.CreateAsync(db, userId, backSide: $"\t![Mnesios:{imageName},size=small] " + RandomHelper.String());

        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateCard(dbContext.AsCallContext()).RunAsync(UpdateCardHelper.RequestForFrontSideChange(card, $"![Mnesios:{RandomHelper.String()}]"));

        using (var dbContext = new MemCheckDbContext(db))
        {
            Assert.AreEqual(1, dbContext.ImagesInCards.Count());

            Assert.AreEqual(1, dbContext.ImagesInCards.Count(imageInCard => imageInCard.ImageId == imageId));
            Assert.AreEqual(card.Id, dbContext.ImagesInCards.Single(imageInCard => imageInCard.ImageId == imageId).CardId);
        }
    }
    [TestMethod()]
    public async Task AddNonExistingImagesToCard()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);

        var card = await CardHelper.CreateAsync(db, userId);

        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateCard(dbContext.AsCallContext()).RunAsync(UpdateCardHelper.RequestForFrontSideChange(card, $"![Mnesios:{RandomHelper.String()}] ![Mnesios:{RandomHelper.String()}]"));

        using (var dbContext = new MemCheckDbContext(db))
            Assert.IsFalse(dbContext.ImagesInCards.Any());
    }
}

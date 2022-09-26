using MemCheck.Application.Helpers;
using MemCheck.Application.QueryValidation;
using MemCheck.Application.Users;
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
public class DeleteCardsTests
{
    [TestMethod()]
    public async Task FailIfUserDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var card = await CardHelper.CreateAsync(db, user);
        using var dbContext = new MemCheckDbContext(db);
        var e = await Assert.ThrowsExceptionAsync<NonexistentUserException>(async () => await CardDeletionHelper.DeleteCardAsync(db, Guid.NewGuid(), card.Id));
        Assert.AreEqual("User not found", e.Message);
    }
    [TestMethod()]
    public async Task FailIfNotAllowedToView()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userWithView = await UserHelper.CreateInDbAsync(db);
        var cardCreator = await UserHelper.CreateInDbAsync(db);
        var card = await CardHelper.CreateAsync(db, cardCreator, new DateTime(2020, 11, 1), userWithViewIds: new[] { userWithView, cardCreator });
        using (var dbContext = new MemCheckDbContext(db))
        using (var userManager = UserHelper.GetUserManager(dbContext))
            await new DeleteUserAccount(dbContext.AsCallContext(new TestRoleChecker(userWithView)), userManager).RunAsync(new DeleteUserAccount.Request(userWithView, cardCreator));
        var otherUser = await UserHelper.CreateInDbAsync(db);
        using (var dbContext = new MemCheckDbContext(db))
        {
            var deleter = new DeleteCards(dbContext.AsCallContext(new TestLocalizer("YouAreNotTheCreatorOfCurrentVersion".PairedWith("YouAreNotTheCreatorOfCurrentVersion"))));
            var e = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await deleter.RunAsync(new DeleteCards.Request(otherUser, card.Id.AsArray())));
            Assert.AreEqual("User not allowed to view card", e.Message);
        }
    }
    [TestMethod()]
    public async Task FailIfDeletedUser()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var card = await CardHelper.CreateAsync(db, user);
        var adminUser = await UserHelper.CreateInDbAsync(db);
        using (var dbContext = new MemCheckDbContext(db))
        using (var userManager = UserHelper.GetUserManager(dbContext))
            await new DeleteUserAccount(dbContext.AsCallContext(new TestRoleChecker(adminUser)), userManager).RunAsync(new DeleteUserAccount.Request(adminUser, user));
        using (var dbContext = new MemCheckDbContext(db))
        {
            var deleter = new DeleteCards(dbContext.AsCallContext());
            var e = await Assert.ThrowsExceptionAsync<NonexistentUserException>(async () => await deleter.RunAsync(new DeleteCards.Request(user, card.Id.AsArray())));
            Assert.AreEqual("User not found", e.Message);
        }
    }
    [TestMethod()]
    public async Task DeleteNonExistingCard()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var languageId = await CardLanguageHelper.CreateAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId, language: languageId);

        using var dbContext = new MemCheckDbContext(db);
        var deleter = new DeleteCards(dbContext.AsCallContext());
        var deletionRequest = new DeleteCards.Request(userId, new[] { cardId, RandomHelper.Guid() });
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await deleter.RunAsync(deletionRequest));
    }
    [TestMethod()]
    public async Task DeleteDeletedCardMustFail()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var card = await CardHelper.CreateAsync(db, user);
        await CardDeletionHelper.DeleteCardAsync(db, user, card.Id);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await CardDeletionHelper.DeleteCardAsync(db, user, card.Id));
    }
    [TestMethod()]
    public async Task DeletingMustNotDeleteCardNotifications()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var card = await CardHelper.CreateAsync(db, user, new DateTime(2020, 11, 1));
        await CardSubscriptionHelper.CreateAsync(db, user, card.Id);

        using (var dbContext = new MemCheckDbContext(db))
            Assert.AreEqual(1, dbContext.CardNotifications.Count());

        await CardDeletionHelper.DeleteCardAsync(db, user, card.Id, new DateTime(2020, 11, 2));

        using (var dbContext = new MemCheckDbContext(db))
            Assert.AreEqual(1, dbContext.CardNotifications.Count());
    }
    [TestMethod()]
    public async Task DeleteSingleCardSuccessfully()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var language = await CardLanguageHelper.CreateAsync(db);
        var creationDate = RandomHelper.Date();
        var card = await CardHelper.CreateAsync(db, user, language: language, versionDate: creationDate);
        var deletionDate = RandomHelper.Date();
        await CardDeletionHelper.DeleteCardAsync(db, user, card.Id, deletionDate);

        using var dbContext = new MemCheckDbContext(db);
        Assert.IsFalse(dbContext.Cards.Any());
        Assert.AreEqual(2, dbContext.CardPreviousVersions.Count());

        Assert.AreEqual(1, dbContext.CardPreviousVersions.Count(version => version.Card == card.Id && version.PreviousVersion == null));

        var firstVersion = dbContext.CardPreviousVersions
            .Include(version => version.VersionCreator)
            .Include(version => version.CardLanguage)
            .Include(version => version.Tags)
            .Include(version => version.UsersWithView)
            .Single(version => version.Card == card.Id && version.PreviousVersion == null);
        Assert.AreEqual(CardPreviousVersionType.Creation, firstVersion.VersionType);
        CardComparisonHelper.AssertSameContent(card, firstVersion, true);
        Assert.AreEqual(creationDate, firstVersion.VersionUtcDate);

        var deletionVersion = dbContext.CardPreviousVersions
            .Include(version => version.VersionCreator)
            .Include(version => version.CardLanguage)
            .Include(version => version.Tags)
            .Include(version => version.UsersWithView)
            .Single(version => version.Card == card.Id && version.PreviousVersion != null);
        Assert.AreEqual(CardPreviousVersionType.Deletion, deletionVersion.VersionType);
        Assert.AreEqual(firstVersion.Id, deletionVersion.PreviousVersion!.Id);
        CardComparisonHelper.AssertSameContent(card, deletionVersion, true);
        Assert.AreEqual(deletionDate, deletionVersion.VersionUtcDate);
    }
    [TestMethod()]
    public async Task SucceedsIfOtherUserHasCreatedAPreviousVersion()
    {
        var db = DbHelper.GetEmptyTestDB();
        var firstVersionCreatorId = await UserHelper.CreateInDbAsync(db);
        var languageId = await CardLanguageHelper.CreateAsync(db);
        var card = await CardHelper.CreateAsync(db, firstVersionCreatorId, language: languageId);

        var lastVersionCreatorId = await UserHelper.CreateInDbAsync(db);
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateCard(dbContext.AsCallContext()).RunAsync(UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String(), lastVersionCreatorId));

        using (var dbContext = new MemCheckDbContext(db))
            await new DeleteCards(dbContext.AsCallContext()).RunAsync(new DeleteCards.Request(lastVersionCreatorId, card.Id.AsArray()));

        using (var dbContext = new MemCheckDbContext(db))
        {
            Assert.IsFalse(dbContext.Cards.Any());
            Assert.AreEqual(3, dbContext.CardPreviousVersions.Count());
        }
    }
    [TestMethod()]
    public async Task SucceedsIfDeletedUserHasCreatedAPreviousVersion()
    {
        var db = DbHelper.GetEmptyTestDB();
        var firstVersionCreator = await UserHelper.CreateInDbAsync(db);
        var languageId = await CardLanguageHelper.CreateAsync(db);
        var card = await CardHelper.CreateAsync(db, firstVersionCreator, language: languageId);
        var lastVersionCreator = await UserHelper.CreateInDbAsync(db);
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateCard(dbContext.AsCallContext()).RunAsync(UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String(), lastVersionCreator));
        using (var dbContext = new MemCheckDbContext(db))
        using (var userManager = UserHelper.GetUserManager(dbContext))
            await new DeleteUserAccount(dbContext.AsCallContext(new TestRoleChecker(lastVersionCreator)), userManager).RunAsync(new DeleteUserAccount.Request(lastVersionCreator, firstVersionCreator));
        using (var dbContext = new MemCheckDbContext(db))
            await new DeleteCards(dbContext.AsCallContext()).RunAsync(new DeleteCards.Request(lastVersionCreator, card.Id.AsArray()));
        using (var dbContext = new MemCheckDbContext(db))
        {
            Assert.IsFalse(dbContext.Cards.Any());
            Assert.AreEqual(3, dbContext.CardPreviousVersions.Count());
        }
    }
    [TestMethod()]
    public async Task FailIfOtherUserHasCardInDeck()
    {
        var db = DbHelper.GetEmptyTestDB();
        var cardCreatorId = await UserHelper.CreateInDbAsync(db);
        var languageId = await CardLanguageHelper.CreateAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, cardCreatorId, language: languageId);

        var otherUserId = await UserHelper.CreateInDbAsync(db);
        var deckId = await DeckHelper.CreateAsync(db, otherUserId);
        await DeckHelper.AddCardAsync(db, deckId, cardId);

        using (var dbContext = new MemCheckDbContext(db))
        {
            var errorMesg = RandomHelper.String();
            var deleter = new DeleteCards(dbContext.AsCallContext(new TestLocalizer("OneUserHasCardWithFrontSide".PairedWith(errorMesg))));
            var e = await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await deleter.RunAsync(new DeleteCards.Request(cardCreatorId, cardId.AsArray())));
            StringAssert.Contains(e.Message, errorMesg);
        }

        using (var dbContext = new MemCheckDbContext(db))
            Assert.AreEqual(cardId, dbContext.Cards.Single().Id);
    }
    [TestMethod()]
    public async Task SucceedsIfDeleterHasCardInDeck()
    {
        var db = DbHelper.GetEmptyTestDB();
        var cardCreatorId = await UserHelper.CreateInDbAsync(db);
        var languageId = await CardLanguageHelper.CreateAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, cardCreatorId, language: languageId);

        var deckId = await DeckHelper.CreateAsync(db, cardCreatorId);
        await DeckHelper.AddCardAsync(db, deckId, cardId);

        using (var dbContext = new MemCheckDbContext(db))
            await new DeleteCards(dbContext.AsCallContext()).RunAsync(new DeleteCards.Request(cardCreatorId, cardId.AsArray()));

        using (var dbContext = new MemCheckDbContext(db))
            Assert.IsFalse(dbContext.Cards.Any());
    }
    [TestMethod()]
    public async Task SucceedsIfCardHasRating()
    {
        var db = DbHelper.GetEmptyTestDB();
        var cardCreatorId = await UserHelper.CreateInDbAsync(db);
        var languageId = await CardLanguageHelper.CreateAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, cardCreatorId, language: languageId);

        var raterId = await UserHelper.CreateInDbAsync(db);
        await RatingHelper.RecordForUserAsync(db, raterId, cardId, RandomHelper.Rating());

        using (var dbContext = new MemCheckDbContext(db))
            await new DeleteCards(dbContext.AsCallContext()).RunAsync(new DeleteCards.Request(cardCreatorId, cardId.AsArray()));

        using (var dbContext = new MemCheckDbContext(db))
            Assert.IsFalse(dbContext.Cards.Any());
    }
    [TestMethod()]
    public async Task SucceedsIfCreatorOfCurrentVersionIsDeleted()
    {
        var db = DbHelper.GetEmptyTestDB();
        var firstVersionCreator = await UserHelper.CreateInDbAsync(db);
        var languageId = await CardLanguageHelper.CreateAsync(db);
        var card = await CardHelper.CreateAsync(db, firstVersionCreator, language: languageId);
        var lastVersionCreator = await UserHelper.CreateInDbAsync(db);
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateCard(dbContext.AsCallContext()).RunAsync(UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String(), lastVersionCreator));
        await UserHelper.DeleteAsync(db, lastVersionCreator);
        using (var dbContext = new MemCheckDbContext(db))
            await new DeleteCards(dbContext.AsCallContext()).RunAsync(new DeleteCards.Request(firstVersionCreator, card.Id.AsArray()));
        using (var dbContext = new MemCheckDbContext(db))
        {
            Assert.IsFalse(dbContext.Cards.Any());
            Assert.AreEqual(3, dbContext.CardPreviousVersions.Count());
        }
    }
    [TestMethod()]
    public async Task DeleteThreeCardsSuccessfully()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user1 = await UserHelper.CreateInDbAsync(db);
        var user2 = await UserHelper.CreateInDbAsync(db);
        var language = await CardLanguageHelper.CreateAsync(db);

        var deletedCard1Id = await CardHelper.CreateIdAsync(db, user1, language: language);
        var deletedCard2Id = await CardHelper.CreateIdAsync(db, user2, language: language);
        var deletedCard3Id = await CardHelper.CreateIdAsync(db, user1, language: language);

        var deckId = await DeckHelper.CreateAsync(db, user2);
        await DeckHelper.AddCardAsync(db, deckId, deletedCard1Id);

        await RatingHelper.RecordForUserAsync(db, user1, deletedCard2Id, RandomHelper.Rating());

        var nonDeletedCardId = await CardHelper.CreateIdAsync(db, user1, language: language);

        var deletionDate = RandomHelper.Date();
        using (var dbContext = new MemCheckDbContext(db))
            await new DeleteCards(dbContext.AsCallContext(), deletionDate).RunAsync(new DeleteCards.Request(user2, new[] { deletedCard1Id, deletedCard2Id, deletedCard3Id }));

        using (var dbContext = new MemCheckDbContext(db))
        {
            Assert.AreEqual(1, dbContext.Cards.Count());
            Assert.AreEqual(nonDeletedCardId, dbContext.Cards.Single().Id);
        }
    }
    [TestMethod()]
    public async Task DeleteCardContainingImage()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);

        var imageName = RandomHelper.String();
        await ImageHelper.CreateAsync(db, userId, imageName);

        var cardId = await CardHelper.CreateIdAsync(db, userId, additionalInfo: $"![Mnesios:{imageName}]");
        await CardHelper.CreateIdAsync(db, userId, frontSide: $"![Mnesios:{imageName}]");
        await CardHelper.CreateIdAsync(db, userId, backSide: $"![Mnesios:{imageName}]");

        using (var dbContext = new MemCheckDbContext(db))
            Assert.AreEqual(3, dbContext.ImagesInCards.Count());

        using (var dbContext = new MemCheckDbContext(db))
            await new DeleteCards(dbContext.AsCallContext()).RunAsync(new DeleteCards.Request(userId, new[] { cardId }));

        using (var dbContext = new MemCheckDbContext(db))
            Assert.AreEqual(2, dbContext.ImagesInCards.Count());
    }
    [TestMethod()]
    public async Task DeleteCardsContainingImages()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);

        var image1Name = RandomHelper.String();
        var image1Id = await ImageHelper.CreateAsync(db, userId, image1Name);

        var image2Name = RandomHelper.String();
        var image2Id = await ImageHelper.CreateAsync(db, userId, image2Name);

        var image3Name = RandomHelper.String();
        await ImageHelper.CreateAsync(db, userId, image3Name);

        var card1Id = await CardHelper.CreateIdAsync(db, userId, additionalInfo: $"![Mnesios:{image1Name}]");
        var card2Id = await CardHelper.CreateIdAsync(db, userId, frontSide: $"![Mnesios:{image2Name}]", additionalInfo: $"![Mnesios:{image3Name}]");
        await CardHelper.CreateIdAsync(db, userId, frontSide: $"![Mnesios:{image1Name}]");
        await CardHelper.CreateIdAsync(db, userId, backSide: $"![Mnesios:{image2Name}]");

        using (var dbContext = new MemCheckDbContext(db))
            Assert.AreEqual(5, dbContext.ImagesInCards.Count());

        using (var dbContext = new MemCheckDbContext(db))
            await new DeleteCards(dbContext.AsCallContext()).RunAsync(new DeleteCards.Request(userId, new[] { card1Id, card2Id }));

        using (var dbContext = new MemCheckDbContext(db))
        {
            Assert.AreEqual(2, dbContext.ImagesInCards.Count());
            Assert.AreEqual(1, dbContext.ImagesInCards.Count(imageInCard => imageInCard.ImageId == image1Id));
            Assert.AreEqual(1, dbContext.ImagesInCards.Count(imageInCard => imageInCard.ImageId == image2Id));
        }
    }
}

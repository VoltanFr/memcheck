using MemCheck.Application.Helpers;
using MemCheck.Application.QueryValidation;
using MemCheck.Basics;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards;

[TestClass()]
public class GetCardForEditTests
{
    [TestMethod()]
    public async Task UserNotLoggedIn()
    {
        using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetCardForEdit(dbContext.AsCallContext()).RunAsync(new GetCardForEdit.Request(Guid.Empty, Guid.Empty)));
    }
    [TestMethod()]
    public async Task UserDoesNotExist()
    {
        using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetCardForEdit(dbContext.AsCallContext()).RunAsync(new GetCardForEdit.Request(Guid.NewGuid(), Guid.Empty)));
    }
    [TestMethod()]
    public async Task CardDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<NonexistentCardException>(async () => await new GetCardForEdit(dbContext.AsCallContext()).RunAsync(new GetCardForEdit.Request(userId, Guid.NewGuid())));
    }
    [TestMethod()]
    public async Task FailIfUserCanNotView()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var language = await CardLanguageHelper.CreateAsync(db);
        var card = await CardHelper.CreateAsync(db, userId, language: language, userWithViewIds: userId.AsArray());
        var otherUserId = await UserHelper.CreateInDbAsync(db);
        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<UserNotAllowedToAccessCardException>(async () => await new GetCardForEdit(dbContext.AsCallContext()).RunAsync(new GetCardForEdit.Request(otherUserId, card.Id)));
    }
    [TestMethod()]
    public async Task CardWithPreviousVersion()
    {
        var db = DbHelper.GetEmptyTestDB();
        var language = await CardLanguageHelper.CreateAsync(db);

        var firstVersionCreator = await UserHelper.CreateUserInDbAsync(db);
        var firstVersionDate = RandomHelper.Date();
        var card = await CardHelper.CreateAsync(db, firstVersionCreator.Id, language: language, versionDate: firstVersionDate);

        var lastVersionCreator = await UserHelper.CreateUserInDbAsync(db);
        var lastVersionDate = RandomHelper.Date();
        var lastVersionDescription = RandomHelper.String();
        var lastVersionFrontSide = RandomHelper.String();
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateCard(dbContext.AsCallContext(), lastVersionDate).RunAsync(UpdateCardHelper.RequestForFrontSideChange(card, lastVersionFrontSide, versionCreator: lastVersionCreator.Id, versionDescription: lastVersionDescription));

        var otherUserId = await UserHelper.CreateInDbAsync(db);

        using (var dbContext = new MemCheckDbContext(db))
        {
            var loaded = await new GetCardForEdit(dbContext.AsCallContext()).RunAsync(new GetCardForEdit.Request(otherUserId, card.Id));

            Assert.AreEqual(firstVersionDate, loaded.FirstVersionUtcDate);
            Assert.AreEqual(lastVersionDate, loaded.LastVersionUtcDate);
            Assert.AreEqual(lastVersionCreator.UserName, loaded.LastVersionCreatorName);
            Assert.AreEqual(lastVersionDescription, loaded.LastVersionDescription);
            Assert.AreEqual(lastVersionFrontSide, loaded.FrontSide);
        }
    }
    [TestMethod()]
    public async Task CheckAllFields()
    {
        var db = DbHelper.GetEmptyTestDB();
        var languageName = RandomHelper.String();
        var language = await CardLanguageHelper.CreateAsync(db, languageName);
        var creator = await UserHelper.CreateUserInDbAsync(db); // This creates a deck for creator
        var creationDate = RandomHelper.Date();
        var frontSide = RandomHelper.String();
        var backSide = RandomHelper.String();
        var additionalInfo = RandomHelper.String();
        var references = RandomHelper.String();
        var tagName = RandomHelper.String();
        var tag = await TagHelper.CreateAsync(db, creator, tagName);
        var otherUser = await UserHelper.CreateUserInDbAsync(db); // This creates a deck for otherUser
        var versionDescription = RandomHelper.String();
        var card = await CardHelper.CreateAsync(db, creator.Id, language: language, versionDate: creationDate, frontSide: frontSide, backSide: backSide, additionalInfo: additionalInfo, references: references, tagIds: tag.AsArray(), userWithViewIds: new[] { creator.Id, otherUser.Id }, versionDescription: versionDescription);

        var deck = await DeckHelper.CreateAsync(db, otherUser.Id); // otherUser now has two decks
        await DeckHelper.AddCardAsync(db, deck, card.Id);

        using var dbContext = new MemCheckDbContext(db);
        var loaded = await new GetCardForEdit(dbContext.AsCallContext()).RunAsync(new GetCardForEdit.Request(creator.Id, card.Id));

        Assert.AreEqual(frontSide, loaded.FrontSide);
        Assert.AreEqual(backSide, loaded.BackSide);
        Assert.AreEqual(additionalInfo, loaded.AdditionalInfo);
        Assert.AreEqual(references, loaded.References);
        Assert.AreEqual(language, loaded.LanguageId);
        Assert.AreEqual(languageName, loaded.LanguageName);
        Assert.AreEqual(tag, loaded.Tags.Single().TagId);
        Assert.AreEqual(tagName, loaded.Tags.Single().TagName);
        Assert.AreEqual(2, loaded.UsersWithVisibility.Length);
        Assert.IsTrue(loaded.UsersWithVisibility.Count(u => u.UserId == creator.Id) == 1);
        Assert.AreEqual(creator.UserName, loaded.UsersWithVisibility.Single(u => u.UserId == creator.Id).UserName);
        Assert.IsTrue(loaded.UsersWithVisibility.Count(u => u.UserId == otherUser.Id) == 1);
        Assert.AreEqual(otherUser.UserName, loaded.UsersWithVisibility.Single(u => u.UserId == otherUser.Id).UserName);
        Assert.AreEqual(creationDate, loaded.FirstVersionUtcDate);
        Assert.AreEqual(creationDate, loaded.LastVersionUtcDate);
        Assert.AreEqual(creator.UserName, loaded.LastVersionCreatorName);
        Assert.AreEqual(versionDescription, loaded.LastVersionDescription);
        Assert.AreEqual(1, loaded.UsersOwningDeckIncluding.Length);
        Assert.IsTrue(loaded.UsersOwningDeckIncluding.Single() == otherUser.UserName);
        Assert.AreEqual(0, loaded.UserRating);
        Assert.AreEqual(0, loaded.AverageRating);
        Assert.AreEqual(0, loaded.CountOfUserRatings);
        Assert.AreEqual(1, loaded.PossibleTargetDecksForAdd.Length);
        Assert.IsNull(loaded.LatestDiscussionEntryCreationUtcDate);
    }
    [TestMethod()]
    public async Task NotInAuthorSingleDeck()
    {
        var db = DbHelper.GetEmptyTestDB();
        var languageName = RandomHelper.String();
        var language = await CardLanguageHelper.CreateAsync(db, languageName);
        var creatorId = await UserHelper.CreateInDbAsync(db);

        var card = await CardHelper.CreateAsync(db, creatorId, language: language);

        using (var deckFindDbContext = new MemCheckDbContext(db))
        {
            var initialDeck = deckFindDbContext.Decks.Single();
            await DeckHelper.AddCardAsync(db, initialDeck.Id, card.Id);
        }

        var deckName = RandomHelper.String();
        var deckId = await DeckHelper.CreateAsync(db, creatorId, deckName);

        using var dbContext = new MemCheckDbContext(db);
        var loaded = await new GetCardForEdit(dbContext.AsCallContext()).RunAsync(new GetCardForEdit.Request(creatorId, card.Id));

        Assert.AreEqual(1, loaded.PossibleTargetDecksForAdd.Length);
        Assert.AreEqual(deckId, loaded.PossibleTargetDecksForAdd[0].DeckId);
        Assert.AreEqual(deckName, loaded.PossibleTargetDecksForAdd[0].DeckName);
    }
    [TestMethod()]
    public async Task NotInAuthorMultipleDecks()
    {
        var db = DbHelper.GetEmptyTestDB();
        var languageName = RandomHelper.String();
        var language = await CardLanguageHelper.CreateAsync(db, languageName);
        var creatorId = await UserHelper.CreateInDbAsync(db); // This creates a deck for creator

        var cardId = await CardHelper.CreateIdAsync(db, creatorId, language: language);

        var deck1Name = RandomHelper.String();
        var deck1Id = await DeckHelper.CreateAsync(db, creatorId, deck1Name); // creator now has two decks

        var deck2Name = RandomHelper.String();
        var deck2Id = await DeckHelper.CreateAsync(db, creatorId, deck2Name); // creator now has three decks

        var deck3Id = await DeckHelper.CreateAsync(db, creatorId); // creator now has four decks

        using var dbContext = new MemCheckDbContext(db);
        var loaded = await new GetCardForEdit(dbContext.AsCallContext()).RunAsync(new GetCardForEdit.Request(creatorId, cardId));

        await DeckHelper.AddCardAsync(db, deck3Id, cardId);

        Assert.AreEqual(4, loaded.PossibleTargetDecksForAdd.Length);

        var deck1FromResult = loaded.PossibleTargetDecksForAdd.Single(deck => deck.DeckId == deck1Id);
        Assert.AreEqual(deck1Name, deck1FromResult.DeckName);

        var deck2FromResult = loaded.PossibleTargetDecksForAdd.Single(deck => deck.DeckId == deck2Id);
        Assert.AreEqual(deck2Name, deck2FromResult.DeckName);
    }
    [TestMethod()]
    public async Task NotInOtherUserSingleDeck()
    {
        var db = DbHelper.GetEmptyTestDB();
        var languageName = RandomHelper.String();
        var language = await CardLanguageHelper.CreateAsync(db, languageName);
        var creatorId = await UserHelper.CreateInDbAsync(db); // This creates a deck for creator
        var otherUserId = await UserHelper.CreateInDbAsync(db); // This creates a deck for otherUser
        await DeckHelper.CreateAsync(db, creatorId); // creator now has two decks

        await CardHelper.CreateAsync(db, creatorId, language: language);
        var card = await CardHelper.CreateAsync(db, creatorId, language: language);
        await CardHelper.CreateAsync(db, creatorId, language: language);

        var deckName = RandomHelper.String();
        var otherUserDeckId = await DeckHelper.CreateAsync(db, otherUserId, deckName); // otherUser now has two decks

        using var dbContext = new MemCheckDbContext(db);
        var loaded = await new GetCardForEdit(dbContext.AsCallContext()).RunAsync(new GetCardForEdit.Request(otherUserId, card.Id));

        Assert.AreEqual(2, loaded.PossibleTargetDecksForAdd.Length);
        Assert.IsTrue(loaded.PossibleTargetDecksForAdd.Any(deck => deck.DeckId == otherUserDeckId));
    }
    [TestMethod()]
    public async Task CheckLatestDiscussionEntryCreationUtcDate_NoEntry()
    {
        var db = DbHelper.GetEmptyTestDB();
        var creatorId = await UserHelper.CreateInDbAsync(db); // This creates a deck for creator
        var cardId = await CardHelper.CreateIdAsync(db, creatorId);

        using var dbContext = new MemCheckDbContext(db);
        var loaded = await new GetCardForEdit(dbContext.AsCallContext()).RunAsync(new GetCardForEdit.Request(creatorId, cardId));
        Assert.IsNull(loaded.LatestDiscussionEntryCreationUtcDate);
    }
    [TestMethod()]
    public async Task CheckLatestDiscussionEntryCreationUtcDate_OneEntry()
    {
        var db = DbHelper.GetEmptyTestDB();
        var creatorId = await UserHelper.CreateInDbAsync(db); // This creates a deck for creator
        var cardId = await CardHelper.CreateIdAsync(db, creatorId);

        var entryCreationDate = RandomHelper.Date();
        using (var dbContext = new MemCheckDbContext(db))
            await new AddEntryToCardDiscussion(dbContext.AsCallContext(), entryCreationDate).RunAsync(new AddEntryToCardDiscussion.Request(creatorId, cardId, RandomHelper.String()));

        using (var dbContext = new MemCheckDbContext(db))
        {
            var loaded = await new GetCardForEdit(dbContext.AsCallContext()).RunAsync(new GetCardForEdit.Request(creatorId, cardId));
            Assert.AreEqual(entryCreationDate, loaded.LatestDiscussionEntryCreationUtcDate);
        }
    }
    [TestMethod()]
    public async Task CheckLatestDiscussionEntryCreationUtcDate_MultipleEntries()
    {
        var db = DbHelper.GetEmptyTestDB();
        var creatorId = await UserHelper.CreateInDbAsync(db); // This creates a deck for creator
        var cardId = await CardHelper.CreateIdAsync(db, creatorId);

        using (var dbContext = new MemCheckDbContext(db))
            await new AddEntryToCardDiscussion(dbContext.AsCallContext()).RunAsync(new AddEntryToCardDiscussion.Request(creatorId, cardId, RandomHelper.String()));

        var lastEntryCreationDate = RandomHelper.Date();
        using (var dbContext = new MemCheckDbContext(db))
            await new AddEntryToCardDiscussion(dbContext.AsCallContext(), lastEntryCreationDate).RunAsync(new AddEntryToCardDiscussion.Request(creatorId, cardId, RandomHelper.String()));

        using (var dbContext = new MemCheckDbContext(db))
        {
            var loaded = await new GetCardForEdit(dbContext.AsCallContext()).RunAsync(new GetCardForEdit.Request(creatorId, cardId));
            Assert.AreEqual(lastEntryCreationDate, loaded.LatestDiscussionEntryCreationUtcDate);
        }
    }
}

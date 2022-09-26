using MemCheck.Application.Helpers;
using MemCheck.Application.QueryValidation;
using MemCheck.Basics;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards;

[TestClass()]
public class AddTagToCardsTests
{
    [TestMethod()]
    public async Task UserNotLoggedInMustFail()
    {
        var db = DbHelper.GetEmptyTestDB();
        var tagId = await TagHelper.CreateAsync(db);
        var cardCreatorId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, cardCreatorId);
        using var dbContext = new MemCheckDbContext(db);
        var e = await Assert.ThrowsExceptionAsync<NonexistentUserException>(async () => await new AddTagToCards(dbContext.AsCallContext()).RunAsync(new AddTagToCards.Request(RandomHelper.Guid(), tagId, cardId.AsArray())));
        Assert.AreEqual(QueryValidationHelper.ExceptionMesg_UserDoesNotExist, e.Message);
    }
    [TestMethod()]
    public async Task UserDoesNotExistMustFail()
    {
        var db = DbHelper.GetEmptyTestDB();
        var tagId = await TagHelper.CreateAsync(db);
        var cardCreatorId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, cardCreatorId);

        using var dbContext = new MemCheckDbContext(db);
        var e = await Assert.ThrowsExceptionAsync<NonexistentUserException>(async () => await new AddTagToCards(dbContext.AsCallContext()).RunAsync(new AddTagToCards.Request(RandomHelper.Guid(), tagId, cardId.AsArray())));
        Assert.AreEqual(QueryValidationHelper.ExceptionMesg_UserDoesNotExist, e.Message);
    }
    [TestMethod()]
    public async Task DeletedUserMustFail()
    {
        var db = DbHelper.GetEmptyTestDB();
        var tagId = await TagHelper.CreateAsync(db);
        var cardCreatorId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, cardCreatorId);

        await UserHelper.DeleteAsync(db, cardCreatorId);

        using var dbContext = new MemCheckDbContext(db);
        var e = await Assert.ThrowsExceptionAsync<NonexistentUserException>(async () => await new AddTagToCards(dbContext.AsCallContext()).RunAsync(new AddTagToCards.Request(cardCreatorId, tagId, cardId.AsArray())));
        Assert.AreEqual(QueryValidationHelper.ExceptionMesg_UserDoesNotExist, e.Message);
    }
    [TestMethod()]
    public async Task TagDoesNotExistMustFail()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId);

        using var dbContext = new MemCheckDbContext(db);
        var e = await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new AddTagToCards(dbContext.AsCallContext()).RunAsync(new AddTagToCards.Request(userId, RandomHelper.Guid(), cardId.AsArray())));
        Assert.AreEqual(QueryValidationHelper.ExceptionMesg_TagDoesNotExist, e.Message);
    }
    [TestMethod()]
    public async Task EmptyListOfCardsMustFail()
    {
        var db = DbHelper.GetEmptyTestDB();
        var tagId = await TagHelper.CreateAsync(db);
        var userId = await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        var e = await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new AddTagToCards(dbContext.AsCallContext()).RunAsync(new AddTagToCards.Request(userId, tagId, Array.Empty<Guid>())));
        Assert.AreEqual(AddTagToCards.Request.ExceptionMesg_NoCard, e.Message);
    }
    [TestMethod()]
    public async Task ACardDoesNotExistMustFail_OneCard()
    {
        var db = DbHelper.GetEmptyTestDB();
        var tagId = await TagHelper.CreateAsync(db);
        var cardCreatorId = await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        var e = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new AddTagToCards(dbContext.AsCallContext()).RunAsync(new AddTagToCards.Request(cardCreatorId, tagId, RandomHelper.Guid().AsArray())));
        Assert.AreEqual(QueryValidationHelper.ExceptionMesg_CardDoesNotExist, e.Message);
    }
    [TestMethod()]
    public async Task ACardDoesNotExistMustFail_MultipleCards()
    {
        var db = DbHelper.GetEmptyTestDB();
        var tagId = await TagHelper.CreateAsync(db);
        var cardCreatorId = await UserHelper.CreateInDbAsync(db);
        var card1Id = await CardHelper.CreateIdAsync(db, cardCreatorId);
        var card2Id = await CardHelper.CreateIdAsync(db, cardCreatorId);

        using var dbContext = new MemCheckDbContext(db);
        var e = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new AddTagToCards(dbContext.AsCallContext()).RunAsync(new AddTagToCards.Request(cardCreatorId, tagId, new[] { card1Id, RandomHelper.Guid(), card2Id })));
        Assert.AreEqual(QueryValidationHelper.ExceptionMesg_CardDoesNotExist, e.Message);
    }
    [TestMethod()]
    public async Task ACardNotViewableMustFail_OneCard()
    {
        var db = DbHelper.GetEmptyTestDB();
        var tagId = await TagHelper.CreateAsync(db);
        var cardCreatorId = await UserHelper.CreateInDbAsync(db);
        var card = await CardHelper.CreateAsync(db, cardCreatorId);
        await UpdateCardHelper.RunAsync(db, UpdateCardHelper.RequestForVisibilityChange(card, cardCreatorId.AsArray()));
        var otherUserId = await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        var e = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new AddTagToCards(dbContext.AsCallContext()).RunAsync(new AddTagToCards.Request(otherUserId, tagId, card.Id.AsArray())));
        Assert.AreEqual(CardVisibilityHelper.ExceptionMesg_UserNotAllowedToViewCard, e.Message);
    }
    [TestMethod()]
    public async Task ACardNotViewableMustFail_MultipleCards()
    {
        var db = DbHelper.GetEmptyTestDB();
        var tagId = await TagHelper.CreateAsync(db);
        var cardCreatorId = await UserHelper.CreateInDbAsync(db);
        var card = await CardHelper.CreateAsync(db, cardCreatorId);
        await UpdateCardHelper.RunAsync(db, UpdateCardHelper.RequestForVisibilityChange(card, cardCreatorId.AsArray()));
        var card2Id = await CardHelper.CreateIdAsync(db, cardCreatorId);
        var card3Id = await CardHelper.CreateIdAsync(db, cardCreatorId);
        var otherUserId = await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        var e = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new AddTagToCards(dbContext.AsCallContext()).RunAsync(new AddTagToCards.Request(otherUserId, tagId, new[] { card2Id, card.Id, card3Id })));
        Assert.AreEqual(CardVisibilityHelper.ExceptionMesg_UserNotAllowedToViewCard, e.Message);
    }
    [TestMethod()]
    public async Task CheckSuccessfullAdding()
    {
        var db = DbHelper.GetEmptyTestDB();

        var user1Id = await UserHelper.CreateInDbAsync(db);
        var user2Id = await UserHelper.CreateInDbAsync(db);

        var tag1Id = await TagHelper.CreateAsync(db);
        var tag2Id = await TagHelper.CreateAsync(db);
        var tagToAddId = await TagHelper.CreateAsync(db);

        var cards = new[] {
            await CardHelper.CreateIdAsync(db, user1Id),
            await CardHelper.CreateIdAsync(db, user1Id, tagIds: tag1Id.AsArray()),
            await CardHelper.CreateIdAsync(db, user1Id, tagIds: new[] { tag1Id, tag2Id }),
            await CardHelper.CreateIdAsync(db, user1Id, tagIds: tagToAddId.AsArray()),
            await CardHelper.CreateIdAsync(db, user1Id, tagIds: new[] { tag1Id, tagToAddId }),
            await CardHelper.CreateIdAsync(db, user1Id, tagIds: new[] { tag1Id, tag2Id, tagToAddId }),
            await CardHelper.CreateIdAsync(db, user2Id),
            await CardHelper.CreateIdAsync(db, user2Id, tagIds: tag1Id.AsArray()),
            await CardHelper.CreateIdAsync(db, user2Id, tagIds: new[] { tag1Id, tag2Id }),
            await CardHelper.CreateIdAsync(db, user2Id, tagIds: tagToAddId.AsArray()),
            await CardHelper.CreateIdAsync(db, user2Id, tagIds: new[] { tag1Id, tagToAddId }),
            await CardHelper.CreateIdAsync(db, user2Id, tagIds: new[] { tag1Id, tag2Id, tagToAddId }),
            };

        using (var dbContext = new MemCheckDbContext(db))
            await new AddTagToCards(dbContext.AsCallContext()).RunAsync(new AddTagToCards.Request(user1Id, tagToAddId, cards));

        using (var dbContext = new MemCheckDbContext(db))
        {
            var cardsFromDb = dbContext.Cards.AsNoTracking().Where(card => cards.Contains(card.Id)).Include(card => card.TagsInCards);
            Assert.AreEqual(cards.Length, cardsFromDb.Count());
            foreach (var cardFromDb in cardsFromDb)
                CollectionAssert.Contains(cardFromDb.TagsInCards.Select(tag => tag.TagId).ToArray(), tagToAddId);
        }
    }
    [TestMethod()]
    public async Task CardPreviousVersionCreated()
    {
        var db = DbHelper.GetEmptyTestDB();

        var user1Id = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, user1Id);
        var tag1Name = RandomHelper.String();
        var tag1Id = await TagHelper.CreateAsync(db, tag1Name);

        using (var dbContext = new MemCheckDbContext(db))
            await new AddTagToCards(dbContext.AsCallContext()).RunAsync(new AddTagToCards.Request(user1Id, tag1Id, cardId.AsArray()));

        using (var dbContext = new MemCheckDbContext(db))
        {
            var cardFromDb = dbContext.Cards.AsNoTracking().Include(card => card.TagsInCards).Include(card => card.VersionCreator).Single();
            CollectionAssert.Contains(cardFromDb.TagsInCards.Select(tag => tag.TagId).ToArray(), tag1Id);
            StringAssert.Contains(cardFromDb.VersionDescription, tag1Name);
            Assert.AreEqual(CardVersionType.Changes, cardFromDb.VersionType);
            Assert.AreEqual(user1Id, cardFromDb.VersionCreator.Id);

            var previousVersion = dbContext.CardPreviousVersions.Single();
            Assert.AreEqual(CardPreviousVersionType.Creation, previousVersion.VersionType);
        }

        var tag2Name = RandomHelper.String();
        var tag2Id = await TagHelper.CreateAsync(db, tag2Name);

        using (var dbContext = new MemCheckDbContext(db))
            await new AddTagToCards(dbContext.AsCallContext()).RunAsync(new AddTagToCards.Request(user1Id, tag2Id, cardId.AsArray()));

        using (var dbContext = new MemCheckDbContext(db))
        {
            var cardFromDb = dbContext.Cards.AsNoTracking().Include(card => card.TagsInCards).Single();
            var tagsInCard = cardFromDb.TagsInCards.Select(tag => tag.TagId).ToArray();
            CollectionAssert.Contains(tagsInCard, tag1Id);
            CollectionAssert.Contains(tagsInCard, tag2Id);
            StringAssert.Contains(cardFromDb.VersionDescription, tag2Name);
            Assert.AreEqual(CardVersionType.Changes, cardFromDb.VersionType);

            var previousVersions = dbContext.CardPreviousVersions.ToImmutableArray();
            Assert.AreEqual(2, previousVersions.Length);
            Assert.AreEqual(CardPreviousVersionType.Creation, previousVersions[0].VersionType);
            Assert.AreEqual(CardPreviousVersionType.Changes, previousVersions[1].VersionType);
            StringAssert.Contains(previousVersions[1].VersionDescription, tag1Name);
        }
    }
    [TestMethod()]
    public async Task AddingPersoTagToPublicCardMustFail()
    {
        var db = DbHelper.GetEmptyTestDB();

        var user1Id = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, user1Id);
        var persoTagId = await TagHelper.CreateAsync(db, Tag.Perso);

        var errorMesg = RandomHelper.String();
        var localizer = new TestLocalizer("PersoTagAllowedOnlyOnPrivateCards".PairedWith(errorMesg));
        using var dbContext = new MemCheckDbContext(db);
        var e = await Assert.ThrowsExceptionAsync<PersoTagAllowedOnlyOnPrivateCardsException>(async () => await new AddTagToCards(dbContext.AsCallContext(localizer)).RunAsync(new AddTagToCards.Request(user1Id, persoTagId, cardId.AsArray())));
        Assert.AreEqual(errorMesg, e.Message);
    }
    [TestMethod()]
    public async Task AddingPersoTagToMultiUserCardMustFail()
    {
        var db = DbHelper.GetEmptyTestDB();

        var user1Id = await UserHelper.CreateInDbAsync(db);
        var card = await CardHelper.CreateAsync(db, user1Id);
        var persoTagId = await TagHelper.CreateAsync(db, Tag.Perso);
        var user2Id = await UserHelper.CreateInDbAsync(db);
        await UpdateCardHelper.RunAsync(db, UpdateCardHelper.RequestForVisibilityChange(card, new[] { user1Id, user2Id }));

        var errorMesg = RandomHelper.String();
        var localizer = new TestLocalizer("PersoTagAllowedOnlyOnPrivateCards".PairedWith(errorMesg));
        using var dbContext = new MemCheckDbContext(db);
        var e = await Assert.ThrowsExceptionAsync<PersoTagAllowedOnlyOnPrivateCardsException>(async () => await new AddTagToCards(dbContext.AsCallContext(localizer)).RunAsync(new AddTagToCards.Request(user1Id, persoTagId, card.Id.AsArray())));
        Assert.AreEqual(errorMesg, e.Message);
    }
    [TestMethod()]
    public async Task AddingPersoTagToPrivateCardMustSucceed()
    {
        var db = DbHelper.GetEmptyTestDB();

        var user1Id = await UserHelper.CreateInDbAsync(db);
        var card = await CardHelper.CreateAsync(db, user1Id);
        var persoTagId = await TagHelper.CreateAsync(db, Tag.Perso);
        await UpdateCardHelper.RunAsync(db, UpdateCardHelper.RequestForVisibilityChange(card, user1Id.AsArray()));

        using (var dbContext = new MemCheckDbContext(db))
            await new AddTagToCards(dbContext.AsCallContext()).RunAsync(new AddTagToCards.Request(user1Id, persoTagId, card.Id.AsArray()));

        using (var dbContext = new MemCheckDbContext(db))
        {
            var cardFromDb = dbContext.Cards.AsNoTracking().Include(card => card.TagsInCards).Single();
            CollectionAssert.Contains(cardFromDb.TagsInCards.Select(tag => tag.TagId).ToArray(), persoTagId);
        }
    }
}

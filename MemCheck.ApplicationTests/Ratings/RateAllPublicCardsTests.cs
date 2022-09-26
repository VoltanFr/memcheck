using MemCheck.Application.Cards;
using MemCheck.Application.Helpers;
using MemCheck.Application.QueryValidation;
using MemCheck.Basics;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Ratings;

[TestClass()]
public class RateAllPublicCardsTests
{
    [TestMethod()]
    public async Task NoUser()
    {
        var db = DbHelper.GetEmptyTestDB();
        await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<NonexistentUserException>(async () => await new RateAllPublicCards(dbContext.AsCallContext()).RunAsync(new RateAllPublicCards.Request(Guid.Empty)));
    }
    [TestMethod()]
    public async Task UserDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<NonexistentUserException>(async () => await new RateAllPublicCards(dbContext.AsCallContext()).RunAsync(new RateAllPublicCards.Request(Guid.NewGuid())));
    }
    [TestMethod()]
    public async Task SingleCardWithoutPreviousRating()
    {
        var db = DbHelper.GetEmptyTestDB();
        var cardCreatorId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, cardCreatorId);

        var botUserId = await UserHelper.CreateInDbAsync(db);

        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new RateAllPublicCards(dbContext.AsCallContext()).RunAsync(new RateAllPublicCards.Request(botUserId));
            Assert.AreEqual(1, result.PublicCardCount);
            Assert.AreEqual(1, result.ChangedRatingCount);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            Assert.AreEqual(1, dbContext.UserCardRatings.Count());
            var ratingFromDb = dbContext.UserCardRatings.Single();
            Assert.AreEqual(botUserId, ratingFromDb.UserId);
            Assert.AreEqual(cardId, ratingFromDb.CardId);
            Assert.AreEqual(5, ratingFromDb.Rating);
        }
    }
    [TestMethod()]
    public async Task SingleCardWithPreviousRatingNotToUpdate()
    {
        var db = DbHelper.GetEmptyTestDB();
        var cardCreatorId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, cardCreatorId);

        var botUserId = await UserHelper.CreateInDbAsync(db);

        using (var dbContext = new MemCheckDbContext(db))
            await new SetCardRating(dbContext.AsCallContext()).RunAsync(new SetCardRating.Request(botUserId, cardId, 5));

        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new RateAllPublicCards(dbContext.AsCallContext()).RunAsync(new RateAllPublicCards.Request(botUserId));
            Assert.AreEqual(1, result.PublicCardCount);
            Assert.AreEqual(0, result.ChangedRatingCount);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            Assert.AreEqual(1, dbContext.UserCardRatings.Count());
            var ratingFromDb = dbContext.UserCardRatings.Single();
            Assert.AreEqual(botUserId, ratingFromDb.UserId);
            Assert.AreEqual(cardId, ratingFromDb.CardId);
            Assert.AreEqual(5, ratingFromDb.Rating);
        }
    }
    [TestMethod()]
    public async Task SingleCardWithPreviousRatingToUpdate()
    {
        var db = DbHelper.GetEmptyTestDB();
        var cardCreatorId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, cardCreatorId);

        var botUserId = await UserHelper.CreateInDbAsync(db);

        using (var dbContext = new MemCheckDbContext(db))
            await new SetCardRating(dbContext.AsCallContext()).RunAsync(new SetCardRating.Request(botUserId, cardId, 4));

        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new RateAllPublicCards(dbContext.AsCallContext()).RunAsync(new RateAllPublicCards.Request(botUserId));
            Assert.AreEqual(1, result.PublicCardCount);
            Assert.AreEqual(1, result.ChangedRatingCount);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            Assert.AreEqual(1, dbContext.UserCardRatings.Count());
            var ratingFromDb = dbContext.UserCardRatings.Single();
            Assert.AreEqual(botUserId, ratingFromDb.UserId);
            Assert.AreEqual(cardId, ratingFromDb.CardId);
            Assert.AreEqual(5, ratingFromDb.Rating);
        }
    }
    [TestMethod()]
    public async Task ComplexCase()
    {
        var db = DbHelper.GetEmptyTestDB();

        var cardCreatorId = await UserHelper.CreateInDbAsync(db);
        var otherUserId = await UserHelper.CreateInDbAsync(db);
        var card1Id = await CardHelper.CreateIdAsync(db, cardCreatorId, userWithViewIds: new[] { cardCreatorId, otherUserId }, additionalInfo: ""); //Limited visibility, must not be rated
        var card2Id = await CardHelper.CreateIdAsync(db, cardCreatorId, userWithViewIds: cardCreatorId.AsArray(), references: ""); //Private, must not be rated
        var card3 = await CardHelper.CreateAsync(db, cardCreatorId, additionalInfo: ""); //Initial rating 3, we will set additional info and rating must become 5
        var card4 = await CardHelper.CreateAsync(db, cardCreatorId, additionalInfo: "", references: ""); //Initial rating 3, we will set additional info and rating must become 4
        var card5 = await CardHelper.CreateAsync(db, cardCreatorId, references: ""); //Initial rating 4, we will set references and rating must become 5
        var card6Id = await CardHelper.CreateIdAsync(db, cardCreatorId, references: ""); //Initial rating 4, must not change
        var card7Id = await CardHelper.CreateIdAsync(db, cardCreatorId); //Initial rating 5, must not change
        var card8 = await CardHelper.CreateAsync(db, cardCreatorId); //Initial rating 5, we will remove additional info and rating must become 3

        var botUserId = await UserHelper.CreateInDbAsync(db);

        using (var dbContext = new MemCheckDbContext(db))
        {
            var setCardRating = new SetCardRating(dbContext.AsCallContext());
            await setCardRating.RunAsync(new SetCardRating.Request(botUserId, card4.Id, 2));
            await setCardRating.RunAsync(new SetCardRating.Request(botUserId, card5.Id, 3));
            await setCardRating.RunAsync(new SetCardRating.Request(botUserId, card7Id, 4));
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new RateAllPublicCards(dbContext.AsCallContext()).RunAsync(new RateAllPublicCards.Request(botUserId));
            Assert.AreEqual(6, result.PublicCardCount);
            Assert.AreEqual(6, result.ChangedRatingCount);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            Assert.AreEqual(6, dbContext.UserCardRatings.Count());
            Assert.AreEqual(3, dbContext.UserCardRatings.Single(userCardRating => userCardRating.CardId == card3.Id).Rating);
            Assert.AreEqual(3, dbContext.UserCardRatings.Single(userCardRating => userCardRating.CardId == card4.Id).Rating);
            Assert.AreEqual(4, dbContext.UserCardRatings.Single(userCardRating => userCardRating.CardId == card5.Id).Rating);
            Assert.AreEqual(4, dbContext.UserCardRatings.Single(userCardRating => userCardRating.CardId == card6Id).Rating);
            Assert.AreEqual(5, dbContext.UserCardRatings.Single(userCardRating => userCardRating.CardId == card7Id).Rating);
            Assert.AreEqual(5, dbContext.UserCardRatings.Single(userCardRating => userCardRating.CardId == card8.Id).Rating);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            await new UpdateCard(dbContext.AsCallContext()).RunAsync(UpdateCardHelper.RequestForAdditionalInfoChange(card3, RandomHelper.String()));
            await new UpdateCard(dbContext.AsCallContext()).RunAsync(UpdateCardHelper.RequestForAdditionalInfoChange(card4, RandomHelper.String()));
            await new UpdateCard(dbContext.AsCallContext()).RunAsync(UpdateCardHelper.RequestForReferencesChange(card5, RandomHelper.String()));
            await new UpdateCard(dbContext.AsCallContext()).RunAsync(UpdateCardHelper.RequestForAdditionalInfoChange(card8, ""));
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new RateAllPublicCards(dbContext.AsCallContext()).RunAsync(new RateAllPublicCards.Request(botUserId));
            Assert.AreEqual(6, result.PublicCardCount);
            Assert.AreEqual(4, result.ChangedRatingCount);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            Assert.AreEqual(6, dbContext.UserCardRatings.Count());
            Assert.AreEqual(5, dbContext.UserCardRatings.Single(userCardRating => userCardRating.CardId == card3.Id).Rating);
            Assert.AreEqual(4, dbContext.UserCardRatings.Single(userCardRating => userCardRating.CardId == card4.Id).Rating);
            Assert.AreEqual(5, dbContext.UserCardRatings.Single(userCardRating => userCardRating.CardId == card5.Id).Rating);
            Assert.AreEqual(4, dbContext.UserCardRatings.Single(userCardRating => userCardRating.CardId == card6Id).Rating);
            Assert.AreEqual(5, dbContext.UserCardRatings.Single(userCardRating => userCardRating.CardId == card7Id).Rating);
            Assert.AreEqual(3, dbContext.UserCardRatings.Single(userCardRating => userCardRating.CardId == card8.Id).Rating);
        }
    }
}

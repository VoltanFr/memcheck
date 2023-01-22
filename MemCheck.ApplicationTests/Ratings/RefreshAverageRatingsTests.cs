using MemCheck.Application.Helpers;
using MemCheck.Basics;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Ratings;

[TestClass()]
public class RefreshAverageRatingsTests
{
    [TestMethod()]
    public async Task NoCard()
    {
        var db = DbHelper.GetEmptyTestDB();

        using var dbContext = new MemCheckDbContext(db);
        var result = await new RefreshAverageRatings(dbContext.AsCallContext()).RunAsync(new RefreshAverageRatings.Request());
        Assert.AreEqual(0, result.TotalCardCountInDb);
        Assert.AreEqual(0, result.ChangedAverageRatingCount);
    }
    [TestMethod()]
    public async Task OneCardWithoutRating()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        await CardHelper.CreateAsync(db, userId);

        using var dbContext = new MemCheckDbContext(db);
        var result = await new RefreshAverageRatings(dbContext.AsCallContext()).RunAsync(new RefreshAverageRatings.Request());
        Assert.AreEqual(1, result.TotalCardCountInDb);
        Assert.AreEqual(0, result.ChangedAverageRatingCount);
    }
    [TestMethod()]
    public async Task OneCardWithAverageRatingUpToDate()
    {
        var db = DbHelper.GetEmptyTestDB();
        var cardCreatorId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, cardCreatorId);

        var ratingByCreator = RandomHelper.Rating();
        await RatingHelper.RecordForUserAsync(db, cardCreatorId, cardId, ratingByCreator);

        var otherUserId = await UserHelper.CreateInDbAsync(db);
        var ratingByOtherUser = RandomHelper.Rating(ratingByCreator);
        await RatingHelper.RecordForUserAsync(db, otherUserId, cardId, ratingByOtherUser);

        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new RefreshAverageRatings(dbContext.AsCallContext()).RunAsync(new RefreshAverageRatings.Request());
            Assert.AreEqual(1, result.TotalCardCountInDb);
            Assert.AreEqual(0, result.ChangedAverageRatingCount);
        }

        using (var dbContext = new MemCheckDbContext(db))
            Assert.AreEqual((double)(ratingByCreator + ratingByOtherUser) / 2, dbContext.Cards.Single().AverageRating);
    }
    [TestMethod()]
    public async Task OneCardWithAverageRatingNotUpToDate()
    {
        var db = DbHelper.GetEmptyTestDB();
        var cardCreatorId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, cardCreatorId);

        var ratingByCreator = RandomHelper.Rating();
        await RatingHelper.RecordForUserAsync(db, cardCreatorId, cardId, ratingByCreator);

        var otherUserId = await UserHelper.CreateInDbAsync(db);
        var ratingByOtherUser = RandomHelper.Rating(ratingByCreator);
        using (var dbContext = new MemCheckDbContext(db))
        {
            var userCardRating = new Domain.UserCardRating
            {
                UserId = otherUserId,
                CardId = cardId,
                Rating = ratingByOtherUser
            };
            dbContext.UserCardRatings.Add(userCardRating);
            dbContext.SaveChanges();
        };

        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new RefreshAverageRatings(dbContext.AsCallContext()).RunAsync(new RefreshAverageRatings.Request());
            Assert.AreEqual(1, result.TotalCardCountInDb);
            Assert.AreEqual(1, result.ChangedAverageRatingCount);
        }

        using (var dbContext = new MemCheckDbContext(db))
            Assert.AreEqual((double)(ratingByCreator + ratingByOtherUser) / 2, dbContext.Cards.Single().AverageRating);
    }
    [TestMethod()]
    public async Task ComplexCase()
    {
        // One user who creates cards, named cardCreator
        // Some users who give ratings to cards: ratingUserIds
        // cardCreator creates some cards
        // For each card, we record some ratings without refreshing the cards's average rating, and with a probability of 0.5 we set a rating by cardCreator WITH refreshing the card's average rating
        // Then we launch a refresh and check the results

        var db = DbHelper.GetEmptyTestDB();
        var cardCreatorId = await UserHelper.CreateInDbAsync(db);
        var ratingUserIds = await RandomHelper.Int(20, 30).TimesAsync(() => UserHelper.CreateInDbAsync(db));

        var cardRatings = new Dictionary<Guid, double>();
        var cardCount = RandomHelper.Int(50, 100);
        var countOfCardsWithCorrectAverageRating = 0;

        await cardCount.TimesAsync(async () =>
                {
                    var cardId = await CardHelper.CreateIdAsync(db, cardCreatorId);
                    var ratings = new List<int>();

                    using (var dbContext = new MemCheckDbContext(db))
                    {
                        // Add some ratings without refreshing the card's average rating
                        for (var ratingIndex = 0; ratingIndex < RandomHelper.Int(1, ratingUserIds.Length + 1); ratingIndex++)
                        {
                            var rating = RandomHelper.Rating();
                            dbContext.UserCardRatings.Add(new Domain.UserCardRating { UserId = ratingUserIds[ratingIndex], CardId = cardId, Rating = rating });
                            ratings.Add(rating);
                        }
                        dbContext.SaveChanges();
                    }

                    if (RandomHelper.Bool())
                    {
                        // Add a rating by cardCreator and refresh the card's average rating
                        var creatorRating = RandomHelper.Rating();
                        await RatingHelper.RecordForUserAsync(db, cardCreatorId, cardId, creatorRating); // Updates the average rating
                        ratings.Add(creatorRating);
                        countOfCardsWithCorrectAverageRating++;
                    }

                    cardRatings.Add(cardId, ratings.Average());
                });

        await CardHelper.CreateIdAsync(db, cardCreatorId); // Ensure there is at least a card without rating

        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new RefreshAverageRatings(dbContext.AsCallContext()).RunAsync(new RefreshAverageRatings.Request());
            Assert.AreEqual(cardCount + 1, result.TotalCardCountInDb);
            Assert.AreEqual(cardRatings.Count - countOfCardsWithCorrectAverageRating, result.ChangedAverageRatingCount);
        }

        using (var dbContext = new MemCheckDbContext(db))
            foreach (var cardId in cardRatings.Keys)
            {
                var cardFromDb = dbContext.Cards.Single(card => card.Id == cardId);
                Assert.AreEqual(cardRatings[cardId], cardFromDb.AverageRating);
            }
    }
}

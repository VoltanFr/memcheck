using MemCheck.Application.Helpers;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        var db = DbHelper.GetEmptyTestDB();
        var cardCreatorId = await UserHelper.CreateInDbAsync(db);
        var ratingUserIds = Enumerable.Range(0, 20).Select(i => UserHelper.CreateInDbAsync(db).Result).ToImmutableArray();

        var cardRatings = new Dictionary<Guid, double>();

        for (var cardIndex = 0; cardIndex < 50; cardIndex++)
        {
            var cardId = await CardHelper.CreateIdAsync(db, cardCreatorId);
            var ratings = new List<int>();
            for (var ratingIndex = 0; ratingIndex < RandomHelper.Int(0, 19); ratingIndex++)
            {
                var rating = RandomHelper.Rating();
                if (ratingIndex != 0 && RandomHelper.Bool())
                {
                    var ratingByCreator = RandomHelper.Rating();
                    await RatingHelper.RecordForUserAsync(db, ratingUserIds[ratingIndex], cardId, rating);
                }
                else
                {
                    using (var dbContext = new MemCheckDbContext(db))
                    {
                        var userCardRating = new Domain.UserCardRating { UserId = ratingUserIds[ratingIndex], CardId = cardId, Rating = rating };
                        dbContext.UserCardRatings.Add(userCardRating);
                        dbContext.SaveChanges();
                    };
                }
                ratings.Add(rating);
            }
            cardRatings.Add(cardId, ratings.Any() ? ratings.Average() : 0);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new RefreshAverageRatings(dbContext.AsCallContext()).RunAsync(new RefreshAverageRatings.Request());
            Assert.AreEqual(50, result.TotalCardCountInDb);
            Assert.AreEqual(cardRatings.Count(cardRating => cardRating.Value != 0), result.ChangedAverageRatingCount);
        }

        using (var dbContext = new MemCheckDbContext(db))
            foreach (var cardId in cardRatings.Keys)
            {
                var cardFromDb = dbContext.Cards.Single(card => card.Id == cardId);
                Assert.AreEqual(cardRatings[cardId], cardFromDb.AverageRating);
            }
    }
}

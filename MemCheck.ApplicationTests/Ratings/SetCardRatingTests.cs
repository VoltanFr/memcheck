using MemCheck.Application.Helpers;
using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Ratings;

[TestClass()]
public class SetCardRatingTests
{
    #region Private methods
    private static async Task AssertUserRatingAsync(DbContextOptions<MemCheckDbContext> db, Guid userId, Guid cardId, int expectedUserRating)
    {
        using var dbContext = new MemCheckDbContext(db);
        if (expectedUserRating == 0)
            Assert.IsFalse(await dbContext.UserCardRatings.AnyAsync(r => r.CardId == cardId && r.UserId == userId));
        else
        {
            var rating = await dbContext.UserCardRatings.SingleAsync(r => r.CardId == cardId && r.UserId == userId);
            Assert.AreEqual(expectedUserRating, rating.Rating);
        }
    }
    private static async Task AssertAverageRatingAsync(DbContextOptions<MemCheckDbContext> db, Guid cardId, int expectedCount, double expectedAverage)
    {
        using var dbContext = new MemCheckDbContext(db);
        var loadedCard = await dbContext.Cards.SingleAsync(card => card.Id == cardId);
        Assert.AreEqual(expectedCount, loadedCard.RatingCount);
        Assert.AreEqual(expectedAverage, loadedCard.AverageRating);
    }
    #endregion
    [TestMethod()]
    public async Task UserNotLoggedIn()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var languageId = await CardLanguageHelper.CreateAsync(db);
        var card = await CardHelper.CreateIdAsync(db, user, language: languageId);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new SetCardRating(dbContext.AsCallContext()).RunAsync(new SetCardRating.Request(Guid.Empty, card, RandomHelper.Rating())));
    }
    [TestMethod()]
    public async Task UserDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var languageId = await CardLanguageHelper.CreateAsync(db);
        var card = await CardHelper.CreateIdAsync(db, user, language: languageId);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new SetCardRating(dbContext.AsCallContext()).RunAsync(new SetCardRating.Request(Guid.NewGuid(), card, RandomHelper.Rating())));
    }
    [TestMethod()]
    public async Task TooSmall()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var languageId = await CardLanguageHelper.CreateAsync(db);
        var card = await CardHelper.CreateIdAsync(db, user, language: languageId);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new SetCardRating(dbContext.AsCallContext()).RunAsync(new SetCardRating.Request(user, card, 0)));
    }
    [TestMethod()]
    public async Task TooBig()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var languageId = await CardLanguageHelper.CreateAsync(db);
        var card = await CardHelper.CreateIdAsync(db, user, language: languageId);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new SetCardRating(dbContext.AsCallContext()).RunAsync(new SetCardRating.Request(user, card, 6)));
    }
    [TestMethod()]
    public async Task SingleUser_NoPreviousValue()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var languageId = await CardLanguageHelper.CreateAsync(db);
        var card = await CardHelper.CreateIdAsync(db, user, language: languageId);
        var rating = RandomHelper.Rating();

        using (var dbContext = new MemCheckDbContext(db))
            await new SetCardRating(dbContext.AsCallContext()).RunAsync(new SetCardRating.Request(user, card, rating));

        await AssertUserRatingAsync(db, user, card, rating);
        await AssertAverageRatingAsync(db, card, 1, rating);
    }
    [TestMethod()]
    public async Task SingleUser_PreviousValue()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var languageId = await CardLanguageHelper.CreateAsync(db);
        var card = await CardHelper.CreateIdAsync(db, user, language: languageId);

        for (int i = 0; i < 5; i++)
        {
            var rating = RandomHelper.Rating();
            using (var dbContext = new MemCheckDbContext(db))
                await new SetCardRating(dbContext.AsCallContext()).RunAsync(new SetCardRating.Request(user, card, rating));
            await AssertUserRatingAsync(db, user, card, rating);
            await AssertAverageRatingAsync(db, card, 1, rating);
        }
    }
    [TestMethod()]
    public async Task Complex()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user1 = await UserHelper.CreateInDbAsync(db);
        var user2 = await UserHelper.CreateInDbAsync(db);
        var user3 = await UserHelper.CreateInDbAsync(db);
        var languageId = await CardLanguageHelper.CreateAsync(db);
        var card1 = await CardHelper.CreateIdAsync(db, user1, language: languageId);
        var card2 = await CardHelper.CreateIdAsync(db, user2, language: languageId);

        using (var dbContext = new MemCheckDbContext(db))
            await new SetCardRating(dbContext.AsCallContext()).RunAsync(new SetCardRating.Request(user1, card1, 1));
        await AssertAverageRatingAsync(db, card1, 1, 1);
        await AssertAverageRatingAsync(db, card2, 0, 0);
        await AssertUserRatingAsync(db, user1, card1, 1);
        await AssertUserRatingAsync(db, user2, card1, 0);
        await AssertUserRatingAsync(db, user3, card1, 0);
        await AssertUserRatingAsync(db, user1, card2, 0);
        await AssertUserRatingAsync(db, user2, card2, 0);
        await AssertUserRatingAsync(db, user3, card2, 0);

        using (var dbContext = new MemCheckDbContext(db))
            await new SetCardRating(dbContext.AsCallContext()).RunAsync(new SetCardRating.Request(user2, card1, 2));
        await AssertAverageRatingAsync(db, card1, 2, 1.5);
        await AssertAverageRatingAsync(db, card2, 0, 0);
        await AssertUserRatingAsync(db, user1, card1, 1);
        await AssertUserRatingAsync(db, user2, card1, 2);
        await AssertUserRatingAsync(db, user3, card1, 0);
        await AssertUserRatingAsync(db, user1, card2, 0);
        await AssertUserRatingAsync(db, user2, card2, 0);
        await AssertUserRatingAsync(db, user3, card2, 0);

        using (var dbContext = new MemCheckDbContext(db))
            await new SetCardRating(dbContext.AsCallContext()).RunAsync(new SetCardRating.Request(user3, card1, 3));
        await AssertAverageRatingAsync(db, card1, 3, 2);
        await AssertAverageRatingAsync(db, card2, 0, 0);
        await AssertUserRatingAsync(db, user1, card1, 1);
        await AssertUserRatingAsync(db, user2, card1, 2);
        await AssertUserRatingAsync(db, user3, card1, 3);
        await AssertUserRatingAsync(db, user1, card2, 0);
        await AssertUserRatingAsync(db, user2, card2, 0);
        await AssertUserRatingAsync(db, user3, card2, 0);

        using (var dbContext = new MemCheckDbContext(db))
            await new SetCardRating(dbContext.AsCallContext()).RunAsync(new SetCardRating.Request(user1, card2, 1));
        await AssertAverageRatingAsync(db, card1, 3, 2);
        await AssertAverageRatingAsync(db, card2, 1, 1);
        await AssertUserRatingAsync(db, user1, card1, 1);
        await AssertUserRatingAsync(db, user2, card1, 2);
        await AssertUserRatingAsync(db, user3, card1, 3);
        await AssertUserRatingAsync(db, user1, card2, 1);
        await AssertUserRatingAsync(db, user2, card2, 0);
        await AssertUserRatingAsync(db, user3, card2, 0);

        using (var dbContext = new MemCheckDbContext(db))
            await new SetCardRating(dbContext.AsCallContext()).RunAsync(new SetCardRating.Request(user1, card1, 4));
        await AssertAverageRatingAsync(db, card1, 3, 3);
        await AssertAverageRatingAsync(db, card2, 1, 1);
        await AssertUserRatingAsync(db, user1, card1, 4);
        await AssertUserRatingAsync(db, user2, card1, 2);
        await AssertUserRatingAsync(db, user3, card1, 3);
        await AssertUserRatingAsync(db, user1, card2, 1);
        await AssertUserRatingAsync(db, user2, card2, 0);
        await AssertUserRatingAsync(db, user3, card2, 0);
    }
}

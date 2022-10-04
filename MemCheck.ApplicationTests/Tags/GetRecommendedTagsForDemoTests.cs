using MemCheck.Application.Helpers;
using MemCheck.Basics;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Tags;

[TestClass()]
public class GetRecommendedTagsForDemoTests
{
    [TestMethod()]
    public async Task EmptyDb()
    {
        using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
        var result = await new GetRecommendedTagsForDemo(dbContext.AsCallContext()).RunAsync(new GetRecommendedTagsForDemo.Request(100, 3));
        Assert.IsFalse(result.Tags.Any());
    }
    [TestMethod()]
    public async Task DbHasOneTagWithNotEnoughCards()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var tagId = await TagHelper.CreateAsync(db);
        var cardIds = await 9.TimesAsync(async () => await CardHelper.CreateIdAsync(db, userId, tagIds: tagId.AsArray()));
        foreach (var cardId in cardIds)
            await RatingHelper.RecordForUserAsync(db, userId, cardId, RandomHelper.Int(3, 5));

        using var dbContext = new MemCheckDbContext(db);
        var result = await new GetRecommendedTagsForDemo(dbContext.AsCallContext()).RunAsync(new GetRecommendedTagsForDemo.Request(10, 3));
        Assert.IsFalse(result.Tags.Any());
    }
    [TestMethod()]
    public async Task DbHasOneTagWithTooLowRating()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var tagId = await TagHelper.CreateAsync(db);
        var cardIds = await 10.TimesAsync(async () => await CardHelper.CreateIdAsync(db, userId, tagIds: tagId.AsArray()));
        cardIds.ForEach(async cardId => await RatingHelper.RecordForUserAsync(db, userId, cardId, RandomHelper.Int(1, 3)));

        using var dbContext = new MemCheckDbContext(db);
        var result = await new GetRecommendedTagsForDemo(dbContext.AsCallContext()).RunAsync(new GetRecommendedTagsForDemo.Request(10, 4));
        Assert.IsFalse(result.Tags.Any());
    }
    [TestMethod()]
    public async Task DbHasOneTagToBeSelected()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var tagId = await TagHelper.CreateAsync(db);
        var cardIds = await 10.TimesAsync(async () => await CardHelper.CreateIdAsync(db, userId, tagIds: tagId.AsArray()));
        cardIds.ForEach(async cardId => await RatingHelper.RecordForUserAsync(db, userId, cardId, RandomHelper.Int(4, 5)));
        await TagHelper.RefreshAllAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        var result = await new GetRecommendedTagsForDemo(dbContext.AsCallContext()).RunAsync(new GetRecommendedTagsForDemo.Request(10, 4));
        Assert.AreEqual(1, result.Tags.Length);
    }
}

using MemCheck.Application.Helpers;
using MemCheck.Basics;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Tags;

[TestClass()]
public class RefreshTagStatsTests
{
    [TestMethod()]
    public async Task NoTag()
    {
        using var db = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
        var result = await new RefreshTagStats(db.AsCallContext()).RunAsync(new RefreshTagStats.Request());
        Assert.AreEqual(0, result.Tags.Length);
    }
    [TestMethod()]
    public async Task SingleTag_NoCard()
    {
        var db = DbHelper.GetEmptyTestDB();
        var name = RandomHelper.String();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var tag = await TagHelper.CreateAsync(db, user, name: name);

        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new RefreshTagStats(dbContext.AsCallContext()).RunAsync(new RefreshTagStats.Request());
            Assert.AreEqual(1, result.Tags.Length);
            Assert.AreEqual(tag, result.Tags.Single().TagId);
            Assert.AreEqual(name, result.Tags.Single().TagName);
            Assert.AreEqual(0, result.Tags.Single().CardCountBeforeRun);
            Assert.AreEqual(0, result.Tags.Single().AverageRatingBeforeRun);
            Assert.AreEqual(0, result.Tags.Single().CardCountAfterRun);
            Assert.AreEqual(0, result.Tags.Single().AverageRatingAfterRun);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var tagFromDb = await dbContext.Tags.SingleAsync();
            Assert.AreEqual(0, tagFromDb.AverageRatingOfPublicCards);
            Assert.AreEqual(0, tagFromDb.CountOfPublicCards);
        }
    }
    [TestMethod()]
    public async Task SingleTag_OnePrivateCardWithTagWithoutRating()
    {
        var db = DbHelper.GetEmptyTestDB();
        var name = RandomHelper.String();
        var cardCreator = await UserHelper.CreateUserInDbAsync(db);
        var tagId = await TagHelper.CreateAsync(db, cardCreator, name: name);
        await CardHelper.CreateAsync(db, cardCreator.Id, tagIds: tagId.AsArray(), userWithViewIds: cardCreator.Id.AsArray());

        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new RefreshTagStats(dbContext.AsCallContext()).RunAsync(new RefreshTagStats.Request());
            Assert.AreEqual(1, result.Tags.Length);
            Assert.AreEqual(0, result.Tags.Single().CardCountBeforeRun);
            Assert.AreEqual(0, result.Tags.Single().AverageRatingBeforeRun);
            Assert.AreEqual(0, result.Tags.Single().CardCountAfterRun);
            Assert.AreEqual(0, result.Tags.Single().AverageRatingAfterRun);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var tagFromDb = await dbContext.Tags.SingleAsync();
            Assert.AreEqual(0, tagFromDb.AverageRatingOfPublicCards);
            Assert.AreEqual(0, tagFromDb.CountOfPublicCards);
        }
    }
    [TestMethod()]
    public async Task SingleTag_OnePrivateCardWithTagWithRating()
    {
        var db = DbHelper.GetEmptyTestDB();
        var name = RandomHelper.String();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var tagId = await TagHelper.CreateAsync(db, user, name: name);
        var card = await CardHelper.CreateAsync(db, user, tagIds: tagId.AsArray(), usersWithView: user.AsArray());

        await RatingHelper.RecordForUserAsync(db, user, card.Id, RandomHelper.Rating());

        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new RefreshTagStats(dbContext.AsCallContext()).RunAsync(new RefreshTagStats.Request());
            Assert.AreEqual(1, result.Tags.Length);
            Assert.AreEqual(0, result.Tags.Single().CardCountBeforeRun);
            Assert.AreEqual(0, result.Tags.Single().AverageRatingBeforeRun);
            Assert.AreEqual(0, result.Tags.Single().CardCountAfterRun);
            Assert.AreEqual(0, result.Tags.Single().AverageRatingAfterRun);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var tagFromDb = await dbContext.Tags.SingleAsync();
            Assert.AreEqual(0, tagFromDb.AverageRatingOfPublicCards);
            Assert.AreEqual(0, tagFromDb.CountOfPublicCards);
        }
    }
    [TestMethod()]
    public async Task SingleTag_OnePublicCardWithoutTagWithoutRating()
    {
        var db = DbHelper.GetEmptyTestDB();
        var name = RandomHelper.String();
        var cardCreator = await UserHelper.CreateUserInDbAsync(db);
        await TagHelper.CreateAsync(db, cardCreator, name: name);
        await CardHelper.CreateAsync(db, cardCreator.Id);

        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new RefreshTagStats(dbContext.AsCallContext()).RunAsync(new RefreshTagStats.Request());
            Assert.AreEqual(1, result.Tags.Length);
            Assert.AreEqual(0, result.Tags.Single().CardCountBeforeRun);
            Assert.AreEqual(0, result.Tags.Single().CardCountAfterRun);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var tagFromDb = await dbContext.Tags.SingleAsync();
            Assert.AreEqual(0, tagFromDb.AverageRatingOfPublicCards);
            Assert.AreEqual(0, tagFromDb.CountOfPublicCards);
        }
    }
    [TestMethod()]
    public async Task SingleTag_OnePublicCardWithoutTagWithRating()
    {
        var db = DbHelper.GetEmptyTestDB();
        var name = RandomHelper.String();
        var user = await UserHelper.CreateUserInDbAsync(db);
        await TagHelper.CreateAsync(db, user, name: name);
        var cardId = await CardHelper.CreateIdAsync(db, user.Id);

        await RatingHelper.RecordForUserAsync(db, user.Id, cardId, RandomHelper.Rating());

        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new RefreshTagStats(dbContext.AsCallContext()).RunAsync(new RefreshTagStats.Request());
            Assert.AreEqual(1, result.Tags.Length);
            Assert.AreEqual(0, result.Tags.Single().CardCountBeforeRun);
            Assert.AreEqual(0, result.Tags.Single().CardCountAfterRun);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var tagFromDb = await dbContext.Tags.SingleAsync();
            Assert.AreEqual(0, tagFromDb.AverageRatingOfPublicCards);
            Assert.AreEqual(0, tagFromDb.CountOfPublicCards);
        }
    }
    [TestMethod()]
    public async Task SingleTag_OnePublicCardWithTagWithoutRating()
    {
        var db = DbHelper.GetEmptyTestDB();
        var name = RandomHelper.String();
        var cardCreator = await UserHelper.CreateUserInDbAsync(db);
        var tagId = await TagHelper.CreateAsync(db, cardCreator, name: name);
        await CardHelper.CreateAsync(db, cardCreator.Id, tagIds: tagId.AsArray());

        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new RefreshTagStats(dbContext.AsCallContext()).RunAsync(new RefreshTagStats.Request());
            Assert.AreEqual(1, result.Tags.Length);
            Assert.AreEqual(0, result.Tags.Single().CardCountBeforeRun);
            Assert.AreEqual(0, result.Tags.Single().AverageRatingBeforeRun);
            Assert.AreEqual(1, result.Tags.Single().CardCountAfterRun);
            Assert.AreEqual(0, result.Tags.Single().AverageRatingAfterRun);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var tagFromDb = await dbContext.Tags.SingleAsync();
            Assert.AreEqual(0, tagFromDb.AverageRatingOfPublicCards);
            Assert.AreEqual(1, tagFromDb.CountOfPublicCards);
        }
    }
    [TestMethod()]
    public async Task SingleTag_OnePublicCardWithTagWithOneRating()
    {
        var db = DbHelper.GetEmptyTestDB();
        var name = RandomHelper.String();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var tagId = await TagHelper.CreateAsync(db, user, name: name);
        var card = await CardHelper.CreateAsync(db, user.Id, tagIds: tagId.AsArray());

        var rating = RandomHelper.Rating();
        await RatingHelper.RecordForUserAsync(db, user.Id, card.Id, rating);

        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new RefreshTagStats(dbContext.AsCallContext()).RunAsync(new RefreshTagStats.Request());
            Assert.AreEqual(1, result.Tags.Length);
            Assert.AreEqual(0, result.Tags.Single().CardCountBeforeRun);
            Assert.AreEqual(0, result.Tags.Single().AverageRatingBeforeRun);
            Assert.AreEqual(1, result.Tags.Single().CardCountAfterRun);
            Assert.AreEqual(rating, result.Tags.Single().AverageRatingAfterRun);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var tagFromDb = await dbContext.Tags.SingleAsync();
            Assert.AreEqual(rating, tagFromDb.AverageRatingOfPublicCards);
            Assert.AreEqual(1, tagFromDb.CountOfPublicCards);
        }
    }
    [TestMethod()]
    public async Task ComplexCase()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user1 = await UserHelper.CreateUserInDbAsync(db);
        var tag1Id = await TagHelper.CreateAsync(db, user1);
        var tag2Id = await TagHelper.CreateAsync(db, user1);
        var card1 = await CardHelper.CreateAsync(db, user1.Id, tagIds: tag1Id.AsArray());

        var user1RatingOnCard1 = RandomHelper.Rating();
        await RatingHelper.RecordForUserAsync(db, user1.Id, card1.Id, user1RatingOnCard1);

        var user2Id = await UserHelper.CreateInDbAsync(db);
        var user2RatingOnCard1 = RandomHelper.Rating();
        await RatingHelper.RecordForUserAsync(db, user2Id, card1.Id, user2RatingOnCard1);

        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new RefreshTagStats(dbContext.AsCallContext()).RunAsync(new RefreshTagStats.Request());
            Assert.AreEqual(2, result.Tags.Length);

            var resultTag1 = result.Tags.Single(tag => tag.TagId == tag1Id);
            Assert.AreEqual(0, resultTag1.CardCountBeforeRun);
            Assert.AreEqual(0, resultTag1.AverageRatingBeforeRun);
            Assert.AreEqual(1, resultTag1.CardCountAfterRun);
            Assert.AreEqual((user1RatingOnCard1 + user2RatingOnCard1) / 2.0, resultTag1.AverageRatingAfterRun);

            var resultTag2 = result.Tags.Single(tag => tag.TagId == tag2Id);
            Assert.AreEqual(0, resultTag2.CardCountBeforeRun);
            Assert.AreEqual(0, resultTag2.AverageRatingBeforeRun);
            Assert.AreEqual(0, resultTag2.CardCountAfterRun);
            Assert.AreEqual(0, resultTag2.AverageRatingAfterRun);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var tag1FromDb = await dbContext.Tags.SingleAsync(tag => tag.Id == tag1Id);
            Assert.AreEqual((user1RatingOnCard1 + user2RatingOnCard1) / 2.0, tag1FromDb.AverageRatingOfPublicCards);
            Assert.AreEqual(1, tag1FromDb.CountOfPublicCards);

            var tag2FromDb = await dbContext.Tags.SingleAsync(tag => tag.Id == tag2Id);
            Assert.AreEqual(0, tag2FromDb.AverageRatingOfPublicCards);
            Assert.AreEqual(0, tag2FromDb.CountOfPublicCards);
        }

        var user2NewRatingOnCard1 = RandomHelper.Rating(user2RatingOnCard1);
        await RatingHelper.RecordForUserAsync(db, user2Id, card1.Id, user2NewRatingOnCard1);

        var card2 = await CardHelper.CreateAsync(db, user1.Id, tagIds: new[] { tag1Id, tag2Id });
        var user2RatingOnCard2 = RandomHelper.Rating();
        await RatingHelper.RecordForUserAsync(db, user2Id, card2.Id, user2RatingOnCard2);

        var card3 = await CardHelper.CreateAsync(db, user1.Id, tagIds: tag2Id.AsArray());

        var card4 = await CardHelper.CreateAsync(db, user1.Id, tagIds: tag1Id.AsArray(), userWithViewIds: user1.Id.AsArray());
        await RatingHelper.RecordForUserAsync(db, user1.Id, card4.Id, RandomHelper.Rating());

        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new RefreshTagStats(dbContext.AsCallContext()).RunAsync(new RefreshTagStats.Request());
            Assert.AreEqual(2, result.Tags.Length);

            var resultTag1 = result.Tags.Single(tag => tag.TagId == tag1Id);
            Assert.AreEqual(1, resultTag1.CardCountBeforeRun);
            Assert.AreEqual((user1RatingOnCard1 + user2RatingOnCard1) / 2.0, resultTag1.AverageRatingBeforeRun);
            Assert.AreEqual(2, resultTag1.CardCountAfterRun);
            var sum1 = user1RatingOnCard1 + user2NewRatingOnCard1;
            var average1 = sum1 / 2.0;
            Assert.AreEqual((average1 + user2RatingOnCard2) / 2.0, resultTag1.AverageRatingAfterRun);

            var resultTag2 = result.Tags.Single(tag => tag.TagId == tag2Id);
            Assert.AreEqual(0, resultTag2.CardCountBeforeRun);
            Assert.AreEqual(0, resultTag2.AverageRatingBeforeRun);
            Assert.AreEqual(2, resultTag2.CardCountAfterRun);
            Assert.AreEqual(user2RatingOnCard2, resultTag2.AverageRatingAfterRun);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var tag1FromDb = await dbContext.Tags.SingleAsync(tag => tag.Id == tag1Id);
            Assert.AreEqual((((user1RatingOnCard1 + user2NewRatingOnCard1) / 2.0) + user2RatingOnCard2) / 2.0, tag1FromDb.AverageRatingOfPublicCards);
            Assert.AreEqual(2, tag1FromDb.CountOfPublicCards);

            var tag2FromDb = await dbContext.Tags.SingleAsync(tag => tag.Id == tag2Id);
            Assert.AreEqual(user2RatingOnCard2, tag2FromDb.AverageRatingOfPublicCards);
            Assert.AreEqual(2, tag2FromDb.CountOfPublicCards);
        }
    }
}

using MemCheck.Application.Tests.Helpers;
using MemCheck.Basics;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Tags
{
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
            var tag = await TagHelper.CreateAsync(db, name: name);

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
            var tagId = await TagHelper.CreateAsync(db, name: name);
            var cardCreatorId = await UserHelper.CreateInDbAsync(db);
            await CardHelper.CreateAsync(db, cardCreatorId, tagIds: tagId.AsArray(), userWithViewIds: cardCreatorId.AsArray());

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
            var tagId = await TagHelper.CreateAsync(db, name: name);
            var userId = await UserHelper.CreateInDbAsync(db);
            var card = await CardHelper.CreateAsync(db, userId, tagIds: tagId.AsArray(), userWithViewIds: userId.AsArray());

            await RatingHelper.RecordForUserAsync(db, userId, card.Id, RandomHelper.Rating());

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
            await TagHelper.CreateAsync(db, name: name);
            var cardCreatorId = await UserHelper.CreateInDbAsync(db);
            await CardHelper.CreateAsync(db, cardCreatorId);

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
            var tagId = await TagHelper.CreateAsync(db, name: name);
            var userId = await UserHelper.CreateInDbAsync(db);
            var cardId = await CardHelper.CreateIdAsync(db, userId);

            await RatingHelper.RecordForUserAsync(db, userId, cardId, RandomHelper.Rating());

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
            var tagId = await TagHelper.CreateAsync(db, name: name);
            var cardCreatorId = await UserHelper.CreateInDbAsync(db);
            await CardHelper.CreateAsync(db, cardCreatorId, tagIds: tagId.AsArray());

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
            var tagId = await TagHelper.CreateAsync(db, name: name);
            var userId = await UserHelper.CreateInDbAsync(db);
            var card = await CardHelper.CreateAsync(db, userId, tagIds: tagId.AsArray());

            var rating = RandomHelper.Rating();
            await RatingHelper.RecordForUserAsync(db, userId, card.Id, rating);

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
            var tag1Id = await TagHelper.CreateAsync(db);
            var tag2Id = await TagHelper.CreateAsync(db);
            var user1Id = await UserHelper.CreateInDbAsync(db);
            var card1 = await CardHelper.CreateAsync(db, user1Id, tagIds: tag1Id.AsArray());

            var user1RatingOnCard1 = RandomHelper.Rating();
            await RatingHelper.RecordForUserAsync(db, user1Id, card1.Id, user1RatingOnCard1);

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

            var card2 = await CardHelper.CreateAsync(db, user1Id, tagIds: new[] { tag1Id, tag2Id });
            var user2RatingOnCard2 = RandomHelper.Rating();
            await RatingHelper.RecordForUserAsync(db, user2Id, card2.Id, user2RatingOnCard2);

            var card3 = await CardHelper.CreateAsync(db, user1Id, tagIds: tag2Id.AsArray());

            var card4 = await CardHelper.CreateAsync(db, user1Id, tagIds: tag1Id.AsArray(), userWithViewIds: user1Id.AsArray());
            await RatingHelper.RecordForUserAsync(db, user1Id, card4.Id, RandomHelper.Rating());

            using (var dbContext = new MemCheckDbContext(db))
            {
                var result = await new RefreshTagStats(dbContext.AsCallContext()).RunAsync(new RefreshTagStats.Request());
                Assert.AreEqual(2, result.Tags.Length);

                var resultTag1 = result.Tags.Single(tag => tag.TagId == tag1Id);
                Assert.AreEqual(1, resultTag1.CardCountBeforeRun);
                Assert.AreEqual((user1RatingOnCard1 + user2RatingOnCard1) / 2.0, resultTag1.AverageRatingBeforeRun);
                Assert.AreEqual(2, resultTag1.CardCountAfterRun);
                Assert.AreEqual(((user1RatingOnCard1 + user2NewRatingOnCard1) / 2.0 + user2RatingOnCard2) / 2.0, resultTag1.AverageRatingAfterRun);

                var resultTag2 = result.Tags.Single(tag => tag.TagId == tag2Id);
                Assert.AreEqual(0, resultTag2.CardCountBeforeRun);
                Assert.AreEqual(0, resultTag2.AverageRatingBeforeRun);
                Assert.AreEqual(2, resultTag2.CardCountAfterRun);
                Assert.AreEqual(user2RatingOnCard2, resultTag2.AverageRatingAfterRun);
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var tag1FromDb = await dbContext.Tags.SingleAsync(tag => tag.Id == tag1Id);
                Assert.AreEqual(((user1RatingOnCard1 + user2NewRatingOnCard1) / 2.0 + user2RatingOnCard2) / 2.0, tag1FromDb.AverageRatingOfPublicCards);
                Assert.AreEqual(2, tag1FromDb.CountOfPublicCards);

                var tag2FromDb = await dbContext.Tags.SingleAsync(tag => tag.Id == tag2Id);
                Assert.AreEqual(user2RatingOnCard2, tag2FromDb.AverageRatingOfPublicCards);
                Assert.AreEqual(2, tag2FromDb.CountOfPublicCards);
            }
        }
    }
}

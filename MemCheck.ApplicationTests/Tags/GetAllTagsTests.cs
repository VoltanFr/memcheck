using MemCheck.Application.QueryValidation;
using MemCheck.Application.Tests.Helpers;
using MemCheck.Basics;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Tags
{
    [TestClass()]
    public class GetAllTagsTests
    {
        [TestMethod()]
        public async Task Page0()
        {
            using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(1, 0, "")));
        }
        [TestMethod()]
        public async Task PageSize0()
        {
            using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(0, 1, "")));
        }
        [TestMethod()]
        public async Task PageSizeTooBig()
        {
            using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(0, GetAllTags.Request.MaxPageSize + 1, "")));
        }
        [TestMethod()]
        public async Task None()
        {
            using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
            var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(1, 10, ""));
            Assert.AreEqual(0, result.PageCount);
            Assert.AreEqual(0, result.TotalCount);
            Assert.AreEqual(0, result.Tags.Count());
        }
        [TestMethod()]
        public async Task OneTagInDbWithOneCardWithoutRating()
        {
            var db = DbHelper.GetEmptyTestDB();
            var name = RandomHelper.String();
            var description = RandomHelper.String();
            var tag = await TagHelper.CreateAsync(db, name, description);
            var user = await UserHelper.CreateInDbAsync(db);
            await CardHelper.CreateAsync(db, user, tagIds: tag.AsArray());

            using (var dbContext = new MemCheckDbContext(db))
                await new RefreshTagStats(dbContext.AsCallContext()).RunAsync(new RefreshTagStats.Request());

            using (var dbContext = new MemCheckDbContext(db))
            {
                var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(1, 1, ""));
                Assert.AreEqual(1, result.PageCount);
                Assert.AreEqual(1, result.TotalCount);
                Assert.AreEqual(1, result.Tags.Count());
                var resultTag = result.Tags.Single();
                Assert.AreEqual(tag, resultTag.TagId);
                Assert.AreEqual(name, resultTag.TagName);
                Assert.AreEqual(description, resultTag.TagDescription);
                Assert.AreEqual(1, resultTag.CardCount);
                Assert.AreEqual(0, resultTag.AverageRating);
            }
        }
        [TestMethod()]
        public async Task OneTagInDbWithOneCardWithRating()
        {
            var db = DbHelper.GetEmptyTestDB();
            var name = RandomHelper.String();
            var description = RandomHelper.String();
            var tag = await TagHelper.CreateAsync(db, name, description);
            var user = await UserHelper.CreateInDbAsync(db);
            var card = await CardHelper.CreateAsync(db, user, tagIds: tag.AsArray());
            var rating = RandomHelper.Rating();
            await RatingHelper.RecordForUserAsync(db, user, card.Id, rating);

            using (var dbContext = new MemCheckDbContext(db))
            {
                var cardFromDb = dbContext.Cards.Single();
                Assert.AreEqual(rating, cardFromDb.AverageRating);
            }

            using (var dbContext = new MemCheckDbContext(db))
                await new RefreshTagStats(dbContext.AsCallContext()).RunAsync(new RefreshTagStats.Request());

            using (var dbContext = new MemCheckDbContext(db))
            {
                var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(1, 1, ""));
                Assert.AreEqual(1, result.PageCount);
                Assert.AreEqual(1, result.TotalCount);
                Assert.AreEqual(1, result.Tags.Count());
                var resultTag = result.Tags.Single();
                Assert.AreEqual(tag, resultTag.TagId);
                Assert.AreEqual(name, resultTag.TagName);
                Assert.AreEqual(description, resultTag.TagDescription);
                Assert.AreEqual(1, resultTag.CardCount);
                Assert.AreEqual(rating, resultTag.AverageRating);
            }
        }
        [TestMethod()]
        public async Task Paging()
        {
            var db = DbHelper.GetEmptyTestDB();
            await TagHelper.CreateAsync(db);
            using (var dbContext = new MemCheckDbContext(db))
            {
                var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(1, 1, ""));
                Assert.AreEqual(1, result.PageCount);
                Assert.AreEqual(1, result.TotalCount);
                Assert.AreEqual(1, result.Tags.Count());
            }
            using (var dbContext = new MemCheckDbContext(db))
            {
                var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(1, 2, ""));
                Assert.AreEqual(1, result.PageCount);
                Assert.AreEqual(1, result.TotalCount);
                Assert.IsFalse(result.Tags.Any());
            }
        }
        [TestMethod()]
        public async Task NoUser_Filtering()
        {
            var db = DbHelper.GetEmptyTestDB();
            var tagName = RandomHelper.String(10);
            await TagHelper.CreateAsync(db, tagName);
            await TagHelper.CreateAsync(db);

            using (var dbContext = new MemCheckDbContext(db))
            {
                var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(1, 1, RandomHelper.String()));
                Assert.AreEqual(0, result.PageCount);
                Assert.AreEqual(0, result.TotalCount);
                Assert.AreEqual(0, result.Tags.Count());
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(1, 1, tagName.Substring(3, 5)));
                Assert.AreEqual(1, result.PageCount);
                Assert.AreEqual(1, result.TotalCount);
                Assert.AreEqual(1, result.Tags.Count());
            }
        }
        [TestMethod()]
        public async Task User_Filtering()
        {
            var db = DbHelper.GetEmptyTestDB();
            var tagName = RandomHelper.String(10);
            await TagHelper.CreateAsync(db, tagName);
            await TagHelper.CreateAsync(db);

            using (var dbContext = new MemCheckDbContext(db))
            {
                var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(1, 1, RandomHelper.String()));
                Assert.AreEqual(0, result.PageCount);
                Assert.AreEqual(0, result.TotalCount);
                Assert.AreEqual(0, result.Tags.Count());
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(1, 1, tagName.Substring(3, 5)));
                Assert.AreEqual(1, result.PageCount);
                Assert.AreEqual(1, result.TotalCount);
                Assert.AreEqual(1, result.Tags.Count());
            }
        }
        [TestMethod()]
        public async Task CardCount_OneTagWithNoCard()
        {
            var db = DbHelper.GetEmptyTestDB();
            await TagHelper.CreateAsync(db);

            using (var dbContext = new MemCheckDbContext(db))
                await new RefreshTagStats(dbContext.AsCallContext()).RunAsync(new RefreshTagStats.Request());

            using (var dbContext = new MemCheckDbContext(db))
            {
                var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(10, 1, ""));
                Assert.AreEqual(0, result.Tags.Single().CardCount);
            }
        }
        [TestMethod()]
        public async Task CardCount_OneTagWithOnePublicCardWithTag()
        {
            var db = DbHelper.GetEmptyTestDB();
            var tagId = await TagHelper.CreateAsync(db);
            var cardCreatorId = await UserHelper.CreateInDbAsync(db);
            await CardHelper.CreateAsync(db, cardCreatorId, tagIds: tagId.AsArray());

            using (var dbContext = new MemCheckDbContext(db))
                await new RefreshTagStats(dbContext.AsCallContext()).RunAsync(new RefreshTagStats.Request());

            using (var dbContext = new MemCheckDbContext(db))
            {
                var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(10, 1, ""));
                Assert.AreEqual(1, result.Tags.Single().CardCount);
            }
        }
        [TestMethod()]
        public async Task CardCount_OneTagWithOnePrivateCardWithTag()
        {
            var db = DbHelper.GetEmptyTestDB();
            var tagId = await TagHelper.CreateAsync(db);
            var cardCreatorId = await UserHelper.CreateInDbAsync(db);
            await CardHelper.CreateAsync(db, cardCreatorId, tagIds: tagId.AsArray(), userWithViewIds: cardCreatorId.AsArray());

            using (var dbContext = new MemCheckDbContext(db))
                await new RefreshTagStats(dbContext.AsCallContext()).RunAsync(new RefreshTagStats.Request());

            using (var dbContext = new MemCheckDbContext(db))
            {
                var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(10, 1, ""));
                Assert.AreEqual(0, result.Tags.Single().CardCount);
            }
        }
        [TestMethod()]
        public async Task CardCount_TwoTagsWithOnePublicCardWithOneTag()
        {
            var db = DbHelper.GetEmptyTestDB();
            var tag1 = await TagHelper.CreateAsync(db);
            var tag2 = await TagHelper.CreateAsync(db);
            var cardCreatorId = await UserHelper.CreateInDbAsync(db);
            await CardHelper.CreateAsync(db, cardCreatorId, tagIds: tag1.AsArray());

            using (var dbContext = new MemCheckDbContext(db))
                await new RefreshTagStats(dbContext.AsCallContext()).RunAsync(new RefreshTagStats.Request());

            using (var dbContext = new MemCheckDbContext(db))
            {
                var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(10, 1, ""));
                Assert.AreEqual(2, result.Tags.Count());
                Assert.AreEqual(1, result.Tags.Single(t => t.TagId == tag1).CardCount);
                Assert.AreEqual(0, result.Tags.Single(t => t.TagId == tag2).CardCount);
            }
        }
        [TestMethod()]
        public async Task CardCount_ThreeTagsWithOnePublicCardWithTwoTags()
        {
            var db = DbHelper.GetEmptyTestDB();
            var tag1 = await TagHelper.CreateAsync(db);
            var tag2 = await TagHelper.CreateAsync(db);
            var tag3 = await TagHelper.CreateAsync(db);
            var cardCreatorId = await UserHelper.CreateInDbAsync(db);
            await CardHelper.CreateAsync(db, cardCreatorId, tagIds: new[] { tag1, tag2 });

            using (var dbContext = new MemCheckDbContext(db))
                await new RefreshTagStats(dbContext.AsCallContext()).RunAsync(new RefreshTagStats.Request());

            using (var dbContext = new MemCheckDbContext(db))
            {
                var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(10, 1, ""));
                Assert.AreEqual(3, result.Tags.Count());
                Assert.AreEqual(1, result.Tags.Single(t => t.TagId == tag1).CardCount);
                Assert.AreEqual(1, result.Tags.Single(t => t.TagId == tag2).CardCount);
                Assert.AreEqual(0, result.Tags.Single(t => t.TagId == tag3).CardCount);
            }
        }
        [TestMethod()]
        public async Task CardCount_OneTagWithOnePublicCardWithoutTag()
        {
            var db = DbHelper.GetEmptyTestDB();
            await TagHelper.CreateAsync(db);
            var cardCreatorId = await UserHelper.CreateInDbAsync(db);
            await CardHelper.CreateAsync(db, cardCreatorId);

            using (var dbContext = new MemCheckDbContext(db))
                await new RefreshTagStats(dbContext.AsCallContext()).RunAsync(new RefreshTagStats.Request());

            using (var dbContext = new MemCheckDbContext(db))
            {
                var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(10, 1, ""));
                Assert.AreEqual(0, result.Tags.Single().CardCount);
            }
        }
        [TestMethod()]
        public async Task CardCount_OneTagWithOneNotOwnedPrivateCardWithTag()
        {
            var db = DbHelper.GetEmptyTestDB();
            var tagId = await TagHelper.CreateAsync(db);
            var cardCreatorId = await UserHelper.CreateInDbAsync(db);
            await CardHelper.CreateAsync(db, cardCreatorId, tagIds: tagId.AsArray(), userWithViewIds: cardCreatorId.AsArray());

            using (var dbContext = new MemCheckDbContext(db))
                await new RefreshTagStats(dbContext.AsCallContext()).RunAsync(new RefreshTagStats.Request());

            using (var dbContext = new MemCheckDbContext(db))
            {
                var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(10, 1, ""));
                Assert.AreEqual(0, result.Tags.Single().CardCount);
            }
        }
        [TestMethod()]
        public async Task CardCount_OneTagWithOneOwnedPrivateCardWithTag()
        {
            var db = DbHelper.GetEmptyTestDB();
            var tagName = RandomHelper.String();
            var tagDescription = RandomHelper.String();
            var tagId = await TagHelper.CreateAsync(db, tagName, tagDescription);
            var cardCreatorId = await UserHelper.CreateInDbAsync(db);
            await CardHelper.CreateAsync(db, cardCreatorId, tagIds: tagId.AsArray(), userWithViewIds: cardCreatorId.AsArray());

            using (var dbContext = new MemCheckDbContext(db))
                await new RefreshTagStats(dbContext.AsCallContext()).RunAsync(new RefreshTagStats.Request());

            using (var dbContext = new MemCheckDbContext(db))
                await new RefreshTagStats(dbContext.AsCallContext()).RunAsync(new RefreshTagStats.Request());

            using (var dbContext = new MemCheckDbContext(db))
            {
                var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(10, 1, ""));
                Assert.AreEqual(1, result.TotalCount);
                var resultTag = result.Tags.Single();
                Assert.AreEqual(tagId, resultTag.TagId);
                Assert.AreEqual(tagName, resultTag.TagName);
                Assert.AreEqual(tagDescription, resultTag.TagDescription);
                Assert.AreEqual(0, resultTag.CardCount); //Private cards are not counted
                Assert.AreEqual(0, resultTag.AverageRating);
            }
        }
        [TestMethod()]
        public async Task CardCount_OneTagWithOnePublicCardWithoutTag_CanView()
        {
            var db = DbHelper.GetEmptyTestDB();
            await TagHelper.CreateAsync(db);
            var cardCreatorId = await UserHelper.CreateInDbAsync(db);
            await CardHelper.CreateAsync(db, cardCreatorId);

            using (var dbContext = new MemCheckDbContext(db))
                await new RefreshTagStats(dbContext.AsCallContext()).RunAsync(new RefreshTagStats.Request());

            using (var dbContext = new MemCheckDbContext(db))
            {
                var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(10, 1, ""));
                Assert.AreEqual(0, result.Tags.Single().CardCount);
            }
        }
        [TestMethod()]
        public async Task CardCount_OneTagWithOnePublicCardWithoutTag_CantView()
        {
            var db = DbHelper.GetEmptyTestDB();
            await TagHelper.CreateAsync(db);
            var cardCreatorId = await UserHelper.CreateInDbAsync(db);
            await CardHelper.CreateAsync(db, cardCreatorId);

            using (var dbContext = new MemCheckDbContext(db))
                await new RefreshTagStats(dbContext.AsCallContext()).RunAsync(new RefreshTagStats.Request());

            using (var dbContext = new MemCheckDbContext(db))
            {
                var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(10, 1, ""));
                Assert.AreEqual(0, result.Tags.Single().CardCount);
            }
        }
        [TestMethod()]
        public async Task CardStats_ComplexCase()
        {
            var db = DbHelper.GetEmptyTestDB();

            var tag1 = await TagHelper.CreateAsync(db);
            var tag2 = await TagHelper.CreateAsync(db);
            var tag3 = await TagHelper.CreateAsync(db);

            var user1 = await UserHelper.CreateInDbAsync(db);
            var user2 = await UserHelper.CreateInDbAsync(db);
            var user3 = await UserHelper.CreateInDbAsync(db);

            //public cards
            var card1 = await CardHelper.CreateIdAsync(db, user1);
            var card2 = await CardHelper.CreateIdAsync(db, user1, tagIds: tag1.AsArray());
            await RatingHelper.RecordForUserAsync(db, user1, card2, 1);
            var card3 = await CardHelper.CreateIdAsync(db, user1, tagIds: new[] { tag1, tag2 });
            await RatingHelper.RecordForUserAsync(db, user1, card3, 1);
            await RatingHelper.RecordForUserAsync(db, user2, card3, 3);
            var card4 = await CardHelper.CreateIdAsync(db, user1, tagIds: new[] { tag1, tag2, tag3 });
            await RatingHelper.RecordForUserAsync(db, user2, card4, 2);
            await RatingHelper.RecordForUserAsync(db, user3, card4, 4);

            //Cards visible to user2 only
            var card5 = await CardHelper.CreateIdAsync(db, user2, userWithViewIds: new[] { user2 });
            await RatingHelper.RecordForUserAsync(db, user2, card5, 1);
            var card6 = await CardHelper.CreateIdAsync(db, user2, tagIds: tag1.AsArray(), userWithViewIds: new[] { user2 });
            await RatingHelper.RecordForUserAsync(db, user2, card6, 2);
            var card7 = await CardHelper.CreateIdAsync(db, user2, tagIds: new[] { tag1, tag2 }, userWithViewIds: new[] { user2 });
            await RatingHelper.RecordForUserAsync(db, user2, card7, 3);
            var card8 = await CardHelper.CreateIdAsync(db, user2, tagIds: new[] { tag1, tag2, tag3 }, userWithViewIds: new[] { user2 });
            await RatingHelper.RecordForUserAsync(db, user2, card8, 4);

            //Cards visible to user3 only
            var card9 = await CardHelper.CreateIdAsync(db, user3, userWithViewIds: new[] { user3 });
            await RatingHelper.RecordForUserAsync(db, user2, card9, 5);

            //Cards visible to user2 and user3
            var card10 = await CardHelper.CreateIdAsync(db, user2, tagIds: tag1.AsArray(), userWithViewIds: new[] { user2, user3 });
            await RatingHelper.RecordForUserAsync(db, user2, card10, 1);
            var card11 = await CardHelper.CreateIdAsync(db, user2, tagIds: new[] { tag1, tag2 }, userWithViewIds: new[] { user2, user3 });
            await RatingHelper.RecordForUserAsync(db, user2, card11, 2);
            var card12 = await CardHelper.CreateIdAsync(db, user2, tagIds: new[] { tag1, tag2, tag3 }, userWithViewIds: new[] { user2, user3 });
            await RatingHelper.RecordForUserAsync(db, user2, card12, 3);

            using (var dbContext = new MemCheckDbContext(db))
                await new RefreshTagStats(dbContext.AsCallContext()).RunAsync(new RefreshTagStats.Request());

            using (var dbContext = new MemCheckDbContext(db))
            {
                var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(10, 1, ""));
                Assert.AreEqual(3, result.Tags.Count());
                Assert.AreEqual(3, result.Tags.Single(t => t.TagId == tag1).CardCount);
                Assert.AreEqual(2, result.Tags.Single(t => t.TagId == tag1).AverageRating);
                Assert.AreEqual(2, result.Tags.Single(t => t.TagId == tag2).CardCount);
                Assert.AreEqual(2.5, result.Tags.Single(t => t.TagId == tag2).AverageRating);
                Assert.AreEqual(1, result.Tags.Single(t => t.TagId == tag3).CardCount);
                Assert.AreEqual(3, result.Tags.Single(t => t.TagId == tag3).AverageRating);
            }
        }
    }
}

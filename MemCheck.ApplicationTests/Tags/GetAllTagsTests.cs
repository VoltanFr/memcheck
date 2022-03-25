using MemCheck.Application.QueryValidation;
using MemCheck.Application.Tests.Helpers;
using MemCheck.Basics;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Tags
{
    [TestClass()]
    public class GetAllTagsTests
    {
        [TestMethod()]
        public async Task InvalidUser()
        {
            using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(RandomHelper.Guid(), 1, 1, "")));
        }
        [TestMethod()]
        public async Task Page0()
        {
            using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(Guid.Empty, 1, 0, "")));
        }
        [TestMethod()]
        public async Task PageSize0()
        {
            using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(Guid.Empty, 0, 1, "")));
        }
        [TestMethod()]
        public async Task PageSizeTooBig()
        {
            using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(Guid.Empty, 0, GetAllTags.Request.MaxPageSize + 1, "")));
        }
        [TestMethod()]
        public async Task None()
        {
            using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
            var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(Guid.Empty, 1, 10, ""));
            Assert.AreEqual(0, result.PageCount);
            Assert.AreEqual(0, result.TotalCount);
            Assert.AreEqual(0, result.Tags.Count());
        }
        [TestMethod()]
        public async Task OneTagInDb()
        {
            var db = DbHelper.GetEmptyTestDB();
            var name = RandomHelper.String();
            var description = RandomHelper.String();
            var tag = await TagHelper.CreateAsync(db, name, description);
            var user = await UserHelper.CreateInDbAsync(db);
            await CardHelper.CreateAsync(db, user, tagIds: tag.AsArray());
            using var dbContext = new MemCheckDbContext(db);
            var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(Guid.Empty, 1, 1, ""));
            Assert.AreEqual(1, result.PageCount);
            Assert.AreEqual(1, result.TotalCount);
            Assert.AreEqual(1, result.Tags.Count());
            Assert.AreEqual(tag, result.Tags.Single().TagId);
            Assert.AreEqual(name, result.Tags.Single().TagName);
            Assert.AreEqual(description, result.Tags.Single().TagDescription);
            Assert.AreEqual(1, result.Tags.Single().CardCount);
        }
        [TestMethod()]
        public async Task Paging()
        {
            var db = DbHelper.GetEmptyTestDB();
            await TagHelper.CreateAsync(db);
            using (var dbContext = new MemCheckDbContext(db))
            {
                var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(Guid.Empty, 1, 1, ""));
                Assert.AreEqual(1, result.PageCount);
                Assert.AreEqual(1, result.TotalCount);
                Assert.AreEqual(1, result.Tags.Count());
            }
            using (var dbContext = new MemCheckDbContext(db))
            {
                var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(Guid.Empty, 1, 2, ""));
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
                var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(Guid.Empty, 1, 1, RandomHelper.String()));
                Assert.AreEqual(0, result.PageCount);
                Assert.AreEqual(0, result.TotalCount);
                Assert.AreEqual(0, result.Tags.Count());
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(Guid.Empty, 1, 1, tagName.Substring(3, 5)));
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
            var user = await UserHelper.CreateInDbAsync(db);

            using (var dbContext = new MemCheckDbContext(db))
            {
                var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(user, 1, 1, RandomHelper.String()));
                Assert.AreEqual(0, result.PageCount);
                Assert.AreEqual(0, result.TotalCount);
                Assert.AreEqual(0, result.Tags.Count());
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(user, 1, 1, tagName.Substring(3, 5)));
                Assert.AreEqual(1, result.PageCount);
                Assert.AreEqual(1, result.TotalCount);
                Assert.AreEqual(1, result.Tags.Count());
            }
        }
        [TestMethod()]
        public async Task CardCount_NoUser_OneTagWithNoCard()
        {
            var db = DbHelper.GetEmptyTestDB();
            await TagHelper.CreateAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(Guid.Empty, 10, 1, ""));
            Assert.AreEqual(0, result.Tags.Single().CardCount);
        }
        [TestMethod()]
        public async Task CardCount_NoUser_OneTagWithOnePublicCardWithTag()
        {
            var db = DbHelper.GetEmptyTestDB();
            var tagId = await TagHelper.CreateAsync(db);
            var cardCreatorId = await UserHelper.CreateInDbAsync(db);
            await CardHelper.CreateAsync(db, cardCreatorId, tagIds: tagId.AsArray());

            using var dbContext = new MemCheckDbContext(db);
            var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(Guid.Empty, 10, 1, ""));
            Assert.AreEqual(1, result.Tags.Single().CardCount);
        }
        [TestMethod()]
        public async Task CardCount_NoUser_OneTagWithOnePrivateCardWithTag()
        {
            var db = DbHelper.GetEmptyTestDB();
            var tagId = await TagHelper.CreateAsync(db);
            var cardCreatorId = await UserHelper.CreateInDbAsync(db);
            await CardHelper.CreateAsync(db, cardCreatorId, tagIds: tagId.AsArray(), userWithViewIds: cardCreatorId.AsArray());

            using var dbContext = new MemCheckDbContext(db);
            var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(Guid.Empty, 10, 1, ""));
            Assert.AreEqual(0, result.Tags.Single().CardCount);
        }
        [TestMethod()]
        public async Task CardCount_NoUser_TwoTagsWithOnePublicCardWithOneTag()
        {
            var db = DbHelper.GetEmptyTestDB();
            var tag1 = await TagHelper.CreateAsync(db);
            var tag2 = await TagHelper.CreateAsync(db);
            var cardCreatorId = await UserHelper.CreateInDbAsync(db);
            await CardHelper.CreateAsync(db, cardCreatorId, tagIds: tag1.AsArray());

            using var dbContext = new MemCheckDbContext(db);
            var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(Guid.Empty, 10, 1, ""));
            Assert.AreEqual(2, result.Tags.Count());
            Assert.AreEqual(1, result.Tags.Single(t => t.TagId == tag1).CardCount);
            Assert.AreEqual(0, result.Tags.Single(t => t.TagId == tag2).CardCount);
        }
        [TestMethod()]
        public async Task CardCount_NoUser_ThreeTagsWithOnePublicCardWithTwoTags()
        {
            var db = DbHelper.GetEmptyTestDB();
            var tag1 = await TagHelper.CreateAsync(db);
            var tag2 = await TagHelper.CreateAsync(db);
            var tag3 = await TagHelper.CreateAsync(db);
            var cardCreatorId = await UserHelper.CreateInDbAsync(db);
            await CardHelper.CreateAsync(db, cardCreatorId, tagIds: new[] { tag1, tag2 });

            using var dbContext = new MemCheckDbContext(db);
            var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(Guid.Empty, 10, 1, ""));
            Assert.AreEqual(3, result.Tags.Count());
            Assert.AreEqual(1, result.Tags.Single(t => t.TagId == tag1).CardCount);
            Assert.AreEqual(1, result.Tags.Single(t => t.TagId == tag2).CardCount);
            Assert.AreEqual(0, result.Tags.Single(t => t.TagId == tag3).CardCount);
        }
        [TestMethod()]
        public async Task CardCount_NoUser_OneTagWithOnePublicCardWithoutTag()
        {
            var db = DbHelper.GetEmptyTestDB();
            await TagHelper.CreateAsync(db);
            var cardCreatorId = await UserHelper.CreateInDbAsync(db);
            await CardHelper.CreateAsync(db, cardCreatorId);

            using var dbContext = new MemCheckDbContext(db);
            var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(Guid.Empty, 10, 1, ""));
            Assert.AreEqual(0, result.Tags.Single().CardCount);
        }
        [TestMethod()]
        public async Task CardCount_User_OneTagWithNoCard()
        {
            var db = DbHelper.GetEmptyTestDB();
            await TagHelper.CreateAsync(db);
            var user = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(user, 10, 1, ""));
            Assert.AreEqual(0, result.Tags.Single().CardCount);
        }
        [TestMethod()]
        public async Task CardCount_User_OneTagWithOnePublicCardWithTag()
        {
            var db = DbHelper.GetEmptyTestDB();
            var tagId = await TagHelper.CreateAsync(db);
            var cardCreatorId = await UserHelper.CreateInDbAsync(db);
            await CardHelper.CreateAsync(db, cardCreatorId, tagIds: tagId.AsArray());

            using var dbContext = new MemCheckDbContext(db);
            var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(cardCreatorId, 10, 1, ""));
            Assert.AreEqual(1, result.Tags.Single().CardCount);
        }
        [TestMethod()]
        public async Task CardCount_User_OneTagWithOneNotOwnedPrivateCardWithTag()
        {
            var db = DbHelper.GetEmptyTestDB();
            var tagId = await TagHelper.CreateAsync(db);
            var cardCreatorId = await UserHelper.CreateInDbAsync(db);
            await CardHelper.CreateAsync(db, cardCreatorId, tagIds: tagId.AsArray(), userWithViewIds: cardCreatorId.AsArray());
            var otherUser = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(otherUser, 10, 1, ""));
            Assert.AreEqual(0, result.Tags.Single().CardCount);
        }
        [TestMethod()]
        public async Task CardCount_User_OneTagWithOneOwnedPrivateCardWithTag()
        {
            var db = DbHelper.GetEmptyTestDB();
            var tagId = await TagHelper.CreateAsync(db);
            var cardCreatorId = await UserHelper.CreateInDbAsync(db);
            await CardHelper.CreateAsync(db, cardCreatorId, tagIds: tagId.AsArray(), userWithViewIds: cardCreatorId.AsArray());

            using var dbContext = new MemCheckDbContext(db);
            var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(cardCreatorId, 10, 1, ""));
            Assert.AreEqual(1, result.Tags.Single().CardCount);
        }
        [TestMethod()]
        public async Task CardCount_User_TwoTagsWithOnePublicCardWithOneTag()
        {
            var db = DbHelper.GetEmptyTestDB();
            var tag1 = await TagHelper.CreateAsync(db);
            var tag2 = await TagHelper.CreateAsync(db);
            var cardCreatorId = await UserHelper.CreateInDbAsync(db);
            await CardHelper.CreateAsync(db, cardCreatorId, tagIds: tag1.AsArray());
            var user = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(user, 10, 1, ""));
            Assert.AreEqual(2, result.Tags.Count());
            Assert.AreEqual(1, result.Tags.Single(t => t.TagId == tag1).CardCount);
            Assert.AreEqual(0, result.Tags.Single(t => t.TagId == tag2).CardCount);
        }
        [TestMethod()]
        public async Task CardCount_User_ThreeTagsWithOnePublicCardWithTwoTags()
        {
            var db = DbHelper.GetEmptyTestDB();
            var tag1 = await TagHelper.CreateAsync(db);
            var tag2 = await TagHelper.CreateAsync(db);
            var tag3 = await TagHelper.CreateAsync(db);
            var cardCreatorId = await UserHelper.CreateInDbAsync(db);
            await CardHelper.CreateAsync(db, cardCreatorId, tagIds: new[] { tag1, tag2 });

            using var dbContext = new MemCheckDbContext(db);
            var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(cardCreatorId, 10, 1, ""));
            Assert.AreEqual(3, result.Tags.Count());
            Assert.AreEqual(1, result.Tags.Single(t => t.TagId == tag1).CardCount);
            Assert.AreEqual(1, result.Tags.Single(t => t.TagId == tag2).CardCount);
            Assert.AreEqual(0, result.Tags.Single(t => t.TagId == tag3).CardCount);
        }
        [TestMethod()]
        public async Task CardCount_User_OneTagWithOnePublicCardWithoutTag_CanView()
        {
            var db = DbHelper.GetEmptyTestDB();
            await TagHelper.CreateAsync(db);
            var cardCreatorId = await UserHelper.CreateInDbAsync(db);
            await CardHelper.CreateAsync(db, cardCreatorId);

            using var dbContext = new MemCheckDbContext(db);
            var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(cardCreatorId, 10, 1, ""));
            Assert.AreEqual(0, result.Tags.Single().CardCount);
        }
        [TestMethod()]
        public async Task CardCount_User_OneTagWithOnePublicCardWithoutTag_CantView()
        {
            var db = DbHelper.GetEmptyTestDB();
            await TagHelper.CreateAsync(db);
            var cardCreatorId = await UserHelper.CreateInDbAsync(db);
            await CardHelper.CreateAsync(db, cardCreatorId);
            var user1 = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(user1, 10, 1, ""));
            Assert.AreEqual(0, result.Tags.Single().CardCount);
        }
        [TestMethod()]
        public async Task CardCount_ComplexCase()
        {
            var db = DbHelper.GetEmptyTestDB();

            var tag1 = await TagHelper.CreateAsync(db);
            var tag2 = await TagHelper.CreateAsync(db);
            var tag3 = await TagHelper.CreateAsync(db);

            var user1 = await UserHelper.CreateInDbAsync(db);
            var user2 = await UserHelper.CreateInDbAsync(db);
            var user3 = await UserHelper.CreateInDbAsync(db);

            //public cards
            await CardHelper.CreateAsync(db, user1);
            await CardHelper.CreateAsync(db, user1, tagIds: tag1.AsArray());
            await CardHelper.CreateAsync(db, user1, tagIds: new[] { tag1, tag2 });
            await CardHelper.CreateAsync(db, user1, tagIds: new[] { tag1, tag2, tag3 });

            //Cards visible to user2 only
            await CardHelper.CreateAsync(db, user2, userWithViewIds: new[] { user2 });
            await CardHelper.CreateAsync(db, user2, tagIds: tag1.AsArray(), userWithViewIds: new[] { user2 });
            await CardHelper.CreateAsync(db, user2, tagIds: new[] { tag1, tag2 }, userWithViewIds: new[] { user2 });
            await CardHelper.CreateAsync(db, user2, tagIds: new[] { tag1, tag2, tag3 }, userWithViewIds: new[] { user2 });

            //Cards visible to user3 only
            await CardHelper.CreateAsync(db, user3, userWithViewIds: new[] { user3 });

            //Cards visible to user2 and user3
            await CardHelper.CreateAsync(db, user2, tagIds: tag1.AsArray(), userWithViewIds: new[] { user2, user3 });
            await CardHelper.CreateAsync(db, user2, tagIds: new[] { tag1, tag2 }, userWithViewIds: new[] { user2, user3 });
            await CardHelper.CreateAsync(db, user2, tagIds: new[] { tag1, tag2, tag3 }, userWithViewIds: new[] { user2, user3 });

            //Query by user1
            using (var dbContext = new MemCheckDbContext(db))
            {
                var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(user1, 10, 1, ""));
                Assert.AreEqual(3, result.Tags.Count());
                Assert.AreEqual(3, result.Tags.Single(t => t.TagId == tag1).CardCount);
                Assert.AreEqual(2, result.Tags.Single(t => t.TagId == tag2).CardCount);
                Assert.AreEqual(1, result.Tags.Single(t => t.TagId == tag3).CardCount);
            }

            //Query by user2
            using (var dbContext = new MemCheckDbContext(db))
            {
                var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(user2, 10, 1, ""));
                Assert.AreEqual(3, result.Tags.Count());
                Assert.AreEqual(9, result.Tags.Single(t => t.TagId == tag1).CardCount);
                Assert.AreEqual(6, result.Tags.Single(t => t.TagId == tag2).CardCount);
                Assert.AreEqual(3, result.Tags.Single(t => t.TagId == tag3).CardCount);
            }

            //Query by user3
            using (var dbContext = new MemCheckDbContext(db))
            {
                var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(user3, 10, 1, ""));
                Assert.AreEqual(3, result.Tags.Count());
                Assert.AreEqual(6, result.Tags.Single(t => t.TagId == tag1).CardCount);
                Assert.AreEqual(4, result.Tags.Single(t => t.TagId == tag2).CardCount);
                Assert.AreEqual(2, result.Tags.Single(t => t.TagId == tag3).CardCount);
            }
        }
    }
}

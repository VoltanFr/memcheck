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
        public async Task PagSize0()
        {
            using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(0, 1, "")));
        }
        [TestMethod()]
        public async Task PagSizeTooBig()
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
        public async Task OneTagInDb()
        {
            var db = DbHelper.GetEmptyTestDB();
            var tagName = RandomHelper.String();
            var tag = await TagHelper.CreateAsync(db, tagName);
            var user = await UserHelper.CreateInDbAsync(db);
            await CardHelper.CreateAsync(db, user, tagIds: tag.AsArray());
            using var dbContext = new MemCheckDbContext(db);
            var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(1, 1, ""));
            Assert.AreEqual(1, result.PageCount);
            Assert.AreEqual(1, result.TotalCount);
            Assert.AreEqual(1, result.Tags.Count());
            Assert.AreEqual(tag, result.Tags.Single().TagId);
            Assert.AreEqual(tagName, result.Tags.Single().TagName);
            Assert.AreEqual(1, result.Tags.Single().CardCount);
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
        public async Task Filtering()
        {
            var db = DbHelper.GetEmptyTestDB();
            var tagName = RandomHelper.String();
            await TagHelper.CreateAsync(db, tagName);
            using (var dbContext = new MemCheckDbContext(db))
            {
                var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(1, 1, RandomHelper.String()));
                Assert.AreEqual(0, result.PageCount);
                Assert.AreEqual(0, result.TotalCount);
                Assert.AreEqual(0, result.Tags.Count());
            }
            using (var dbContext = new MemCheckDbContext(db))
            {
                var result = await new GetAllTags(dbContext.AsCallContext()).RunAsync(new GetAllTags.Request(1, 1, tagName.Substring(1, 5)));
                Assert.AreEqual(1, result.PageCount);
                Assert.AreEqual(1, result.TotalCount);
                Assert.AreEqual(1, result.Tags.Count());
            }
        }
    }
}

using MemCheck.Application.QueryValidation;
using MemCheck.Application.Tests.Helpers;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Tags
{
    [TestClass()]
    public class CreateTagTests
    {
        [TestMethod()]
        public async Task EmptyName()
        {
            using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new CreateTag(dbContext).RunAsync(new CreateTag.Request(""), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task NameNotTrimmed()
        {
            using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new CreateTag(dbContext).RunAsync(new CreateTag.Request(RandomHelper.String(QueryValidationHelper.TagMinNameLength) + '\t'), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task NameTooShort()
        {
            using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new CreateTag(dbContext).RunAsync(new CreateTag.Request(RandomHelper.String(QueryValidationHelper.TagMinNameLength - 1)), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task NameTooLong()
        {
            using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new CreateTag(dbContext).RunAsync(new CreateTag.Request(RandomHelper.String(QueryValidationHelper.TagMaxNameLength + 1)), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task NameWithForbiddenChar()
        {
            using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new CreateTag(dbContext).RunAsync(new CreateTag.Request("a<b"), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task AlreadyExists()
        {
            var name = RandomHelper.String();
            var db = DbHelper.GetEmptyTestDB();
            using (var dbContext = new MemCheckDbContext(db))
                await new CreateTag(dbContext).RunAsync(new CreateTag.Request(name), new TestLocalizer());
            using (var dbContext = new MemCheckDbContext(db))
                await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new CreateTag(dbContext).RunAsync(new CreateTag.Request(name), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task Success()
        {
            var name = RandomHelper.String();
            var db = DbHelper.GetEmptyTestDB();
            using (var dbContext = new MemCheckDbContext(db))
                await new CreateTag(dbContext).RunAsync(new CreateTag.Request(name), new TestLocalizer());
            using (var dbContext = new MemCheckDbContext(db))
            {
                var allTags = await new GetAllTags(dbContext).RunAsync(new GetAllTags.Request(GetAllTags.Request.MaxPageSize, 1, ""));
                Assert.AreEqual(1, allTags.Tags.Count());
                Assert.AreEqual(name, allTags.Tags.Single().TagName);
            }
        }
    }
}

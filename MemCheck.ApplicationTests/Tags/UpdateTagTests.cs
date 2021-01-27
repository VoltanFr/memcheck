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
    public class UpdateTagTests
    {
        [TestMethod()]
        public async Task DoesNotExist()
        {
            using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new UpdateTag(dbContext).RunAsync(new UpdateTag.Request(Guid.NewGuid(), RandomHelper.String()), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task EmptyName()
        {
            var db = DbHelper.GetEmptyTestDB();
            var tag = await TagHelper.CreateAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateTag(dbContext).RunAsync(new UpdateTag.Request(tag, ""), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task NameNotTrimmed()
        {
            var db = DbHelper.GetEmptyTestDB();
            var tag = await TagHelper.CreateAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new UpdateTag(dbContext).RunAsync(new UpdateTag.Request(tag, RandomHelper.String(QueryValidationHelper.TagMinNameLength) + '\t'), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task NameTooShort()
        {
            var db = DbHelper.GetEmptyTestDB();
            var tag = await TagHelper.CreateAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateTag(dbContext).RunAsync(new UpdateTag.Request(tag, RandomHelper.String(QueryValidationHelper.TagMinNameLength - 1)), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task NameTooLong()
        {
            var db = DbHelper.GetEmptyTestDB();
            var tag = await TagHelper.CreateAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateTag(dbContext).RunAsync(new UpdateTag.Request(tag, RandomHelper.String(QueryValidationHelper.TagMaxNameLength + 1)), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task NameWithForbiddenChar()
        {
            var db = DbHelper.GetEmptyTestDB();
            var tag = await TagHelper.CreateAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateTag(dbContext).RunAsync(new UpdateTag.Request(tag, "a<b"), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task AlreadyExists()
        {
            var name = RandomHelper.String();
            var db = DbHelper.GetEmptyTestDB();
            var tag = await TagHelper.CreateAsync(db);
            var otherTag = await TagHelper.CreateAsync(db, name);
            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateTag(dbContext).RunAsync(new UpdateTag.Request(tag, name), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task Success()
        {
            var db = DbHelper.GetEmptyTestDB();
            var tag = await TagHelper.CreateAsync(db);
            var name = RandomHelper.String();
            using (var dbContext = new MemCheckDbContext(db))
                await new UpdateTag(dbContext).RunAsync(new UpdateTag.Request(tag, name), new TestLocalizer());
            using (var dbContext = new MemCheckDbContext(db))
            {
                var allTags = await new GetAllTags(dbContext).RunAsync(new GetAllTags.Request(GetAllTags.Request.MaxPageSize, 1, ""));
                Assert.AreEqual(1, allTags.Tags.Count());
                Assert.AreEqual(name, allTags.Tags.Single().TagName);
            }
        }
    }
}

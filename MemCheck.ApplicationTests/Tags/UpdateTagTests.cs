using MemCheck.Application.QueryValidation;
using MemCheck.Application.Tests.Helpers;
using MemCheck.Database;
using MemCheck.Domain;
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
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new UpdateTag(dbContext).RunAsync(new UpdateTag.Request(Guid.NewGuid(), RandomHelper.String(), ""), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task EmptyName()
        {
            var db = DbHelper.GetEmptyTestDB();
            var tag = await TagHelper.CreateAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateTag(dbContext).RunAsync(new UpdateTag.Request(tag, "", ""), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task NameNotTrimmed()
        {
            var db = DbHelper.GetEmptyTestDB();
            var tag = await TagHelper.CreateAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new UpdateTag(dbContext).RunAsync(new UpdateTag.Request(tag, RandomHelper.String(Tag.MinNameLength) + '\t', ""), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task DescriptionNotTrimmed()
        {
            var db = DbHelper.GetEmptyTestDB();
            var tag = await TagHelper.CreateAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new UpdateTag(dbContext).RunAsync(new UpdateTag.Request(tag, RandomHelper.String(), "\t"), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task NameTooShort()
        {
            var db = DbHelper.GetEmptyTestDB();
            var tag = await TagHelper.CreateAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateTag(dbContext).RunAsync(new UpdateTag.Request(tag, RandomHelper.String(Tag.MinNameLength - 1), ""), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task NameTooLong()
        {
            var db = DbHelper.GetEmptyTestDB();
            var tag = await TagHelper.CreateAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateTag(dbContext).RunAsync(new UpdateTag.Request(tag, RandomHelper.String(Tag.MaxNameLength + 1), ""), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task DescriptionTooLong()
        {
            var db = DbHelper.GetEmptyTestDB();
            var tag = await TagHelper.CreateAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateTag(dbContext).RunAsync(new UpdateTag.Request(tag, RandomHelper.String(), RandomHelper.String(Tag.MaxDescriptionLength + 1)), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task NameWithForbiddenChar()
        {
            var db = DbHelper.GetEmptyTestDB();
            var tag = await TagHelper.CreateAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateTag(dbContext).RunAsync(new UpdateTag.Request(tag, "a<b", ""), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task AlreadyExists()
        {
            var name = RandomHelper.String();
            var db = DbHelper.GetEmptyTestDB();
            var tag = await TagHelper.CreateAsync(db);
            var otherTag = await TagHelper.CreateAsync(db, name);
            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateTag(dbContext).RunAsync(new UpdateTag.Request(tag, name, ""), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task NoChange()
        {
            var name = RandomHelper.String();
            var description = RandomHelper.String();
            var db = DbHelper.GetEmptyTestDB();
            var tag = await TagHelper.CreateAsync(db, name, description);
            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateTag(dbContext).RunAsync(new UpdateTag.Request(tag, name, description), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task SuccessfulUpdateOfBothFields()
        {
            var db = DbHelper.GetEmptyTestDB();
            var tag = await TagHelper.CreateAsync(db);
            var name = RandomHelper.String();
            var description = RandomHelper.String();
            using (var dbContext = new MemCheckDbContext(db))
                await new UpdateTag(dbContext).RunAsync(new UpdateTag.Request(tag, name, description), new TestLocalizer());
            using (var dbContext = new MemCheckDbContext(db))
            {
                var loaded = await new GetTag(dbContext).RunAsync(new GetTag.Request(tag));
                Assert.AreEqual(name, loaded.TagName);
                Assert.AreEqual(description, loaded.Description);
            }
        }
        [TestMethod()]
        public async Task SuccessfulUpdateOfName()
        {
            var db = DbHelper.GetEmptyTestDB();
            var description = RandomHelper.String();
            var tag = await TagHelper.CreateAsync(db, description: description);
            var name = RandomHelper.String();
            using (var dbContext = new MemCheckDbContext(db))
                await new UpdateTag(dbContext).RunAsync(new UpdateTag.Request(tag, name, description), new TestLocalizer());
            using (var dbContext = new MemCheckDbContext(db))
            {
                var loaded = await new GetTag(dbContext).RunAsync(new GetTag.Request(tag));
                Assert.AreEqual(name, loaded.TagName);
                Assert.AreEqual(description, loaded.Description);
            }
        }
        [TestMethod()]
        public async Task SuccessfulUpdateOfDescription()
        {
            var db = DbHelper.GetEmptyTestDB();
            var name = RandomHelper.String();
            var tag = await TagHelper.CreateAsync(db, name);
            var description = RandomHelper.String();
            using (var dbContext = new MemCheckDbContext(db))
                await new UpdateTag(dbContext).RunAsync(new UpdateTag.Request(tag, name, description), new TestLocalizer());
            using (var dbContext = new MemCheckDbContext(db))
            {
                var loaded = await new GetTag(dbContext).RunAsync(new GetTag.Request(tag));
                Assert.AreEqual(name, loaded.TagName);
                Assert.AreEqual(description, loaded.Description);
            }
        }
    }
}

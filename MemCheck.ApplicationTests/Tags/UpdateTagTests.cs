using MemCheck.Application.Helpers;
using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Tags
{
    [TestClass()]
    public class UpdateTagTests
    {
        [TestMethod()]
        public async Task DoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(db);
            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(userId, Guid.NewGuid(), RandomHelper.String(), "")));
        }
        [TestMethod()]
        public async Task EmptyName()
        {
            var db = DbHelper.GetEmptyTestDB();
            var tag = await TagHelper.CreateAsync(db);
            var userId = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(userId, tag, "", "")));
        }
        [TestMethod()]
        public async Task NameNotTrimmed()
        {
            var db = DbHelper.GetEmptyTestDB();
            var tag = await TagHelper.CreateAsync(db);
            var userId = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(userId, tag, RandomHelper.String(Tag.MinNameLength) + '\t', "")));
        }
        [TestMethod()]
        public async Task DescriptionNotTrimmed()
        {
            var db = DbHelper.GetEmptyTestDB();
            var tag = await TagHelper.CreateAsync(db);
            var userId = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(userId, tag, RandomHelper.String(), "\t")));
        }
        [TestMethod()]
        public async Task NameTooShort()
        {
            var db = DbHelper.GetEmptyTestDB();
            var tag = await TagHelper.CreateAsync(db);
            var userId = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(userId, tag, RandomHelper.String(Tag.MinNameLength - 1), "")));
        }
        [TestMethod()]
        public async Task NameTooLong()
        {
            var db = DbHelper.GetEmptyTestDB();
            var tag = await TagHelper.CreateAsync(db);
            var userId = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(userId, tag, RandomHelper.String(Tag.MaxNameLength + 1), "")));
        }
        [TestMethod()]
        public async Task DescriptionTooLong()
        {
            var db = DbHelper.GetEmptyTestDB();
            var tag = await TagHelper.CreateAsync(db);
            var userId = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(userId, tag, RandomHelper.String(), RandomHelper.String(Tag.MaxDescriptionLength + 1))));
        }
        [TestMethod()]
        public async Task NameWithForbiddenChar()
        {
            var db = DbHelper.GetEmptyTestDB();
            var tag = await TagHelper.CreateAsync(db);
            var userId = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(userId, tag, "a<b", "")));
        }
        [TestMethod()]
        public async Task AlreadyExists()
        {
            var name = RandomHelper.String();
            var db = DbHelper.GetEmptyTestDB();
            var tag = await TagHelper.CreateAsync(db);
            var otherTag = await TagHelper.CreateAsync(db, name);
            var userId = await UserHelper.CreateInDbAsync(db);
            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(userId, tag, name, "")));
        }
        [TestMethod()]
        public async Task NoChange()
        {
            var name = RandomHelper.String();
            var description = RandomHelper.String();
            var db = DbHelper.GetEmptyTestDB();
            var tag = await TagHelper.CreateAsync(db, name, description);
            var userId = await UserHelper.CreateInDbAsync(db);
            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(userId, tag, name, description)));
        }
        [TestMethod()]
        public async Task SuccessfulUpdateOfBothFields()
        {
            var db = DbHelper.GetEmptyTestDB();
            var tag = await TagHelper.CreateAsync(db);
            var name = RandomHelper.String();
            var description = RandomHelper.String();
            var userId = await UserHelper.CreateInDbAsync(db);
            using (var dbContext = new MemCheckDbContext(db))
                await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(userId, tag, name, description));
            using (var dbContext = new MemCheckDbContext(db))
            {
                var loaded = await new GetTag(dbContext.AsCallContext()).RunAsync(new GetTag.Request(tag));
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
            var userId = await UserHelper.CreateInDbAsync(db);
            using (var dbContext = new MemCheckDbContext(db))
                await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(userId, tag, name, description));
            using (var dbContext = new MemCheckDbContext(db))
            {
                var loaded = await new GetTag(dbContext.AsCallContext()).RunAsync(new GetTag.Request(tag));
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
            var userId = await UserHelper.CreateInDbAsync(db);
            using (var dbContext = new MemCheckDbContext(db))
                await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(userId, tag, name, description));
            using (var dbContext = new MemCheckDbContext(db))
            {
                var loaded = await new GetTag(dbContext.AsCallContext()).RunAsync(new GetTag.Request(tag));
                Assert.AreEqual(name, loaded.TagName);
                Assert.AreEqual(description, loaded.Description);
            }
        }
        [TestMethod()]
        public async Task UserNotLoggedIn()
        {
            var db = DbHelper.GetEmptyTestDB();

            var name = RandomHelper.String();
            var tag = await TagHelper.CreateAsync(db, name);

            using (var dbContext = new MemCheckDbContext(db))
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(Guid.Empty, tag, RandomHelper.String(), RandomHelper.String())));

            using (var dbContext = new MemCheckDbContext(db))
                Assert.AreEqual(name, (await new GetTag(dbContext.AsCallContext()).RunAsync(new GetTag.Request(tag))).TagName);
        }
        [TestMethod()]
        public async Task UnknownUser()
        {
            var db = DbHelper.GetEmptyTestDB();

            var name = RandomHelper.String();
            var tag = await TagHelper.CreateAsync(db, name);

            await UserHelper.CreateInDbAsync(db);

            using (var dbContext = new MemCheckDbContext(db))
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(Guid.NewGuid(), tag, RandomHelper.String(), RandomHelper.String())));

            using (var dbContext = new MemCheckDbContext(db))
                Assert.AreEqual(name, (await new GetTag(dbContext.AsCallContext()).RunAsync(new GetTag.Request(tag))).TagName);
        }
    }
}

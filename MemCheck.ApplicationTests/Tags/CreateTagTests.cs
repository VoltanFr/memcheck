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
    public class CreateTagTests
    {
        [TestMethod()]
        public async Task EmptyName()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(testDB);
            using var dbContext = new MemCheckDbContext(testDB);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new CreateTag(dbContext.AsCallContext()).RunAsync(new CreateTag.Request(userId, "", "")));
        }
        [TestMethod()]
        public async Task NameNotTrimmed()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(testDB);
            using var dbContext = new MemCheckDbContext(testDB);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new CreateTag(dbContext.AsCallContext()).RunAsync(new CreateTag.Request(userId, RandomHelper.String(Tag.MinNameLength) + '\t', "")));
        }
        [TestMethod()]
        public async Task DescriptionNotTrimmed()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(testDB);
            using var dbContext = new MemCheckDbContext(testDB);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new CreateTag(dbContext.AsCallContext()).RunAsync(new CreateTag.Request(userId, RandomHelper.String(), RandomHelper.String() + '\t')));
        }
        [TestMethod()]
        public async Task NameTooShort()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(testDB);
            using var dbContext = new MemCheckDbContext(testDB);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new CreateTag(dbContext.AsCallContext()).RunAsync(new CreateTag.Request(userId, RandomHelper.String(Tag.MinNameLength - 1), "")));
        }
        [TestMethod()]
        public async Task NameTooLong()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(testDB);
            using var dbContext = new MemCheckDbContext(testDB);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new CreateTag(dbContext.AsCallContext()).RunAsync(new CreateTag.Request(userId, RandomHelper.String(Tag.MaxNameLength + 1), "")));
        }
        [TestMethod()]
        public async Task DescriptionTooLong()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(testDB);
            using var dbContext = new MemCheckDbContext(testDB);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new CreateTag(dbContext.AsCallContext()).RunAsync(new CreateTag.Request(userId, RandomHelper.String(), RandomHelper.String(Tag.MaxDescriptionLength + 1))));
        }
        [TestMethod()]
        public async Task NameWithForbiddenChar()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(testDB);
            using var dbContext = new MemCheckDbContext(testDB);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new CreateTag(dbContext.AsCallContext()).RunAsync(new CreateTag.Request(userId, "a<b", "")));
        }
        [TestMethod()]
        public async Task AlreadyExists()
        {
            var name = RandomHelper.String();
            var db = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(db);
            using (var dbContext = new MemCheckDbContext(db))
                await new CreateTag(dbContext.AsCallContext()).RunAsync(new CreateTag.Request(userId, name, ""));
            using (var dbContext = new MemCheckDbContext(db))
                await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new CreateTag(dbContext.AsCallContext()).RunAsync(new CreateTag.Request(userId, name, "")));
        }
        [TestMethod()]
        public async Task Success()
        {
            var name = RandomHelper.String();
            var description = RandomHelper.String();
            var db = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(db);
            Guid tag;
            using (var dbContext = new MemCheckDbContext(db))
                tag = (await new CreateTag(dbContext.AsCallContext()).RunAsync(new CreateTag.Request(userId, name, description))).TagId;
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
            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new CreateTag(dbContext.AsCallContext()).RunAsync(new CreateTag.Request(Guid.Empty, RandomHelper.String(), RandomHelper.String())));
        }
        [TestMethod()]
        public async Task UnknownUser()
        {
            var db = DbHelper.GetEmptyTestDB();
            await UserHelper.CreateInDbAsync(db);
            var userId = Guid.NewGuid();
            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new CreateTag(dbContext.AsCallContext()).RunAsync(new CreateTag.Request(userId, RandomHelper.String(), RandomHelper.String())));
        }
    }
}

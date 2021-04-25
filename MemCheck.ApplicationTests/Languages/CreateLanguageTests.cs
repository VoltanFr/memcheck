using MemCheck.Application.QueryValidation;
using MemCheck.Application.Tests.Helpers;
using MemCheck.Database;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Languages
{
    [TestClass()]
    public class CreateLanguageTests
    {
        [TestMethod()]
        public async Task UserNotLoggedIn()
        {
            using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new CreateLanguage(dbContext, new TestRoleChecker()).RunAsync(new CreateLanguage.Request(Guid.Empty, RandomHelper.String()), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task UserDoesNotExist()
        {
            using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new CreateLanguage(dbContext, new TestRoleChecker()).RunAsync(new CreateLanguage.Request(Guid.NewGuid(), RandomHelper.String()), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task UserIsNotAdmin()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new CreateLanguage(dbContext, new TestRoleChecker()).RunAsync(new CreateLanguage.Request(user, RandomHelper.String()), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task EmptyName()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new CreateLanguage(dbContext, new TestRoleChecker(user)).RunAsync(new CreateLanguage.Request(user, ""), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task NameNotTrimmed()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new CreateLanguage(dbContext, new TestRoleChecker(user)).RunAsync(new CreateLanguage.Request(user, RandomHelper.String(QueryValidationHelper.LanguageMinNameLength) + '\t'), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task NameTooShort()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new CreateLanguage(dbContext, new TestRoleChecker(user)).RunAsync(new CreateLanguage.Request(user, RandomHelper.String(QueryValidationHelper.LanguageMinNameLength - 1)), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task NameTooLong()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new CreateLanguage(dbContext, new TestRoleChecker(user)).RunAsync(new CreateLanguage.Request(user, RandomHelper.String(QueryValidationHelper.LanguageMaxNameLength + 1)), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task NameWithForbiddenChar()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new CreateLanguage(dbContext, new TestRoleChecker(user)).RunAsync(new CreateLanguage.Request(user, "a<b"), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task AlreadyExists()
        {
            var name = RandomHelper.String();
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            using (var dbContext = new MemCheckDbContext(db))
                await new CreateLanguage(dbContext, new TestRoleChecker(user)).RunAsync(new CreateLanguage.Request(user, name), new TestLocalizer());
            using (var dbContext = new MemCheckDbContext(db))
                await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new CreateLanguage(dbContext, new TestRoleChecker(user)).RunAsync(new CreateLanguage.Request(user, name), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task Success()
        {
            var name = RandomHelper.String();
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            Guid createdId;
            using (var dbContext = new MemCheckDbContext(db))
            {
                var result = await new CreateLanguage(dbContext, new TestRoleChecker(user)).RunAsync(new CreateLanguage.Request(user, name), new TestLocalizer());
                Assert.AreEqual(name, result.Name);
                Assert.AreEqual(0, result.CardCount);
                createdId = result.Id;
            }
            using (var dbContext = new MemCheckDbContext(db))
            {
                var fromDb = await dbContext.CardLanguages.SingleAsync(l => l.Name == name);
                Assert.AreEqual(createdId, fromDb.Id);
            }
        }
    }
}

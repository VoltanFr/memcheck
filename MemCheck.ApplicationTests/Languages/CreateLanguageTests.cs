using MemCheck.Application.Helpers;
using MemCheck.Application.QueryValidation;
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
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new CreateLanguage(dbContext.AsCallContext()).RunAsync(new CreateLanguage.Request(Guid.Empty, RandomHelper.String())));
        }
        [TestMethod()]
        public async Task UserDoesNotExist()
        {
            using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new CreateLanguage(dbContext.AsCallContext()).RunAsync(new CreateLanguage.Request(Guid.NewGuid(), RandomHelper.String())));
        }
        [TestMethod()]
        public async Task UserIsNotAdmin()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new CreateLanguage(dbContext.AsCallContext()).RunAsync(new CreateLanguage.Request(user, RandomHelper.String())));
        }
        [TestMethod()]
        public async Task EmptyName()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new CreateLanguage(dbContext.AsCallContext()).RunAsync(new CreateLanguage.Request(user, "")));
        }
        [TestMethod()]
        public async Task NameNotTrimmed()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new CreateLanguage(dbContext.AsCallContext()).RunAsync(new CreateLanguage.Request(user, RandomHelper.String(QueryValidationHelper.LanguageMinNameLength) + '\t')));
        }
        [TestMethod()]
        public async Task NameTooShort()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new CreateLanguage(dbContext.AsCallContext()).RunAsync(new CreateLanguage.Request(user, RandomHelper.String(QueryValidationHelper.LanguageMinNameLength - 1))));
        }
        [TestMethod()]
        public async Task NameTooLong()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new CreateLanguage(dbContext.AsCallContext()).RunAsync(new CreateLanguage.Request(user, RandomHelper.String(QueryValidationHelper.LanguageMaxNameLength + 1))));
        }
        [TestMethod()]
        public async Task NameWithForbiddenChar()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new CreateLanguage(dbContext.AsCallContext()).RunAsync(new CreateLanguage.Request(user, "a<b")));
        }
        [TestMethod()]
        public async Task AlreadyExists()
        {
            var name = RandomHelper.String();
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            using (var dbContext = new MemCheckDbContext(db))
                await new CreateLanguage(dbContext.AsCallContext(new TestRoleChecker(user))).RunAsync(new CreateLanguage.Request(user, name));
            using (var dbContext = new MemCheckDbContext(db))
                await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new CreateLanguage(dbContext.AsCallContext(new TestRoleChecker(user))).RunAsync(new CreateLanguage.Request(user, name)));
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
                var result = await new CreateLanguage(dbContext.AsCallContext(new TestRoleChecker(user))).RunAsync(new CreateLanguage.Request(user, name));
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

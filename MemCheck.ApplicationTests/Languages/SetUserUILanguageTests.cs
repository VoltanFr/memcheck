using MemCheck.Application.Tests.Helpers;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Languages
{
    [TestClass()]
    public class SetUserUILanguageTests
    {
        [TestMethod()]
        public async Task UserNotLoggedIn()
        {
            using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new SetUserUILanguage(dbContext).RunAsync(new SetUserUILanguage.Request(Guid.Empty, RandomHelper.CultureName())));
        }
        [TestMethod()]
        public async Task UserDoesNotExist()
        {
            using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new SetUserUILanguage(dbContext).RunAsync(new SetUserUILanguage.Request(Guid.NewGuid(), RandomHelper.CultureName())));
        }
        [TestMethod()]
        public async Task NameTooShort()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new SetUserUILanguage(dbContext).RunAsync(new SetUserUILanguage.Request(user, RandomHelper.String(SetUserUILanguage.Request.MinNameLength - 1))));
        }
        [TestMethod()]
        public async Task NameTooLong()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new SetUserUILanguage(dbContext).RunAsync(new SetUserUILanguage.Request(user, RandomHelper.String(SetUserUILanguage.Request.MaxNameLength + 1))));
        }
        [TestMethod()]
        public async Task NameNotTrimmedAtBegining()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new SetUserUILanguage(dbContext).RunAsync(new SetUserUILanguage.Request(user, '\t' + RandomHelper.CultureName())));
        }
        [TestMethod()]
        public async Task NameNotTrimmedAtEnd()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new SetUserUILanguage(dbContext).RunAsync(new SetUserUILanguage.Request(user, RandomHelper.CultureName() + ' ')));
        }
        [TestMethod()]
        public async Task Success()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var name = RandomHelper.CultureName();
            using (var dbContext = new MemCheckDbContext(db))
                await new SetUserUILanguage(dbContext).RunAsync(new SetUserUILanguage.Request(user, name));
            using (var dbContext = new MemCheckDbContext(db))
                Assert.AreEqual(name, dbContext.Users.Single(u => u.Id == user).UILanguage);
        }
    }
}

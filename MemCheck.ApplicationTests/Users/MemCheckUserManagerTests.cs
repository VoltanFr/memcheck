using MemCheck.Application.Helpers;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Users
{
    [TestClass()]
    public class MemCheckUserManagerTests
    {
        [TestMethod()]
        public async Task NameTooLong()
        {
            var db = DbHelper.GetEmptyTestDB();

            using (var dbContext = new MemCheckDbContext(db))
            {
                using var userManager = UserHelper.GetUserManager(dbContext);

                var user = UserHelper.Create(userName: RandomHelper.String(MemCheckUserManager.MaxUserNameLength + 1));
                var identityResult = await userManager.CreateAsync(user);
                Assert.IsFalse(identityResult.Succeeded);
                Assert.AreEqual(MemCheckUserManager.BadUserNameLengthErrorCode, identityResult.Errors.Single().Code);
            }

            using (var dbContext = new MemCheckDbContext(db))
                Assert.IsFalse(await dbContext.Users.AnyAsync());
        }
        [TestMethod()]
        public async Task NameTooShort()
        {
            var db = DbHelper.GetEmptyTestDB();

            using (var dbContext = new MemCheckDbContext(db))
            {
                using var userManager = UserHelper.GetUserManager(dbContext);

                var user = UserHelper.Create(userName: RandomHelper.String(MemCheckUserManager.MinUserNameLength - 1));
                var identityResult = await userManager.CreateAsync(user);
                Assert.IsFalse(identityResult.Succeeded);
                Assert.AreEqual(MemCheckUserManager.BadUserNameLengthErrorCode, identityResult.Errors.Single().Code);
            }

            using (var dbContext = new MemCheckDbContext(db))
                Assert.IsFalse(await dbContext.Users.AnyAsync());
        }
        [TestMethod()]
        public async Task DeleteMustFail()
        {
            var db = DbHelper.GetEmptyTestDB();
            var userName = RandomHelper.String();
            var userId = await UserHelper.CreateInDbAsync(db, userName: userName);

            using (var dbContext = new MemCheckDbContext(db))
            {
                var user = await dbContext.Users.SingleAsync(u => u.Id == userId);

                using var userManager = UserHelper.GetUserManager(dbContext);
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await userManager.DeleteAsync(user));
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var user = await dbContext.Users.SingleAsync();
                Assert.AreEqual(userName, user.UserName);
            }
        }
        [TestMethod()]
        public async Task RegistrationDate()
        {
            var db = DbHelper.GetEmptyTestDB();

            using (var dbContext = new MemCheckDbContext(db))
            {
                using var userManager = UserHelper.GetUserManager(dbContext);
                var identityResult = await userManager.CreateAsync(UserHelper.Create());
                Assert.IsTrue(identityResult.Succeeded);
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var user = await dbContext.Users.SingleAsync();
                Assert.IsTrue(DateTime.UtcNow - user.RegistrationUtcDate < TimeSpan.FromMinutes(10));
            }
        }
        [TestMethod()]
        public async Task DeckCreated()
        {
            var db = DbHelper.GetEmptyTestDB();

            using (var dbContext = new MemCheckDbContext(db))
            {
                using var userManager = UserHelper.GetUserManager(dbContext);
                var identityResult = await userManager.CreateAsync(UserHelper.Create());
                Assert.IsTrue(identityResult.Succeeded);
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var user = await dbContext.Users.SingleAsync();
                var deck = await dbContext.Decks.SingleAsync();
                Assert.AreEqual(user.Id, deck.Owner.Id);
            }
        }
    }
}

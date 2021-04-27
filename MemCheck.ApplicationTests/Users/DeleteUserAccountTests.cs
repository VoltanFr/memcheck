using MemCheck.Application.Tests.Helpers;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Users
{
    [TestClass()]
    public class DeleteUserAccountTests
    {
        [TestMethod()]
        public async Task UserNotLoggedIn()
        {
            var db = DbHelper.GetEmptyTestDB();
            var userToDelete = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new DeleteUserAccount(dbContext, new TestRoleChecker()).RunAsync(new DeleteUserAccount.Request(Guid.Empty, userToDelete)));
        }
        [TestMethod()]
        public async Task LoggedUserDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var userToDelete = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new DeleteUserAccount(dbContext, new TestRoleChecker()).RunAsync(new DeleteUserAccount.Request(Guid.NewGuid(), userToDelete)));
        }
        [TestMethod()]
        public async Task LoggedUserIsNotAdmin()
        {
            var db = DbHelper.GetEmptyTestDB();
            var loggedUser = await UserHelper.CreateInDbAsync(db);
            var userToDelete = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new DeleteUserAccount(dbContext, new TestRoleChecker()).RunAsync(new DeleteUserAccount.Request(loggedUser, userToDelete)));
        }
        [TestMethod()]
        public async Task UserToDeleteDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var loggedUser = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new DeleteUserAccount(dbContext, new TestRoleChecker(loggedUser)).RunAsync(new DeleteUserAccount.Request(loggedUser, Guid.NewGuid())));
        }
        [TestMethod()]
        public async Task UserToDeleteIsAdmin()
        {
            var db = DbHelper.GetEmptyTestDB();
            var loggedUser = await UserHelper.CreateInDbAsync(db);
            var userToDelete = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new DeleteUserAccount(dbContext, new TestRoleChecker(loggedUser, userToDelete)).RunAsync(new DeleteUserAccount.Request(loggedUser, userToDelete)));
        }
        [TestMethod()]
        public async Task UserAccountGetsAnonymized()
        {
            var db = DbHelper.GetEmptyTestDB();
            var loggedUser = await UserHelper.CreateInDbAsync(db);

            var userToDeleteName = RandomHelper.String();
            var userToDeleteEmail = RandomHelper.String();
            var userToDelete = await UserHelper.CreateInDbAsync(db, userName: userToDeleteName, userEMail: userToDeleteEmail);

            using (var dbContext = new MemCheckDbContext(db))
                await new DeleteUserAccount(dbContext, new TestRoleChecker(loggedUser)).RunAsync(new DeleteUserAccount.Request(loggedUser, userToDelete));

            using (var dbContext = new MemCheckDbContext(db))
            {
                var deletedUser = await dbContext.Users.SingleAsync(u => u.Id == userToDelete);
                Assert.AreEqual(DeleteUserAccount.DeletedUserName, deletedUser.UserName);
                Assert.AreEqual(DeleteUserAccount.DeletedUserEmail, deletedUser.Email);
                Assert.IsFalse(deletedUser.EmailConfirmed);
                Assert.IsTrue(deletedUser.LockoutEnabled);
                Assert.AreEqual(DateTime.MaxValue, deletedUser.LockoutEnd);
            }
        }
    }
}

using MemCheck.Application.QueryValidation;
using MemCheck.Application.Tests.Helpers;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Users
{
    [TestClass()]
    public class GetAllUsersTests
    {
        [TestMethod()]
        public async Task UserNotLoggedIn()
        {
            using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetAllUsers(dbContext, TestRoleChecker.TrueForAdmin).RunAsync(new GetAllUsers.Request(Guid.Empty, 1, 0, "")));
        }
        [TestMethod()]
        public async Task UserDoesNotExist()
        {
            using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetAllUsers(dbContext, TestRoleChecker.TrueForAdmin).RunAsync(new GetAllUsers.Request(Guid.NewGuid(), 1, 0, "")));
        }
        [TestMethod()]
        public async Task UserIsNotAdmin()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetAllUsers(dbContext, TestRoleChecker.FalseForAdmin).RunAsync(new GetAllUsers.Request(user, 1, 0, "")));
        }
        [TestMethod()]
        public async Task Page0()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetAllUsers(dbContext, TestRoleChecker.TrueForAdmin).RunAsync(new GetAllUsers.Request(user, 1, 0, "")));
        }
        [TestMethod()]
        public async Task PageSize0()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetAllUsers(dbContext, TestRoleChecker.TrueForAdmin).RunAsync(new GetAllUsers.Request(user, 0, 0, "")));
        }
        [TestMethod()]
        public async Task PageSizeTooBig()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetAllUsers(dbContext, TestRoleChecker.TrueForAdmin).RunAsync(new GetAllUsers.Request(user, GetAllUsers.Request.MaxPageSize + 1, 0, "")));
        }
        [TestMethod()]
        public async Task OnlyUser()
        {
            var db = DbHelper.GetEmptyTestDB();
            var userName = RandomHelper.String();
            var userEMail = RandomHelper.String();
            var user = await UserHelper.CreateInDbAsync(db, userName: userName, userEMail: userEMail);
            using var dbContext = new MemCheckDbContext(db);
            var loaded = await new GetAllUsers(dbContext, TestRoleChecker.TrueForAdmin).RunAsync(new GetAllUsers.Request(user, 10, 1, ""));
            Assert.AreEqual(1, loaded.TotalCount);
            Assert.AreEqual(1, loaded.PageCount);
            var loadedUsers = loaded.Users.ToArray();
            Assert.AreEqual(1, loadedUsers.Length);
            Assert.AreEqual(userName, loadedUsers[0].UserName);
            Assert.AreEqual(IRoleChecker.AdminRoleName, loadedUsers[0].Roles);
            Assert.AreEqual(userEMail, loadedUsers[0].Email);
            Assert.AreEqual(0, loadedUsers[0].NotifInterval);
            Assert.AreEqual(DateTime.MinValue, loadedUsers[0].LastNotifUtcDate);
        }
        [TestMethod()]
        public async Task Paging()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user1Name = "a" + RandomHelper.String();
            var user1 = await UserHelper.CreateInDbAsync(db, userName: user1Name);
            var user2Name = "b" + RandomHelper.String();
            await UserHelper.CreateInDbAsync(db, userName: user2Name);

            using var dbContext = new MemCheckDbContext(db);

            var loaded = await new GetAllUsers(dbContext, TestRoleChecker.TrueForAdmin).RunAsync(new GetAllUsers.Request(user1, 1, 1, ""));
            Assert.AreEqual(2, loaded.TotalCount);
            Assert.AreEqual(2, loaded.PageCount);
            Assert.AreEqual(user1Name, loaded.Users.Single().UserName);

            loaded = await new GetAllUsers(dbContext, TestRoleChecker.TrueForAdmin).RunAsync(new GetAllUsers.Request(user1, 1, 2, ""));
            Assert.AreEqual(2, loaded.TotalCount);
            Assert.AreEqual(2, loaded.PageCount);
            Assert.AreEqual(user2Name, loaded.Users.Single().UserName);

            loaded = await new GetAllUsers(dbContext, TestRoleChecker.TrueForAdmin).RunAsync(new GetAllUsers.Request(user1, 1, 3, ""));
            Assert.AreEqual(2, loaded.TotalCount);
            Assert.AreEqual(2, loaded.PageCount);
            Assert.IsFalse(loaded.Users.Any());
        }
        [TestMethod()]
        public async Task Filtering()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user1Name = "a" + RandomHelper.String();
            var user1 = await UserHelper.CreateInDbAsync(db, userName: user1Name);
            var user2Name = "b" + RandomHelper.String();
            await UserHelper.CreateInDbAsync(db, userName: user2Name);

            using var dbContext = new MemCheckDbContext(db);

            var loaded = await new GetAllUsers(dbContext, TestRoleChecker.TrueForAdmin).RunAsync(new GetAllUsers.Request(user1, 1, 1, user1Name.ToLowerInvariant()));
            Assert.AreEqual(1, loaded.TotalCount);
            Assert.AreEqual(1, loaded.PageCount);
            Assert.AreEqual(user1Name, loaded.Users.Single().UserName);

            loaded = await new GetAllUsers(dbContext, TestRoleChecker.TrueForAdmin).RunAsync(new GetAllUsers.Request(user1, 1, 1, user2Name.ToUpperInvariant()));
            Assert.AreEqual(1, loaded.TotalCount);
            Assert.AreEqual(1, loaded.PageCount);
            Assert.AreEqual(user2Name, loaded.Users.Single().UserName);

            loaded = await new GetAllUsers(dbContext, TestRoleChecker.TrueForAdmin).RunAsync(new GetAllUsers.Request(user1, 1, 1, RandomHelper.String()));
            Assert.AreEqual(0, loaded.TotalCount);
            Assert.AreEqual(0, loaded.PageCount);
            Assert.IsFalse(loaded.Users.Any());
        }
    }
}

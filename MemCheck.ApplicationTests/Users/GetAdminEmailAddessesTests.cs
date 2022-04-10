using MemCheck.Application.Helpers;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Users
{
    [TestClass()]
    public class GetAdminEmailAddessesTests
    {
        [TestMethod()]
        public async Task UserNotLoggedIn()
        {
            using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetAdminEmailAddesses(dbContext.AsCallContext()).RunAsync(new GetAdminEmailAddesses.Request(Guid.Empty)));
        }
        [TestMethod()]
        public async Task UserDoesNotExist()
        {
            using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetAdminEmailAddesses(dbContext.AsCallContext()).RunAsync(new GetAdminEmailAddesses.Request(Guid.NewGuid())));
        }
        [TestMethod()]
        public async Task UserIsNotAdmin()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetAdminEmailAddesses(dbContext.AsCallContext()).RunAsync(new GetAdminEmailAddesses.Request(user)));
        }
        [TestMethod()]
        public async Task OnlyOtherUserIsNotAdmin()
        {
            var db = DbHelper.GetEmptyTestDB();

            var adminUserName = RandomHelper.String();
            var adminUserEMail = RandomHelper.String();
            var adminUser = await UserHelper.CreateInDbAsync(db, userName: adminUserName, userEMail: adminUserEMail);

            await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            var loaded = await new GetAdminEmailAddesses(dbContext.AsCallContext(new TestRoleChecker(adminUser))).RunAsync(new GetAdminEmailAddesses.Request(adminUser));
            Assert.AreEqual(1, loaded.Users.Count());
            Assert.AreEqual(adminUserName, loaded.Users.Single().Name);
            Assert.AreEqual(adminUserEMail, loaded.Users.Single().Email);
        }
        [TestMethod()]
        public async Task OnlyUserIsAdmin()
        {
            var db = DbHelper.GetEmptyTestDB();
            var userName = RandomHelper.String();
            var userEMail = RandomHelper.String();
            var user = await UserHelper.CreateInDbAsync(db, userName: userName, userEMail: userEMail);
            using var dbContext = new MemCheckDbContext(db);
            var loaded = await new GetAdminEmailAddesses(dbContext.AsCallContext(new TestRoleChecker(user))).RunAsync(new GetAdminEmailAddesses.Request(user));
            Assert.AreEqual(1, loaded.Users.Count());
            Assert.AreEqual(userName, loaded.Users.Single().Name);
            Assert.AreEqual(userEMail, loaded.Users.Single().Email);
        }
        [TestMethod()]
        public async Task FourUsers()
        {
            var db = DbHelper.GetEmptyTestDB();

            var admin1UserName = RandomHelper.String();
            var admin1UserEMail = RandomHelper.String();
            var admin1User = await UserHelper.CreateInDbAsync(db, userName: admin1UserName, userEMail: admin1UserEMail);

            var admin2UserName = RandomHelper.String();
            var admin2UserEMail = RandomHelper.String();
            var admin2User = await UserHelper.CreateInDbAsync(db, userName: admin2UserName, userEMail: admin2UserEMail);

            var nonAdminUser1 = await UserHelper.CreateInDbAsync(db);
            var nonAdminUser2 = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            var loaded = await new GetAdminEmailAddesses(dbContext.AsCallContext(new TestRoleChecker(admin1User, admin2User))).RunAsync(new GetAdminEmailAddesses.Request(admin1User));
            Assert.AreEqual(2, loaded.Users.Count());

            var resultUser1 = loaded.Users.Single(user => user.Name == admin1UserName);
            Assert.AreEqual(admin1UserEMail, resultUser1.Email);

            var resultUser2 = loaded.Users.Single(user => user.Name == admin2UserName);
            Assert.AreEqual(admin2UserEMail, resultUser2.Email);
        }
    }
}

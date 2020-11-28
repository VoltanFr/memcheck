using Microsoft.VisualStudio.TestTools.UnitTesting;
using MemCheck.Database;
using System.Linq;
using System;
using MemCheck.Application.Notifying;
using System.Threading.Tasks;

namespace MemCheck.Application.Tests.Notifying
{
    [TestClass()]
    public class UsersToNotifyGetterTests
    {
        [TestMethod()]
        public void TestRun_EmptyDB()
        {
            var testDB = DbServices.GetEmptyTestDB(typeof(UserCardVersionsNotifierTests));

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var users = new UsersToNotifyGetter(dbContext).Run();
                Assert.AreEqual(0, users.Length);
            }
        }
        [TestMethod()]
        public async Task TestRun_DBWithUserNotToNotify()
        {
            var testDB = DbServices.GetEmptyTestDB(typeof(UserCardVersionsNotifierTests));

            await UserHelper.CreateUserAsync(testDB);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var users = new UsersToNotifyGetter(dbContext).Run();
                Assert.AreEqual(0, users.Length);
            }
        }
        [TestMethod()]
        public async Task TestRun_DBWithOneUserToNotify()
        {
            var testDB = DbServices.GetEmptyTestDB(typeof(UserCardVersionsNotifierTests));

            await UserHelper.CreateUserAsync(testDB, 1, new DateTime(2020, 11, 10));
            await UserHelper.CreateUserAsync(testDB, 10, new DateTime(2020, 11, 1));
            var userToNotify = await UserHelper.CreateUserAsync(testDB, 9, new DateTime(2020, 11, 1));
            await UserHelper.CreateUserAsync(testDB);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var users = new UsersToNotifyGetter(dbContext).Run(new DateTime(2020, 11, 10));
                Assert.AreEqual(1, users.Length);
                Assert.AreEqual(userToNotify.Id, users[0].Id);
            }
        }
        [TestMethod()]
        public async Task TestRun_DBWithTwoUsersToNotify()
        {
            var testDB = DbServices.GetEmptyTestDB(typeof(UserCardVersionsNotifierTests));

            var userToNotify1 = await UserHelper.CreateUserAsync(testDB, 1, new DateTime(2030, 10, 19));
            await UserHelper.CreateUserAsync(testDB);
            await UserHelper.CreateUserAsync(testDB, 2, new DateTime(2030, 10, 19));
            var userToNotify2 = await UserHelper.CreateUserAsync(testDB, 30, new DateTime(2030, 9, 20));

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var users = new UsersToNotifyGetter(dbContext).Run(new DateTime(2030, 10, 20));
                Assert.AreEqual(2, users.Length);
                Assert.IsTrue(users.Any(u => u.Id == userToNotify1.Id));
                Assert.IsTrue(users.Any(u => u.Id == userToNotify2.Id));
            }
        }
    }
}
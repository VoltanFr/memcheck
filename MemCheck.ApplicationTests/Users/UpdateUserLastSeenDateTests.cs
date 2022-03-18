using MemCheck.Application.Tests.Helpers;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Users
{
    [TestClass()]
    public class UpdateUserLastSeenDateTests
    {
        [TestMethod()]
        public async Task UserNotLoggedIn()
        {
            var db = DbHelper.GetEmptyTestDB();
            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new UpdateUserLastSeenDate(dbContext.AsCallContext()).RunAsync(new UpdateUserLastSeenDate.Request(Guid.Empty)));
        }
        [TestMethod()]
        public async Task UserDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new UpdateUserLastSeenDate(dbContext.AsCallContext()).RunAsync(new UpdateUserLastSeenDate.Request(RandomHelper.Guid())));
        }
        [TestMethod()]
        public async Task UserIsDeleted()
        {
            var db = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(db);
            var lastSeenDate = RandomHelper.Date();

            using (var dbContext = new MemCheckDbContext(db))
            using (var userManager = UserHelper.GetUserManager(dbContext))
                await new DeleteUserAccount(dbContext.AsCallContext(), userManager).RunAsync(new DeleteUserAccount.Request(userId,userId));

            using (var dbContext = new MemCheckDbContext(db))
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new UpdateUserLastSeenDate(dbContext.AsCallContext()).RunAsync(new UpdateUserLastSeenDate.Request(userId)));
        }
        [TestMethod()]
        public async Task Success()
        {
            var db = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(db);
            var lastSeenDate = RandomHelper.Date();

            using (var dbContext = new MemCheckDbContext(db))
                await new UpdateUserLastSeenDate(dbContext.AsCallContext(), lastSeenDate).RunAsync(new UpdateUserLastSeenDate.Request(userId));

            using (var dbContext = new MemCheckDbContext(db))
            {
                var user=dbContext.Users.Single(user => user.Id == userId);
                Assert.AreEqual(lastSeenDate, user.LastSeenUtcDate);
            }
        }
    }
}

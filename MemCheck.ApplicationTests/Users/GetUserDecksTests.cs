using MemCheck.Application.Helpers;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Users
{
    [TestClass()]
    public class GetUsersTests
    {
        [TestMethod()]
        public async Task OnlyUser()
        {
            var db = DbHelper.GetEmptyTestDB();
            var name = RandomHelper.String();
            var user = await UserHelper.CreateInDbAsync(db, userName: name);
            using var dbContext = new MemCheckDbContext(db);
            var result = (await new GetUsers(dbContext.AsCallContext()).RunAsync(new GetUsers.Request())).Single();
            Assert.AreEqual(user, result.UserId);
            Assert.AreEqual(name, result.UserName);
        }
        [TestMethod()]
        public async Task TwoUsers()
        {
            var db = DbHelper.GetEmptyTestDB();
            var name1 = RandomHelper.String();
            var user1 = await UserHelper.CreateInDbAsync(db, userName: name1);
            var name2 = RandomHelper.String();
            var user2 = await UserHelper.CreateInDbAsync(db, userName: name2);
            using var dbContext = new MemCheckDbContext(db);
            var result = await new GetUsers(dbContext.AsCallContext()).RunAsync(new GetUsers.Request());
            Assert.AreEqual(name1, result.Single(r => r.UserId == user1).UserName);
            Assert.AreEqual(name2, result.Single(r => r.UserId == user2).UserName);
        }
    }
}

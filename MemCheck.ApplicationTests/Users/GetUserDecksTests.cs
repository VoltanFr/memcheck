using MemCheck.Application.Helpers;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Users;

[TestClass()]
public class GetUsersTests
{
    [TestMethod()]
    public async Task OnlyUser()
    {
        var db = DbHelper.GetEmptyTestDB();

        var user = await UserHelper.CreateUserInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        var result = (await new GetUsers(dbContext.AsCallContext()).RunAsync(new GetUsers.Request())).Single();
        Assert.AreEqual(user.Id, result.UserId);
        Assert.AreEqual(user.UserName, result.UserName);
    }
    [TestMethod()]
    public async Task TwoUsers()
    {
        var db = DbHelper.GetEmptyTestDB();

        var user1 = await UserHelper.CreateUserInDbAsync(db);
        var user2 = await UserHelper.CreateUserInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        var result = await new GetUsers(dbContext.AsCallContext()).RunAsync(new GetUsers.Request());
        Assert.AreEqual(user1.UserName, result.Single(r => r.UserId == user1.Id).UserName);
        Assert.AreEqual(user2.UserName, result.Single(r => r.UserId == user2.Id).UserName);
    }
}

using MemCheck.Application.Helpers;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Users;

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

        var adminUser = await UserHelper.CreateUserInDbAsync(db);

        await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        var loaded = await new GetAdminEmailAddesses(dbContext.AsCallContext(new TestRoleChecker(adminUser.Id))).RunAsync(new GetAdminEmailAddesses.Request(adminUser.Id));
        Assert.AreEqual(1, loaded.Users.Count());
        Assert.AreEqual(adminUser.UserName, loaded.Users.Single().Name);
        Assert.AreEqual(adminUser.Email, loaded.Users.Single().Email);
    }
    [TestMethod()]
    public async Task OnlyUserIsAdmin()
    {
        var db = DbHelper.GetEmptyTestDB();

        var user = await UserHelper.CreateUserInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        var loaded = await new GetAdminEmailAddesses(dbContext.AsCallContext(new TestRoleChecker(user.Id))).RunAsync(new GetAdminEmailAddesses.Request(user.Id));
        Assert.AreEqual(1, loaded.Users.Count());
        Assert.AreEqual(user.UserName, loaded.Users.Single().Name);
        Assert.AreEqual(user.Email, loaded.Users.Single().Email);
    }
    [TestMethod()]
    public async Task FourUsers()
    {
        var db = DbHelper.GetEmptyTestDB();

        var admin1User = await UserHelper.CreateUserInDbAsync(db);
        var admin2User = await UserHelper.CreateUserInDbAsync(db);

        var nonAdminUser1 = await UserHelper.CreateUserInDbAsync(db);
        var nonAdminUser2 = await UserHelper.CreateUserInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        var loaded = await new GetAdminEmailAddesses(dbContext.AsCallContext(new TestRoleChecker(admin1User.Id, admin2User.Id))).RunAsync(new GetAdminEmailAddesses.Request(admin1User.Id));
        Assert.AreEqual(2, loaded.Users.Count());

        var resultUser1 = loaded.Users.Single(user => user.Name == admin1User.UserName);
        Assert.AreEqual(admin1User.Email, resultUser1.Email);

        var resultUser2 = loaded.Users.Single(user => user.Name == admin2User.UserName);
        Assert.AreEqual(admin2User.Email, resultUser2.Email);
    }
}

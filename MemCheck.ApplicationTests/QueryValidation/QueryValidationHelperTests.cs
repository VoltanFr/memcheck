using MemCheck.Application.Helpers;
using MemCheck.Application.Users;
using MemCheck.Basics;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace MemCheck.Application.QueryValidation;

[TestClass()]
public class QueryValidationHelperTests
{
    [TestMethod()]
    public async Task CheckUserExists_NoUser()
    {
        var db = DbHelper.GetEmptyTestDB();
        await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<NonexistentUserException>(async () => await QueryValidationHelper.CheckUserExistsAsync(dbContext, RandomHelper.Guid()));
    }
    [TestMethod()]
    public async Task CheckUserExists_UserDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<NonexistentUserException>(async () => await QueryValidationHelper.CheckUserExistsAsync(dbContext, RandomHelper.Guid()));
    }
    [TestMethod()]
    public async Task CheckUserExists_UserExists_Single()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        await QueryValidationHelper.CheckUserExistsAsync(dbContext, userId);
    }
    [TestMethod()]
    public async Task CheckUserExists_UserExists_Multiple()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        await 10.TimesAsync(async () => { await UserHelper.CreateInDbAsync(db); });

        using var dbContext = new MemCheckDbContext(db);
        await QueryValidationHelper.CheckUserExistsAsync(dbContext, userId);
    }
    [TestMethod()]
    public async Task CheckUserExists_UserDeleted_Single()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);

        using (var dbContext = new MemCheckDbContext(db))
        {
            using var userManager = UserHelper.GetUserManager(dbContext);
            await new DeleteUserAccount(dbContext.AsCallContext(), userManager).RunAsync(new DeleteUserAccount.Request(userId, userId));
        }

        using (var dbContext = new MemCheckDbContext(db))
            await Assert.ThrowsExceptionAsync<NonexistentUserException>(async () => await QueryValidationHelper.CheckUserExistsAsync(dbContext, RandomHelper.Guid()));
    }
    [TestMethod()]
    public async Task CheckUserExists_UserDeleted_Multiple()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        await 10.TimesAsync(async () => { await UserHelper.CreateInDbAsync(db); });

        using (var dbContext = new MemCheckDbContext(db))
        {
            using var userManager = UserHelper.GetUserManager(dbContext);
            await new DeleteUserAccount(dbContext.AsCallContext(), userManager).RunAsync(new DeleteUserAccount.Request(userId, userId));
        }

        using (var dbContext = new MemCheckDbContext(db))
            await Assert.ThrowsExceptionAsync<NonexistentUserException>(async () => await QueryValidationHelper.CheckUserExistsAsync(dbContext, RandomHelper.Guid()));
    }
    [TestMethod()]
    public async Task CheckUserExists_ComplexCase()
    {
        var db = DbHelper.GetEmptyTestDB();
        await 5.TimesAsync(async () => { await UserHelper.CreateInDbAsync(db); });
        var existingNonAdminUserId = await UserHelper.CreateInDbAsync(db);
        await 5.TimesAsync(async () => { await UserHelper.CreateInDbAsync(db); });
        var userIdDeletedByHimself = await UserHelper.CreateInDbAsync(db);
        await 5.TimesAsync(async () => { await UserHelper.CreateInDbAsync(db); });
        var userIdDeletedByAdmin = await UserHelper.CreateInDbAsync(db);
        await 5.TimesAsync(async () => { await UserHelper.CreateInDbAsync(db); });
        var existingAdminUserId = await UserHelper.CreateInDbAsync(db);
        await 5.TimesAsync(async () => { await UserHelper.CreateInDbAsync(db); });

        using (var dbContext = new MemCheckDbContext(db))
        {
            using var userManager = UserHelper.GetUserManager(dbContext);
            await new DeleteUserAccount(dbContext.AsCallContext(), userManager).RunAsync(new DeleteUserAccount.Request(userIdDeletedByHimself, userIdDeletedByHimself));
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var callContext = dbContext.AsCallContext(new TestRoleChecker(existingAdminUserId));
            using var userManager = UserHelper.GetUserManager(dbContext);
            await new DeleteUserAccount(callContext, userManager).RunAsync(new DeleteUserAccount.Request(existingAdminUserId, userIdDeletedByAdmin));
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            await QueryValidationHelper.CheckUserExistsAsync(dbContext, existingNonAdminUserId);
            await QueryValidationHelper.CheckUserExistsAsync(dbContext, existingAdminUserId);
            await Assert.ThrowsExceptionAsync<NonexistentUserException>(async () => await QueryValidationHelper.CheckUserExistsAsync(dbContext, userIdDeletedByHimself));
            await Assert.ThrowsExceptionAsync<NonexistentUserException>(async () => await QueryValidationHelper.CheckUserExistsAsync(dbContext, userIdDeletedByAdmin));
        }
    }
}

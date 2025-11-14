using MemCheck.Application.Helpers;
using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Languages;

[TestClass()]
public class SetUserUILanguageTests
{
    [TestMethod()]
    public async Task UserNotLoggedIn()
    {
        using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
        await Assert.ThrowsExactlyAsync<NonexistentUserException>(async () => await new SetUserUILanguage(dbContext.AsCallContext()).RunAsync(new SetUserUILanguage.Request(Guid.Empty, MemCheckSupportedCultures.French)));
    }
    [TestMethod()]
    public async Task UserDoesNotExist()
    {
        using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
        await Assert.ThrowsExactlyAsync<NonexistentUserException>(async () => await new SetUserUILanguage(dbContext.AsCallContext()).RunAsync(new SetUserUILanguage.Request(Guid.NewGuid(), MemCheckSupportedCultures.French)));
    }
    [TestMethod()]
    public async Task Success()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        using (var dbContext = new MemCheckDbContext(db))
            await new SetUserUILanguage(dbContext.AsCallContext()).RunAsync(new SetUserUILanguage.Request(user, MemCheckSupportedCultures.French));
        using (var dbContext = new MemCheckDbContext(db))
            Assert.AreEqual(MemCheckSupportedCultures.IdFromCulture(MemCheckSupportedCultures.French), dbContext.Users.Single(u => u.Id == user).UILanguage);
    }
}

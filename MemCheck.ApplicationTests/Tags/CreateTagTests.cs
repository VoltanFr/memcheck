using MemCheck.Application.Helpers;
using MemCheck.Application.QueryValidation;
using MemCheck.Application.Users;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Tags;

[TestClass()]
public class CreateTagTests
{
    [TestMethod()]
    public async Task EmptyName()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(testDB);
        using var dbContext = new MemCheckDbContext(testDB);
        await Assert.ThrowsExactlyAsync<RequestInputException>(async () => await new CreateTag(dbContext.AsCallContext()).RunAsync(new CreateTag.Request(user.Id, "", "", RandomHelper.String())));
    }
    [TestMethod()]
    public async Task NameNotTrimmed()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(testDB);
        using var dbContext = new MemCheckDbContext(testDB);
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await new CreateTag(dbContext.AsCallContext()).RunAsync(new CreateTag.Request(user.Id, RandomHelper.String(Tag.MinNameLength) + '\t', "", RandomHelper.String())));
    }
    [TestMethod()]
    public async Task DescriptionNotTrimmed()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(testDB);
        using var dbContext = new MemCheckDbContext(testDB);
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await new CreateTag(dbContext.AsCallContext()).RunAsync(new CreateTag.Request(user.Id, RandomHelper.String(), RandomHelper.String() + '\t', RandomHelper.String())));
    }
    [TestMethod()]
    public async Task NameTooShort()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(testDB);
        using var dbContext = new MemCheckDbContext(testDB);
        await Assert.ThrowsExactlyAsync<RequestInputException>(async () => await new CreateTag(dbContext.AsCallContext()).RunAsync(new CreateTag.Request(user.Id, RandomHelper.String(Tag.MinNameLength - 1), "", RandomHelper.String())));
    }
    [TestMethod()]
    public async Task NameTooLong()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(testDB);
        using var dbContext = new MemCheckDbContext(testDB);
        await Assert.ThrowsExactlyAsync<RequestInputException>(async () => await new CreateTag(dbContext.AsCallContext()).RunAsync(new CreateTag.Request(user.Id, RandomHelper.String(Tag.MaxNameLength + 1), "", RandomHelper.String())));
    }
    [TestMethod()]
    public async Task DescriptionTooLong()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(testDB);
        using var dbContext = new MemCheckDbContext(testDB);
        await Assert.ThrowsExactlyAsync<RequestInputException>(async () => await new CreateTag(dbContext.AsCallContext()).RunAsync(new CreateTag.Request(user.Id, RandomHelper.String(), RandomHelper.String(Tag.MaxDescriptionLength + 1), RandomHelper.String())));
    }
    [TestMethod()]
    public async Task NameWithForbiddenChar()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(testDB);
        using var dbContext = new MemCheckDbContext(testDB);
        await Assert.ThrowsExactlyAsync<RequestInputException>(async () => await new CreateTag(dbContext.AsCallContext()).RunAsync(new CreateTag.Request(user.Id, "a<b", "", RandomHelper.String())));
    }
    [TestMethod()]
    public async Task AlreadyExists()
    {
        var name = RandomHelper.String();
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        using (var dbContext = new MemCheckDbContext(db))
            await new CreateTag(dbContext.AsCallContext()).RunAsync(new CreateTag.Request(user.Id, name, "", RandomHelper.String()));
        using (var dbContext = new MemCheckDbContext(db))
            await Assert.ThrowsExactlyAsync<RequestInputException>(async () => await new CreateTag(dbContext.AsCallContext()).RunAsync(new CreateTag.Request(user.Id, name, "", RandomHelper.String())));
    }
    [TestMethod()]
    public async Task Success()
    {
        var name = RandomHelper.String();
        var description = RandomHelper.String();
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var runDate = RandomHelper.Date();

        Guid tagId;
        using (var dbContext = new MemCheckDbContext(db))
        {
            var creator = new CreateTag(dbContext.AsCallContext(), runDate);
            var creationResult = await creator.RunAsync(new CreateTag.Request(user.Id, name, description, RandomHelper.String()));
            tagId = creationResult.TagId;
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var loaded = await new GetTag(dbContext.AsCallContext()).RunAsync(new GetTag.Request(tagId));
            Assert.AreEqual(name, loaded.TagName);
            Assert.AreEqual(description, loaded.Description);
            Assert.AreEqual(user.UserName, loaded.CreatingUserName);
            Assert.AreEqual(0, loaded.CardCount);
            Assert.AreEqual(runDate, loaded.VersionUtcDate);
        }
    }
    [TestMethod()]
    public async Task UserDeleted()
    {
        var db = DbHelper.GetEmptyTestDB();

        var user = await UserHelper.CreateUserInDbAsync(db);

        using (var dbContext = new MemCheckDbContext(db))
        using (var userManager = UserHelper.GetUserManager(dbContext))
        {
            var deleter = new DeleteUserAccount(dbContext.AsCallContext(), userManager);
            await deleter.RunAsync(new DeleteUserAccount.Request(user.Id, user.Id));
        }

        using (var dbContext = new MemCheckDbContext(db))
            await Assert.ThrowsExactlyAsync<NonexistentUserException>(async () => await new CreateTag(dbContext.AsCallContext()).RunAsync(new CreateTag.Request(user.Id, RandomHelper.String(), RandomHelper.String(), RandomHelper.String())));
    }
}

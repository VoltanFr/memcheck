using MemCheck.Application.Helpers;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Tags;

[TestClass()]
public class GetTagTests
{
    [TestMethod()]
    public async Task DoesNotExist()
    {
        using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await new GetTag(dbContext.AsCallContext()).RunAsync(new GetTag.Request(Guid.NewGuid())));
    }
    [TestMethod()]
    public async Task TagNotUsedInCards()
    {
        var db = DbHelper.GetEmptyTestDB();
        var tagName = RandomHelper.String();
        var description = RandomHelper.String();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var runDate = RandomHelper.Date();
        var tagId = await TagHelper.CreateAsync(db, user.Id, tagName, description, runDate);

        using var dbContext = new MemCheckDbContext(db);
        var loadedTag = await new GetTag(dbContext.AsCallContext()).RunAsync(new GetTag.Request(tagId));

        Assert.AreEqual(tagId, loadedTag.TagId);
        Assert.AreEqual(tagName, loadedTag.TagName);
        Assert.AreEqual(description, loadedTag.Description);
        Assert.AreEqual(user.UserName, loadedTag.CreatingUserName);
        Assert.AreEqual(0, loadedTag.CardCount);
        Assert.AreEqual(runDate, loadedTag.VersionUtcDate);
    }
    [TestMethod()]
    public async Task TagUsedInCards()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var otherTag = await TagHelper.CreateAsync(db, user);
        var tagName = RandomHelper.String();
        var description = RandomHelper.String();
        var tag = await TagHelper.CreateAsync(db, user, tagName, description);

        await CardHelper.CreateAsync(db, user.Id, tagIds: new[] { tag, otherTag });
        await CardHelper.CreateAsync(db, user.Id, tagIds: new[] { tag });
        await CardHelper.CreateAsync(db, user.Id);

        using var dbContext = new MemCheckDbContext(db);
        var loadedTag = await new GetTag(dbContext.AsCallContext()).RunAsync(new GetTag.Request(tag));

        Assert.AreEqual(tag, loadedTag.TagId);
        Assert.AreEqual(tagName, loadedTag.TagName);
        Assert.AreEqual(description, loadedTag.Description);
        Assert.AreEqual(user.UserName, loadedTag.CreatingUserName);
        Assert.AreEqual(2, loadedTag.CardCount);
    }
}

using MemCheck.Application.Helpers;
using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace MemCheck.Application.Images;

[TestClass()]
public class GetImageInfoFromNameTests
{
    [TestMethod()]
    public async Task NameEmpty()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var image = await ImageHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExactlyAsync<InvalidImageNameLengthException>(async () => await new GetImageInfoFromName(dbContext.AsCallContext()).RunAsync(new GetImageInfoFromName.Request("")));
    }
    [TestMethod()]
    public async Task NameNotTrimmed()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var image = await ImageHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExactlyAsync<ImageNameNotTrimmedException>(async () => await new GetImageInfoFromName(dbContext.AsCallContext()).RunAsync(new GetImageInfoFromName.Request(RandomHelper.String() + ' ')));
    }
    [TestMethod()]
    public async Task ImageDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var image = await ImageHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExactlyAsync<ImageNotFoundException>(async () => await new GetImageInfoFromName(dbContext.AsCallContext()).RunAsync(new GetImageInfoFromName.Request(RandomHelper.String())));
    }
    [TestMethod()]
    public async Task Success_ImageNotUsed()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var name = RandomHelper.String();
        var source = RandomHelper.String();
        await ImageHelper.CreateAsync(db, user, name: name, source: source);

        using var dbContext = new MemCheckDbContext(db);
        var loaded = await new GetImageInfoFromName(dbContext.AsCallContext()).RunAsync(new GetImageInfoFromName.Request(name));
        Assert.AreEqual(source, loaded.Source);
        Assert.AreEqual(0, loaded.CardCount);
    }
    [TestMethod()]
    public async Task Success_ImageUsed()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);

        var imageName = RandomHelper.String();
        await ImageHelper.CreateAsync(db, userId, name: imageName);

        await CardHelper.CreateAsync(db, userId, backSide: $"![Mnesios:{imageName}]");

        using var dbContext = new MemCheckDbContext(db);
        var loaded = await new GetImageInfoFromName(dbContext.AsCallContext()).RunAsync(new GetImageInfoFromName.Request(imageName));
        Assert.AreEqual(1, loaded.CardCount);
    }
    [TestMethod()]
    public async Task CaseInsensitivity()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        await ImageHelper.CreateAsync(db, user.Id, name: "ImageName");

        using var dbContext = new MemCheckDbContext(db);
        var loaded = await new GetImageInfoFromName(dbContext.AsCallContext()).RunAsync(new GetImageInfoFromName.Request("imagEname"));
        Assert.AreEqual(user.UserName, loaded.InitialVersionCreator);
    }
    [TestMethod()]
    public async Task ComplexCase()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);

        var image1Name = RandomHelper.String();
        await ImageHelper.CreateAsync(db, userId, name: image1Name);

        var image2Name = RandomHelper.String();
        await ImageHelper.CreateAsync(db, userId, name: image2Name);

        var image3Name = RandomHelper.String();
        await ImageHelper.CreateAsync(db, userId, name: image3Name);

        await CardHelper.CreateAsync(db, userId, backSide: $"![Mnesios:{image1Name}]");
        await CardHelper.CreateAsync(db, userId, backSide: $"![Mnesios:{image1Name}]![Mnesios:{image2Name}]");
        await CardHelper.CreateAsync(db, userId);

        using (var dbContext = new MemCheckDbContext(db))
        {
            var loaded = await new GetImageInfoFromName(dbContext.AsCallContext()).RunAsync(new GetImageInfoFromName.Request(image1Name));
            Assert.AreEqual(2, loaded.CardCount);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var loaded = await new GetImageInfoFromName(dbContext.AsCallContext()).RunAsync(new GetImageInfoFromName.Request(image2Name));
            Assert.AreEqual(1, loaded.CardCount);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var loaded = await new GetImageInfoFromName(dbContext.AsCallContext()).RunAsync(new GetImageInfoFromName.Request(image3Name));
            Assert.AreEqual(0, loaded.CardCount);
        }
    }
}

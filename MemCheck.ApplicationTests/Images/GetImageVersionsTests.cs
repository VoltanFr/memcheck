using MemCheck.Application.Helpers;
using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Images;

[TestClass()]
public class GetImageVersionsTests
{
    [TestMethod()]
    public async Task ImageDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var image = await ImageHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<ImageNotFoundException>(async () => await new GetImageVersions(dbContext.AsCallContext()).RunAsync(new GetImageVersions.Request(Guid.NewGuid())));
    }
    [TestMethod()]
    public async Task Versionning()
    {
        var db = DbHelper.GetEmptyTestDB();

        var user1 = await UserHelper.CreateUserInDbAsync(db);
        var initialVersionDate = RandomHelper.Date();
        var initialName = RandomHelper.String();
        var initialSource = RandomHelper.String();
        var initialVersionDescription = RandomHelper.String();
        var image = await ImageHelper.CreateAsync(db, user1.Id, lastChangeUtcDate: initialVersionDate, name: initialName, source: initialSource, versionDescription: initialVersionDescription);

        var user2 = await UserHelper.CreateUserInDbAsync(db);
        var firstVersionDate = RandomHelper.Date(initialVersionDate);
        var firstVersionDescription = RandomHelper.String();
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateImageMetadata(dbContext.AsCallContext(), firstVersionDate).RunAsync(new UpdateImageMetadata.Request(image, user2.Id, initialName, initialSource, RandomHelper.String(), firstVersionDescription));

        var user3 = await UserHelper.CreateUserInDbAsync(db);
        var lastVersionDate = RandomHelper.Date(firstVersionDate);
        var lastVersionDescription = RandomHelper.String();
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateImageMetadata(dbContext.AsCallContext(), lastVersionDate).RunAsync(new UpdateImageMetadata.Request(image, user3.Id, initialName, RandomHelper.String(), RandomHelper.String(), lastVersionDescription));

        using (var dbContext = new MemCheckDbContext(db))
        {
            var versions = (await new GetImageVersions(dbContext.AsCallContext()).RunAsync(new GetImageVersions.Request(image))).ToList();

            var currentVersion = versions[0];
            Assert.AreEqual(user3.UserName, currentVersion.Author);
            Assert.AreEqual(lastVersionDate, currentVersion.VersionUtcDate);
            Assert.AreEqual(lastVersionDescription, currentVersion.VersionDescription);
            Assert.AreEqual(2, currentVersion.ChangedFieldNames.Count());

            var previousVersion = versions[1];
            Assert.AreEqual(user2.UserName, previousVersion.Author);
            Assert.AreEqual(firstVersionDate, previousVersion.VersionUtcDate);
            Assert.AreEqual(firstVersionDescription, previousVersion.VersionDescription);
            Assert.AreEqual(1, previousVersion.ChangedFieldNames.Count());

            var initialVersion = versions[2];
            Assert.AreEqual(user1.UserName, initialVersion.Author);
            Assert.AreEqual(initialVersionDate, initialVersion.VersionUtcDate);
            Assert.AreEqual(initialVersionDescription, initialVersion.VersionDescription);
            Assert.AreEqual(3, initialVersion.ChangedFieldNames.Count());
        }
    }
}

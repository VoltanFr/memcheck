using MemCheck.Application.Helpers;
using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Images;

[TestClass()]
public class GetImageInfoFromIdTests
{
    [TestMethod()]
    public async Task ImageDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var image = await ImageHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<ImageNotFoundException>(async () => await new GetImageInfoFromId(dbContext.AsCallContext()).RunAsync(new GetImageInfoFromId.Request(Guid.NewGuid())));
    }
    [TestMethod()]
    public async Task NotUsedInCards()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var name = RandomHelper.String();
        var source = RandomHelper.String();
        var description = RandomHelper.String();
        var uploadDate = RandomHelper.Date();
        var versionDescription = RandomHelper.String();
        var image = await ImageHelper.CreateAsync(db, user.Id, name: name, source: source, description: description, lastChangeUtcDate: uploadDate, versionDescription: versionDescription);

        using var dbContext = new MemCheckDbContext(db);
        var loaded = await new GetImageInfoFromId(dbContext.AsCallContext()).RunAsync(new GetImageInfoFromId.Request(image));
        Assert.AreEqual(user.UserName, loaded.CurrentVersionCreatorName);
        Assert.AreEqual(name, loaded.ImageName);
        Assert.AreEqual(description, loaded.Description);
        Assert.AreEqual(source, loaded.Source);
        Assert.AreEqual(uploadDate, loaded.InitialUploadUtcDate);
        Assert.AreEqual(uploadDate, loaded.LastChangeUtcDate);
        Assert.AreEqual(versionDescription, loaded.CurrentVersionDescription);
        Assert.AreEqual(0, loaded.CardCount);
        Assert.AreEqual(ImageHelper.contentType, loaded.OriginalImageContentType);
        Assert.IsTrue(loaded.OriginalImageSize > 0);
        Assert.IsTrue(loaded.SmallSize > 0);
        Assert.IsTrue(loaded.MediumSize > 0);
        Assert.IsTrue(loaded.BigSize > 0);
        Assert.IsTrue(loaded.MediumSize > loaded.SmallSize);
        Assert.IsTrue(loaded.BigSize > loaded.SmallSize);
    }
    [TestMethod()]
    public async Task UsedInCards()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var name = RandomHelper.String();
        var source = RandomHelper.String();
        var description = RandomHelper.String();
        var uploadDate = RandomHelper.Date();
        var versionDescription = RandomHelper.String();
        var image = await ImageHelper.CreateAsync(db, user.Id, name: name, source: source, description: description, lastChangeUtcDate: uploadDate, versionDescription: versionDescription);

        await CardHelper.CreateIdAsync(db, user.Id, frontSide: $"![Mnesios:{name},size=small]");
        await CardHelper.CreateIdAsync(db, user.Id, frontSide: $"![Mnesios:{name},size=big]");

        using var dbContext = new MemCheckDbContext(db);
        var loaded = await new GetImageInfoFromId(dbContext.AsCallContext()).RunAsync(new GetImageInfoFromId.Request(image));
        Assert.AreEqual(user.UserName, loaded.CurrentVersionCreatorName);
        Assert.AreEqual(name, loaded.ImageName);
        Assert.AreEqual(description, loaded.Description);
        Assert.AreEqual(source, loaded.Source);
        Assert.AreEqual(uploadDate, loaded.InitialUploadUtcDate);
        Assert.AreEqual(uploadDate, loaded.LastChangeUtcDate);
        Assert.AreEqual(versionDescription, loaded.CurrentVersionDescription);
        Assert.AreEqual(2, loaded.CardCount);
        Assert.AreEqual(ImageHelper.contentType, loaded.OriginalImageContentType);
        Assert.IsTrue(loaded.OriginalImageSize > 0);
        Assert.IsTrue(loaded.SmallSize > 0);
        Assert.IsTrue(loaded.MediumSize > 0);
        Assert.IsTrue(loaded.BigSize > 0);
        Assert.IsTrue(loaded.MediumSize > loaded.SmallSize);
        Assert.IsTrue(loaded.BigSize > loaded.SmallSize);
    }
    [TestMethod()]
    public async Task HasNewVersion()
    {
        var db = DbHelper.GetEmptyTestDB();

        var originalVersionCreator = await UserHelper.CreateUserInDbAsync(db);
        var originalVersionImageName = RandomHelper.String();
        var originalVersionSource = RandomHelper.String();
        var originalVersionDescription = RandomHelper.String();
        var originalVersionUploadDate = RandomHelper.Date();
        var originalVersionVersionDescription = RandomHelper.String();
        var originalBlob = StoreImageTests.GetPngImage();

        Guid imageId;

        using (var dbContext = new MemCheckDbContext(db))
        {
            var localizer = new TestLocalizer(new System.Collections.Generic.KeyValuePair<string, string>("InitialImageVersionCreation", originalVersionVersionDescription));
            var storer = new StoreImage(dbContext.AsCallContext(localizer), originalVersionUploadDate);
            var storeRequest = new StoreImage.Request(originalVersionCreator.Id, originalVersionImageName, originalVersionDescription, originalVersionSource, StoreImage.pngImageContentType, originalBlob);
            imageId = (await storer.RunAsync(storeRequest)).ImageId;
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var loaded = await new GetImageInfoFromId(dbContext.AsCallContext()).RunAsync(new GetImageInfoFromId.Request(imageId));
            Assert.AreEqual(originalVersionCreator.UserName, loaded.CurrentVersionCreatorName);
            Assert.AreEqual(originalVersionImageName, loaded.ImageName);
            Assert.AreEqual(originalVersionDescription, loaded.Description);
            Assert.AreEqual(originalVersionSource, loaded.Source);
            Assert.AreEqual(originalVersionUploadDate, loaded.InitialUploadUtcDate);
            Assert.AreEqual(originalVersionUploadDate, loaded.LastChangeUtcDate);
            Assert.AreEqual(originalVersionVersionDescription, loaded.CurrentVersionDescription);
            Assert.AreEqual(0, loaded.CardCount);
            Assert.AreEqual(StoreImage.pngImageContentType, loaded.OriginalImageContentType);
            Assert.AreEqual(originalBlob.Length, loaded.OriginalImageSize);
        }

        var newVersionUser = await UserHelper.CreateUserInDbAsync(db);
        var newVersionSource = RandomHelper.String();
        var newVersionDescription = RandomHelper.String();
        var newVersionUploadDate = RandomHelper.Date();
        var newVersionVersionDescription = RandomHelper.String();
        using (var dbContext = new MemCheckDbContext(db))
        {
            var updater = new UpdateImageMetadata(dbContext.AsCallContext(), newVersionUploadDate);
            var updateRequest = new UpdateImageMetadata.Request(imageId, newVersionUser.Id, originalVersionImageName, newVersionSource, newVersionDescription, newVersionVersionDescription);
            await updater.RunAsync(updateRequest);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var loaded = await new GetImageInfoFromId(dbContext.AsCallContext()).RunAsync(new GetImageInfoFromId.Request(imageId));
            Assert.AreEqual(newVersionUser.UserName, loaded.CurrentVersionCreatorName);
            Assert.AreEqual(originalVersionImageName, loaded.ImageName);
            Assert.AreEqual(newVersionDescription, loaded.Description);
            Assert.AreEqual(newVersionSource, loaded.Source);
            Assert.AreEqual(originalVersionUploadDate, loaded.InitialUploadUtcDate);
            Assert.AreEqual(newVersionUploadDate, loaded.LastChangeUtcDate);
            Assert.AreEqual(newVersionVersionDescription, loaded.CurrentVersionDescription);
            Assert.AreEqual(0, loaded.CardCount);
            Assert.AreEqual(StoreImage.pngImageContentType, loaded.OriginalImageContentType);
            Assert.AreEqual(originalBlob.Length, loaded.OriginalImageSize);
        }
    }
}

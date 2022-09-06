using MemCheck.Application.Helpers;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Images;

[TestClass()]
public class GetImageListTests
{
    [TestMethod()]
    public async Task EmptyDb()
    {
        using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
        var result = await new GetImageList(dbContext.AsCallContext()).RunAsync(new GetImageList.Request(1, 10, ""));
        Assert.AreEqual(0, result.PageCount);
        Assert.AreEqual(0, result.TotalCount);
    }
    [TestMethod()]
    public async Task Page0()
    {
        using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetImageList(dbContext.AsCallContext()).RunAsync(new GetImageList.Request(1, 0, "")));
    }
    [TestMethod()]
    public async Task PageSize0()
    {
        using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetImageList(dbContext.AsCallContext()).RunAsync(new GetImageList.Request(0, 1, "")));
    }
    [TestMethod()]
    public async Task PageSizeTooBig()
    {
        using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetImageList(dbContext.AsCallContext()).RunAsync(new GetImageList.Request(0, GetImageList.Request.MaxPageSize + 1, "")));
    }
    [TestMethod()]
    public async Task FilteredNotTrimmed()
    {
        using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetImageList(dbContext.AsCallContext()).RunAsync(new GetImageList.Request(0, 1, "  " + RandomHelper.String())));
    }
    [TestMethod()]
    public async Task OneImageInDb()
    {
        var db = DbHelper.GetEmptyTestDB();
        var imageName = RandomHelper.String();
        var userName = RandomHelper.String();
        var user = await UserHelper.CreateInDbAsync(db, userName: userName);
        var description = RandomHelper.String();
        var source = RandomHelper.String();
        var uploadDate = RandomHelper.Date();
        var versionDescription = RandomHelper.String();
        var image = await ImageHelper.CreateAsync(db, user, name: imageName, description: description, source: source, lastChangeUtcDate: uploadDate, versionDescription: versionDescription);
        using var dbContext = new MemCheckDbContext(db);
        var result = await new GetImageList(dbContext.AsCallContext()).RunAsync(new GetImageList.Request(1, 1, ""));
        Assert.AreEqual(1, result.PageCount);
        Assert.AreEqual(1, result.TotalCount);
        var loaded = result.Images.Single();
        Assert.AreEqual(image, loaded.ImageId);
        Assert.AreEqual(imageName, loaded.ImageName);
        Assert.AreEqual("InvalidForUnitTests", loaded.OriginalImageContentType);
        Assert.AreEqual(userName, loaded.Uploader);
        Assert.AreEqual(description, loaded.Description);
        Assert.AreEqual(source, loaded.Source);
        Assert.AreEqual(4, loaded.OriginalImageSize);
        Assert.AreEqual(1, loaded.SmallSize);
        Assert.AreEqual(2, loaded.MediumSize);
        Assert.AreEqual(3, loaded.BigSize);
        Assert.AreEqual(uploadDate, loaded.InitialUploadUtcDate);
        Assert.AreEqual(uploadDate, loaded.LastChangeUtcDate);
        Assert.AreEqual(versionDescription, loaded.CurrentVersionDescription);
    }
    [TestMethod()]
    public async Task OnePage()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        await ImageHelper.CreateAsync(db, user);
        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new GetImageList(dbContext.AsCallContext()).RunAsync(new GetImageList.Request(1, 1, ""));
            Assert.AreEqual(1, result.PageCount);
            Assert.AreEqual(1, result.TotalCount);
            Assert.AreEqual(1, result.Images.Count());
        }
        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new GetImageList(dbContext.AsCallContext()).RunAsync(new GetImageList.Request(1, 2, ""));
            Assert.AreEqual(1, result.PageCount);
            Assert.AreEqual(1, result.TotalCount);
            Assert.IsFalse(result.Images.Any());
        }
    }
    [TestMethod()]
    public async Task TwoPages()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var img1 = await ImageHelper.CreateAsync(db, user);
        var img2 = await ImageHelper.CreateAsync(db, user);
        var loaded = new HashSet<Guid>();
        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new GetImageList(dbContext.AsCallContext()).RunAsync(new GetImageList.Request(1, 1, ""));
            Assert.AreEqual(2, result.PageCount);
            Assert.AreEqual(2, result.TotalCount);
            loaded.Add(result.Images.Single().ImageId);
        }
        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new GetImageList(dbContext.AsCallContext()).RunAsync(new GetImageList.Request(1, 2, ""));
            Assert.AreEqual(2, result.PageCount);
            Assert.AreEqual(2, result.TotalCount);
            loaded.Add(result.Images.Single().ImageId);
        }
        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new GetImageList(dbContext.AsCallContext()).RunAsync(new GetImageList.Request(1, 3, ""));
            Assert.AreEqual(2, result.PageCount);
            Assert.AreEqual(2, result.TotalCount);
            Assert.IsFalse(result.Images.Any());
        }
        Assert.IsTrue(loaded.Contains(img1));
        Assert.IsTrue(loaded.Contains(img2));
    }
    [TestMethod()]
    public async Task Filtering()
    {
        var db = DbHelper.GetEmptyTestDB();
        var imageName = RandomHelper.String();
        var user = await UserHelper.CreateInDbAsync(db);
        await ImageHelper.CreateAsync(db, user, name: imageName);
        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new GetImageList(dbContext.AsCallContext()).RunAsync(new GetImageList.Request(1, 1, RandomHelper.String()));
            Assert.AreEqual(0, result.PageCount);
            Assert.AreEqual(0, result.TotalCount);
            Assert.IsFalse(result.Images.Any());
        }
        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new GetImageList(dbContext.AsCallContext()).RunAsync(new GetImageList.Request(1, 1, imageName));
            Assert.AreEqual(1, result.PageCount);
            Assert.AreEqual(1, result.TotalCount);
            Assert.AreEqual(1, result.Images.Count());
        }
        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new GetImageList(dbContext.AsCallContext()).RunAsync(new GetImageList.Request(1, 1, imageName.Substring(1, 5)));
            Assert.AreEqual(1, result.PageCount);
            Assert.AreEqual(1, result.TotalCount);
            Assert.AreEqual(1, result.Images.Count());
        }
    }
}

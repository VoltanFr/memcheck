using MemCheck.Application.Helpers;
using MemCheck.Basics;
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
    public async Task OneImageInDb_NotUsed()
    {
        var db = DbHelper.GetEmptyTestDB();
        var imageName = RandomHelper.String();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var description = RandomHelper.String();
        var source = RandomHelper.String();
        var uploadDate = RandomHelper.Date();
        var versionDescription = RandomHelper.String();
        var imageId = await ImageHelper.CreateAsync(db, user.Id, name: imageName, description: description, source: source, lastChangeUtcDate: uploadDate, versionDescription: versionDescription);

        using var dbContext = new MemCheckDbContext(db);
        var result = await new GetImageList(dbContext.AsCallContext()).RunAsync(new GetImageList.Request(1, 1, ""));
        Assert.AreEqual(1, result.PageCount);
        Assert.AreEqual(1, result.TotalCount);
        var loadedImage = result.Images.Single();
        Assert.AreEqual(imageId, loadedImage.ImageId);
        Assert.AreEqual(imageName, loadedImage.ImageName);
        Assert.AreEqual(0, loadedImage.CardCount);
    }
    [TestMethod()]
    public async Task OneImageInDb_Used()
    {
        var db = DbHelper.GetEmptyTestDB();
        var imageName = RandomHelper.String();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var description = RandomHelper.String();
        var source = RandomHelper.String();
        var uploadDate = RandomHelper.Date();
        var versionDescription = RandomHelper.String();
        var imageId = await ImageHelper.CreateAsync(db, user.Id, name: imageName, description: description, source: source, lastChangeUtcDate: uploadDate, versionDescription: versionDescription);

        await RandomHelper.Int(3, 10).TimesAsync(async () => await CardHelper.CreateAsync(db, user.Id)); //Cards which don't use the image

        var usingCardCount = RandomHelper.Int(3, 10);
        await usingCardCount.TimesAsync(async () => await CardHelper.CreateAsync(db, user.Id, frontSide: $"![Mnesios:{imageName}]"));

        using var dbContext = new MemCheckDbContext(db);
        var result = await new GetImageList(dbContext.AsCallContext()).RunAsync(new GetImageList.Request(1, 1, ""));
        Assert.AreEqual(1, result.PageCount);
        Assert.AreEqual(1, result.TotalCount);
        var loadedImage = result.Images.Single();
        Assert.AreEqual(imageId, loadedImage.ImageId);
        Assert.AreEqual(imageName, loadedImage.ImageName);
        Assert.AreEqual(usingCardCount, loadedImage.CardCount);
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
            Assert.AreEqual(1, result.Images.Length);
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
            Assert.AreEqual(1, result.Images.Length);
        }
        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new GetImageList(dbContext.AsCallContext()).RunAsync(new GetImageList.Request(1, 1, imageName.Substring(1, 5)));
            Assert.AreEqual(1, result.PageCount);
            Assert.AreEqual(1, result.TotalCount);
            Assert.AreEqual(1, result.Images.Length);
        }
    }
    [TestMethod()]
    public async Task ComplexTest()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);

        var filteredString = RandomHelper.String();

        var image1MatchingFilterName = RandomHelper.String();
        var image1IdMatchingFilter = await ImageHelper.CreateAsync(db, userId, name: image1MatchingFilterName, description: filteredString + RandomHelper.String());

        var image2MatchingFilterName = filteredString + RandomHelper.String();
        var image2IdMatchingFilter = await ImageHelper.CreateAsync(db, userId, name: image2MatchingFilterName);

        var image3MatchingFilterName = RandomHelper.String();
        var image3IdMatchingFilter = await ImageHelper.CreateAsync(db, userId, name: image3MatchingFilterName, source: RandomHelper.String() + filteredString);

        var image4NotMatchingFilterName = RandomHelper.String();
        var image4IdNotMatchingFilter = await ImageHelper.CreateAsync(db, userId, name: image4NotMatchingFilterName);

        var image5NotMatchingFilterName = RandomHelper.String();
        var image5IdNotMatchingFilter = await ImageHelper.CreateAsync(db, userId, name: image5NotMatchingFilterName);

        var image6IdNotUsed = await ImageHelper.CreateAsync(db, userId);

        await RandomHelper.Int(3, 10).TimesAsync(async () => await CardHelper.CreateAsync(db, userId)); //Cards which don't use any image

        var cardsUsingImage1Count = RandomHelper.Int(3, 10);
        await cardsUsingImage1Count.TimesAsync(async () => await CardHelper.CreateAsync(db, userId, frontSide: $"![Mnesios:{image1MatchingFilterName}]"));

        var cardsUsingImage2Count = RandomHelper.Int(3, 10);
        await cardsUsingImage2Count.TimesAsync(async () => await CardHelper.CreateAsync(db, userId, frontSide: $"![Mnesios:{image2MatchingFilterName}]"));

        var cardsUsingImage3Count = RandomHelper.Int(3, 10);
        await cardsUsingImage3Count.TimesAsync(async () => await CardHelper.CreateAsync(db, userId, frontSide: $"![Mnesios:{image3MatchingFilterName}]"));

        var cardsUsingImage4Count = RandomHelper.Int(3, 10);
        await cardsUsingImage4Count.TimesAsync(async () => await CardHelper.CreateAsync(db, userId, frontSide: $"![Mnesios:{image4NotMatchingFilterName}]"));

        var cardsUsingImage5Count = RandomHelper.Int(3, 10);
        await cardsUsingImage5Count.TimesAsync(async () => await CardHelper.CreateAsync(db, userId, frontSide: $"![Mnesios:{image5IdNotMatchingFilter}]"));

        using var dbContext = new MemCheckDbContext(db);
        var result = await new GetImageList(dbContext.AsCallContext()).RunAsync(new GetImageList.Request(100, 1, filteredString));

        Assert.AreEqual(1, result.PageCount);
        Assert.AreEqual(3, result.TotalCount);
        Assert.AreEqual(3, result.Images.Length);

        {
            var image1InResult = result.Images.Single(img => img.ImageId == image1IdMatchingFilter);
            Assert.AreEqual(image1MatchingFilterName, image1InResult.ImageName);
            Assert.AreEqual(cardsUsingImage1Count, image1InResult.CardCount);
        }

        {
            var image2InResult = result.Images.Single(img => img.ImageId == image2IdMatchingFilter);
            Assert.AreEqual(image2MatchingFilterName, image2InResult.ImageName);
            Assert.AreEqual(cardsUsingImage2Count, image2InResult.CardCount);
        }

        {
            var image3InResult = result.Images.Single(img => img.ImageId == image3IdMatchingFilter);
            Assert.AreEqual(image3MatchingFilterName, image3InResult.ImageName);
            Assert.AreEqual(cardsUsingImage3Count, image3InResult.CardCount);
        }
    }
}

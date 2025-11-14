using MemCheck.Application.Helpers;
using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Images;

[TestClass()]
public class DeleteImageTests
{
    [TestMethod()]
    public async Task UserNotLoggedIn()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var image = await ImageHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExactlyAsync<NonexistentUserException>(async () => await new DeleteImage(dbContext.AsCallContext()).RunAsync(new DeleteImage.Request(Guid.Empty, image, RandomHelper.String())));
    }
    [TestMethod()]
    public async Task UserDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var image = await ImageHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExactlyAsync<NonexistentUserException>(async () => await new DeleteImage(dbContext.AsCallContext()).RunAsync(new DeleteImage.Request(Guid.NewGuid(), image, RandomHelper.String())));
    }
    [TestMethod()]
    public async Task ImageDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExactlyAsync<ImageNotFoundException>(async () => await new DeleteImage(dbContext.AsCallContext(new TestRoleChecker(new[] { user }))).RunAsync(new DeleteImage.Request(user, Guid.NewGuid(), RandomHelper.String())));
    }
    [TestMethod()]
    public async Task DescriptionTooShort()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var image = await ImageHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExactlyAsync<RequestInputException>(async () => await new DeleteImage(dbContext.AsCallContext()).RunAsync(new DeleteImage.Request(user, image, RandomHelper.String(DeleteImage.Request.MinDescriptionLength - 1))));
    }
    [TestMethod()]
    public async Task DescriptionTooLong()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var image = await ImageHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExactlyAsync<RequestInputException>(async () => await new DeleteImage(dbContext.AsCallContext()).RunAsync(new DeleteImage.Request(user, image, RandomHelper.String(DeleteImage.Request.MaxDescriptionLength + 1))));
    }
    [TestMethod()]
    public async Task DescriptionNotTrimmed()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var image = await ImageHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await new DeleteImage(dbContext.AsCallContext()).RunAsync(new DeleteImage.Request(user, image, RandomHelper.String(DeleteImage.Request.MinDescriptionLength) + Environment.NewLine)));
    }
    [TestMethod()]
    public async Task UsedInCard()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);

        var imageName = RandomHelper.String();
        var imageId = await ImageHelper.CreateAsync(db, userId, imageName);

        var cardId = await CardHelper.CreateIdAsync(db, userId, additionalInfo: $"![Mnesios:{imageName}]");

        using (var dbContext = new MemCheckDbContext(db))
            await Assert.ThrowsExactlyAsync<ImageUsedInCardException>(async () => await new DeleteImage(dbContext.AsCallContext()).RunAsync(new DeleteImage.Request(userId, imageId, RandomHelper.String())));

        using (var dbContext = new MemCheckDbContext(db))
        {
            Assert.AreEqual(1, dbContext.ImagesInCards.Count());
            Assert.AreEqual(1, dbContext.ImagesInCards.Count(imageInCard => imageInCard.ImageId == imageId));
            Assert.AreEqual(1, dbContext.ImagesInCards.Count(imageInCard => imageInCard.CardId == cardId));
        }
    }
    [TestMethod()]
    public async Task UsedInCards()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);

        var image1Name = RandomHelper.String();
        var imageId = await ImageHelper.CreateAsync(db, userId, image1Name);

        var image2Name = RandomHelper.String();
        await ImageHelper.CreateAsync(db, userId, image2Name);

        var image3Name = RandomHelper.String();
        await ImageHelper.CreateAsync(db, userId, image3Name);

        await CardHelper.CreateIdAsync(db, userId, additionalInfo: $"![Mnesios:{image1Name}]![Mnesios:{image2Name}]");
        await CardHelper.CreateIdAsync(db, userId, additionalInfo: $"![Mnesios:{image1Name}]![Mnesios:{image1Name}]");
        await CardHelper.CreateIdAsync(db, userId, additionalInfo: $"![Mnesios:{image3Name}]![Mnesios:{image3Name}]");

        using (var dbContext = new MemCheckDbContext(db))
            await Assert.ThrowsExactlyAsync<ImageUsedInCardException>(async () => await new DeleteImage(dbContext.AsCallContext()).RunAsync(new DeleteImage.Request(userId, imageId, RandomHelper.String())));

        using (var dbContext = new MemCheckDbContext(db))
        {
            Assert.AreEqual(4, dbContext.ImagesInCards.Count());
            Assert.AreEqual(2, dbContext.ImagesInCards.Count(imageInCard => imageInCard.ImageId == imageId));
        }
    }
    [TestMethod()]
    public async Task OthersAreUsedInCards()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);

        var image1Name = RandomHelper.String();
        var imageId = await ImageHelper.CreateAsync(db, userId, image1Name);

        var image2Name = RandomHelper.String();
        await ImageHelper.CreateAsync(db, userId, image2Name);

        var image3Name = RandomHelper.String();
        await ImageHelper.CreateAsync(db, userId, image3Name);

        await CardHelper.CreateIdAsync(db, userId, additionalInfo: $"![Mnesios:{image2Name}]");
        await CardHelper.CreateIdAsync(db, userId, additionalInfo: $"![Mnesios:{image3Name}]![Mnesios:{image3Name}]");

        using (var dbContext = new MemCheckDbContext(db))
            await new DeleteImage(dbContext.AsCallContext()).RunAsync(new DeleteImage.Request(userId, imageId, RandomHelper.String()));

        using (var dbContext = new MemCheckDbContext(db))
        {
            Assert.AreEqual(2, dbContext.ImagesInCards.Count());
            Assert.IsFalse(dbContext.ImagesInCards.Any(imageInCard => imageInCard.ImageId == imageId));
        }
    }
    [TestMethod()]
    public async Task Simple()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var imageName = RandomHelper.String();
        var imageCreationVersionDescription = RandomHelper.String();
        var imageUploadDate = RandomHelper.Date();
        var image = await ImageHelper.CreateAsync(db, user, imageName, imageCreationVersionDescription, lastChangeUtcDate: imageUploadDate);

        var deletionDescription = RandomHelper.String();
        var deletionDate = RandomHelper.Date(imageUploadDate);
        using (var dbContext = new MemCheckDbContext(db))
            await new DeleteImage(dbContext.AsCallContext(new TestRoleChecker(new[] { user })), deletionDate).RunAsync(new DeleteImage.Request(user, image, deletionDescription));

        using (var dbContext = new MemCheckDbContext(db))
        {
            var initialPreviousVersion = dbContext.ImagePreviousVersions.Include(prev => prev.Owner).Single(prev => prev.PreviousVersion == null);
            Assert.AreEqual(image, initialPreviousVersion.Image);
            Assert.AreEqual(user, initialPreviousVersion.Owner.Id);
            Assert.AreEqual(imageName, initialPreviousVersion.Name);
            Assert.AreEqual(imageCreationVersionDescription, initialPreviousVersion.VersionDescription);
            Assert.AreEqual(imageUploadDate, initialPreviousVersion.InitialUploadUtcDate);
            Assert.AreEqual(imageUploadDate, initialPreviousVersion.VersionUtcDate);
            Assert.IsNull(initialPreviousVersion.PreviousVersion);

            var lastPreviousVersion = dbContext.ImagePreviousVersions.Include(prev => prev.Owner).Single(prev => prev.PreviousVersion != null);
            Assert.AreEqual(image, lastPreviousVersion.Image);
            Assert.AreEqual(user, lastPreviousVersion.Owner.Id);
            Assert.AreEqual(imageName, lastPreviousVersion.Name);
            Assert.AreEqual(deletionDescription, lastPreviousVersion.VersionDescription);
            Assert.AreEqual(imageUploadDate, lastPreviousVersion.InitialUploadUtcDate);
            Assert.AreEqual(deletionDate, lastPreviousVersion.VersionUtcDate);
            Assert.AreEqual(initialPreviousVersion, lastPreviousVersion.PreviousVersion);
        }
    }
}

using MemCheck.Application.Helpers;
using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Images;

[TestClass()]
public class UpdateImageMetadataTests
{
    [TestMethod()]
    public async Task UserNotLoggedIn()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var image = await ImageHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new UpdateImageMetadata(dbContext.AsCallContext()).RunAsync(new UpdateImageMetadata.Request(image, Guid.Empty, RandomHelper.String(), RandomHelper.String(), RandomHelper.String(), RandomHelper.String())));
    }
    [TestMethod()]
    public async Task UserDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);

        var name = RandomHelper.String();
        var image = await ImageHelper.CreateAsync(db, user, name);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new UpdateImageMetadata(dbContext.AsCallContext()).RunAsync(new UpdateImageMetadata.Request(image, Guid.NewGuid(), name, RandomHelper.String(), RandomHelper.String(), RandomHelper.String())));
    }
    [TestMethod()]
    public async Task ImageDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var image = await ImageHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new UpdateImageMetadata(dbContext.AsCallContext()).RunAsync(new UpdateImageMetadata.Request(Guid.NewGuid(), user, RandomHelper.String(), RandomHelper.String(), RandomHelper.String(), RandomHelper.String())));
    }
    [TestMethod()]
    public async Task NameTooShort()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var image = await ImageHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateImageMetadata(dbContext.AsCallContext()).RunAsync(new UpdateImageMetadata.Request(image, user, RandomHelper.String(QueryValidationHelper.ImageMinNameLength - 1), RandomHelper.String(), RandomHelper.String(), RandomHelper.String())));
    }
    [TestMethod()]
    public async Task NameTooLong()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var image = await ImageHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateImageMetadata(dbContext.AsCallContext()).RunAsync(new UpdateImageMetadata.Request(image, user, RandomHelper.String(QueryValidationHelper.ImageMaxNameLength + 1), RandomHelper.String(), RandomHelper.String(), RandomHelper.String())));
    }
    [TestMethod()]
    public async Task NameNotTrimmed()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var image = await ImageHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new UpdateImageMetadata(dbContext.AsCallContext()).RunAsync(new UpdateImageMetadata.Request(image, user, RandomHelper.String(QueryValidationHelper.ImageMinNameLength) + "\t\t", RandomHelper.String(), RandomHelper.String(), RandomHelper.String())));
    }
    [TestMethod()]
    public async Task SourceTooShort()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var image = await ImageHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateImageMetadata(dbContext.AsCallContext()).RunAsync(new UpdateImageMetadata.Request(image, user, RandomHelper.String(), RandomHelper.String(QueryValidationHelper.ImageMinSourceLength - 1), RandomHelper.String(), RandomHelper.String())));
    }
    [TestMethod()]
    public async Task SourceTooLong()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var image = await ImageHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateImageMetadata(dbContext.AsCallContext()).RunAsync(new UpdateImageMetadata.Request(image, user, RandomHelper.String(), RandomHelper.String(QueryValidationHelper.ImageMaxSourceLength + 1), RandomHelper.String(), RandomHelper.String())));
    }
    [TestMethod()]
    public async Task SourceNotTrimmed()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);

        var imageName = RandomHelper.String();
        var image = await ImageHelper.CreateAsync(db, user, imageName);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new UpdateImageMetadata(dbContext.AsCallContext()).RunAsync(new UpdateImageMetadata.Request(image, user, imageName, RandomHelper.String(QueryValidationHelper.ImageMinSourceLength) + "\t\t", RandomHelper.String(), RandomHelper.String())));
    }
    [TestMethod()]
    public async Task DescriptionTooShort()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var image = await ImageHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateImageMetadata(dbContext.AsCallContext()).RunAsync(new UpdateImageMetadata.Request(image, user, RandomHelper.String(), RandomHelper.String(), RandomHelper.String(QueryValidationHelper.ImageMinDescriptionLength - 1), RandomHelper.String())));
    }
    [TestMethod()]
    public async Task DescriptionTooLong()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var image = await ImageHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateImageMetadata(dbContext.AsCallContext()).RunAsync(new UpdateImageMetadata.Request(image, user, RandomHelper.String(), RandomHelper.String(), RandomHelper.String(QueryValidationHelper.ImageMaxDescriptionLength + 1), RandomHelper.String())));
    }
    [TestMethod()]
    public async Task DescriptionNotTrimmed()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);

        var imageName = RandomHelper.String();
        var image = await ImageHelper.CreateAsync(db, user, imageName);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new UpdateImageMetadata(dbContext.AsCallContext()).RunAsync(new UpdateImageMetadata.Request(image, user, imageName, RandomHelper.String(), RandomHelper.String(QueryValidationHelper.ImageMinDescriptionLength) + "\t\t", RandomHelper.String())));
    }
    [TestMethod()]
    public async Task VersionDescriptionTooShort()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var image = await ImageHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateImageMetadata(dbContext.AsCallContext()).RunAsync(new UpdateImageMetadata.Request(image, user, RandomHelper.String(), RandomHelper.String(), RandomHelper.String(), RandomHelper.String(QueryValidationHelper.ImageMinVersionDescriptionLength - 1))));
    }
    [TestMethod()]
    public async Task VersionDescriptionTooLong()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var image = await ImageHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateImageMetadata(dbContext.AsCallContext()).RunAsync(new UpdateImageMetadata.Request(image, user, RandomHelper.String(), RandomHelper.String(), RandomHelper.String(), RandomHelper.String(QueryValidationHelper.ImageMaxVersionDescriptionLength + 1))));
    }
    [TestMethod()]
    public async Task VersionDescriptionNotTrimmed()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);

        var name = RandomHelper.String();
        var image = await ImageHelper.CreateAsync(db, user, name);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new UpdateImageMetadata(dbContext.AsCallContext()).RunAsync(new UpdateImageMetadata.Request(image, user, name, RandomHelper.String(), RandomHelper.String(), RandomHelper.String(QueryValidationHelper.ImageMinVersionDescriptionLength) + "\t\t")));
    }
    [TestMethod()]
    public async Task NoChange()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var name = RandomHelper.String();
        var source = RandomHelper.String();
        var description = RandomHelper.String();
        var image = await ImageHelper.CreateAsync(db, user, name: name, source: source, description: description);

        using var dbContext = new MemCheckDbContext(db);
        var request = new UpdateImageMetadata.Request(image, user, name, source, description, RandomHelper.String());
        await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateImageMetadata(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task NameAlreadyUsed()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var name = RandomHelper.String();
        await ImageHelper.CreateAsync(db, user, name: name);
        var imageToRename = await ImageHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        var request = new UpdateImageMetadata.Request(imageToRename, user, name, RandomHelper.String(), RandomHelper.String(), RandomHelper.String());
        await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateImageMetadata(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task UpdateName()
    {
        //Image renaming is not implemented yet

        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);

        var name = RandomHelper.String();
        var source = RandomHelper.String();
        var description = RandomHelper.String();
        var versionDescription = RandomHelper.String();
        var uploadDate = RandomHelper.Date();
        var image = await ImageHelper.CreateAsync(db, user, name: name, source: source, description: description, lastChangeUtcDate: uploadDate, versionDescription: versionDescription);

        using (var dbContext = new MemCheckDbContext(db))
        {
            var request = new UpdateImageMetadata.Request(image, user, RandomHelper.String(), source, description, versionDescription);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateImageMetadata(dbContext.AsCallContext(), RandomHelper.Date(uploadDate)).RunAsync(request));
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            //Check that nothing has changed

            var fromDb = await dbContext.Images.Include(img => img.Owner).SingleAsync();
            Assert.AreEqual(image, fromDb.Id);
            Assert.AreEqual(name, fromDb.Name);
            Assert.AreEqual(user, fromDb.Owner.Id);
            Assert.AreEqual(description, fromDb.Description);
            Assert.AreEqual(source, fromDb.Source);
            Assert.AreEqual(uploadDate, fromDb.InitialUploadUtcDate);
            Assert.AreEqual(uploadDate, fromDb.LastChangeUtcDate);
            Assert.AreEqual(versionDescription, fromDb.VersionDescription);
            Assert.AreEqual(ImageVersionType.Creation, fromDb.VersionType);
        }
    }
    [TestMethod()]
    public async Task UpdateSource()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var name = RandomHelper.String();
        var description = RandomHelper.String();
        var uploadDate = RandomHelper.Date();
        var image = await ImageHelper.CreateAsync(db, user, name: name, description: description, lastChangeUtcDate: uploadDate);

        var newSource = RandomHelper.String();
        var runDate = RandomHelper.Date(uploadDate);
        var versionDescription = RandomHelper.String();

        using (var dbContext = new MemCheckDbContext(db))
        {
            var request = new UpdateImageMetadata.Request(image, user, name, newSource, description, versionDescription);
            await new UpdateImageMetadata(dbContext.AsCallContext(), runDate).RunAsync(request);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var fromDb = await dbContext.Images.Include(img => img.Owner).SingleAsync();
            Assert.AreEqual(image, fromDb.Id);
            Assert.AreEqual(name, fromDb.Name);
            Assert.AreEqual(user, fromDb.Owner.Id);
            Assert.AreEqual(description, fromDb.Description);
            Assert.AreEqual(newSource, fromDb.Source);
            Assert.AreEqual(uploadDate, fromDb.InitialUploadUtcDate);
            Assert.AreEqual(runDate, fromDb.LastChangeUtcDate);
            Assert.AreEqual(versionDescription, fromDb.VersionDescription);
            Assert.AreEqual(ImageVersionType.Changes, fromDb.VersionType);
        }
    }
    [TestMethod()]
    public async Task UpdateDescription()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var name = RandomHelper.String();
        var source = RandomHelper.String();
        var uploadDate = RandomHelper.Date();
        var image = await ImageHelper.CreateAsync(db, user, name: name, source: source, lastChangeUtcDate: uploadDate);

        var newDescription = RandomHelper.String();
        var runDate = RandomHelper.Date(uploadDate);
        var versionDescription = RandomHelper.String();

        using (var dbContext = new MemCheckDbContext(db))
        {
            var request = new UpdateImageMetadata.Request(image, user, name, source, newDescription, versionDescription);
            await new UpdateImageMetadata(dbContext.AsCallContext(), runDate).RunAsync(request);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var fromDb = await dbContext.Images.Include(img => img.Owner).SingleAsync();
            Assert.AreEqual(image, fromDb.Id);
            Assert.AreEqual(name, fromDb.Name);
            Assert.AreEqual(user, fromDb.Owner.Id);
            Assert.AreEqual(newDescription, fromDb.Description);
            Assert.AreEqual(source, fromDb.Source);
            Assert.AreEqual(uploadDate, fromDb.InitialUploadUtcDate);
            Assert.AreEqual(runDate, fromDb.LastChangeUtcDate);
            Assert.AreEqual(versionDescription, fromDb.VersionDescription);
            Assert.AreEqual(ImageVersionType.Changes, fromDb.VersionType);
        }
    }
    [TestMethod()]
    public async Task UpdateByAnotherUser()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);

        var name = RandomHelper.String();
        var image = await ImageHelper.CreateAsync(db, user, name);

        var otherUser = await UserHelper.CreateInDbAsync(db);
        using (var dbContext = new MemCheckDbContext(db))
        {
            var request = new UpdateImageMetadata.Request(image, otherUser, name, RandomHelper.String(), RandomHelper.String(), RandomHelper.String());
            await new UpdateImageMetadata(dbContext.AsCallContext()).RunAsync(request);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var fromDb = await dbContext.Images.Include(img => img.Owner).SingleAsync();
            Assert.AreEqual(image, fromDb.Id);
            Assert.AreEqual(otherUser, fromDb.Owner.Id);
        }
    }
    [TestMethod()]
    public async Task Versionning()
    {
        var db = DbHelper.GetEmptyTestDB();

        var user1 = await UserHelper.CreateInDbAsync(db);
        var initialVersionDate = RandomHelper.Date();

        var name = RandomHelper.String();
        var image = await ImageHelper.CreateAsync(db, user1, lastChangeUtcDate: initialVersionDate, name: name);

        var user2 = await UserHelper.CreateInDbAsync(db);
        var firstVersionDate = RandomHelper.Date(initialVersionDate);
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateImageMetadata(dbContext.AsCallContext(), firstVersionDate).RunAsync(new UpdateImageMetadata.Request(image, user2, name, RandomHelper.String(), RandomHelper.String(), RandomHelper.String()));

        var user3 = await UserHelper.CreateInDbAsync(db);
        var lastVersionDate = RandomHelper.Date(firstVersionDate);
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateImageMetadata(dbContext.AsCallContext(), lastVersionDate).RunAsync(new UpdateImageMetadata.Request(image, user3, name, RandomHelper.String(), RandomHelper.String(), RandomHelper.String()));

        using (var dbContext = new MemCheckDbContext(db))
        {
            var currentVersion = await dbContext.Images.Include(img => img.Owner).Include(img => img.PreviousVersion).ThenInclude(prev => prev!.Owner).SingleAsync();
            Assert.AreEqual(image, currentVersion.Id);
            Assert.AreEqual(user3, currentVersion.Owner.Id);
            Assert.AreEqual(lastVersionDate, currentVersion.LastChangeUtcDate);
            Assert.AreEqual(initialVersionDate, currentVersion.InitialUploadUtcDate);

            var previousVersion = dbContext.ImagePreviousVersions.Include(img => img.Owner).Include(img => img.PreviousVersion).Single(img => img.Id == currentVersion.PreviousVersion!.Id);
            Assert.AreEqual(user2, previousVersion.Owner.Id);
            Assert.AreEqual(image, previousVersion.Image);
            Assert.AreEqual(firstVersionDate, previousVersion.VersionUtcDate);
            Assert.AreEqual(initialVersionDate, previousVersion.InitialUploadUtcDate);
            Assert.AreEqual(ImagePreviousVersionType.Changes, previousVersion.VersionType);

            var initial = dbContext.ImagePreviousVersions.Include(img => img.Owner).Include(img => img.PreviousVersion).Single(img => img.Id == previousVersion.PreviousVersion!.Id);
            Assert.AreEqual(user1, initial.Owner.Id);
            Assert.AreEqual(image, initial.Image);
            Assert.AreEqual(initialVersionDate, initial.VersionUtcDate);
            Assert.AreEqual(initialVersionDate, initial.InitialUploadUtcDate);
            Assert.AreEqual(ImagePreviousVersionType.Creation, initial.VersionType);
            Assert.IsNull(initial.PreviousVersion);
        }
    }
}

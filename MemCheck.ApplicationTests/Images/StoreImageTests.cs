using MemCheck.Application.Helpers;
using MemCheck.Application.QueryValidation;
using MemCheck.Basics;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Images;

[TestClass()]
public class StoreImageTests
{
    #region Fields
    private static readonly ImmutableArray<byte> pngImage = GetPngImage();
    #endregion
    #region Private methods
    public static ImmutableArray<byte> GetPngImage()
    {
        using var resFilestream = typeof(StoreImageTests).Assembly.GetManifestResourceStream("MemCheck.Application.Resources.Gray.png")!;
        var result = new byte[resFilestream.Length];
        resFilestream.Read(result, 0, result.Length);
        return result.ToImmutableArray();
    }
    #endregion
    [TestMethod()]
    public async Task UserNotLoggedIn()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        var request = new StoreImage.Request(Guid.Empty, RandomHelper.String(), RandomHelper.String(), RandomHelper.String(), StoreImage.pngImageContentType, pngImage);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new StoreImage(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task UserDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        var request = new StoreImage.Request(Guid.NewGuid(), RandomHelper.String(), RandomHelper.String(), RandomHelper.String(), StoreImage.pngImageContentType, pngImage);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new StoreImage(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task NameTooShort()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        var request = new StoreImage.Request(user, RandomHelper.String(QueryValidationHelper.ImageMinNameLength - 1), RandomHelper.String(), RandomHelper.String(), StoreImage.pngImageContentType, pngImage);
        await Assert.ThrowsExceptionAsync<InvalidImageNameLengthException>(async () => await new StoreImage(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task NameTooLong()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        var request = new StoreImage.Request(user, RandomHelper.String(QueryValidationHelper.ImageMaxNameLength + 1), RandomHelper.String(), RandomHelper.String(), StoreImage.pngImageContentType, pngImage);
        await Assert.ThrowsExceptionAsync<InvalidImageNameLengthException>(async () => await new StoreImage(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task NameNotTrimmed()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        var request = new StoreImage.Request(user, "   " + RandomHelper.String(), RandomHelper.String(), RandomHelper.String(), StoreImage.pngImageContentType, pngImage);
        await Assert.ThrowsExceptionAsync<ImageNameNotTrimmedException>(async () => await new StoreImage(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task DescriptionTooShort()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        var request = new StoreImage.Request(user, RandomHelper.String(), RandomHelper.String(QueryValidationHelper.ImageMinDescriptionLength - 1), RandomHelper.String(), StoreImage.pngImageContentType, pngImage);
        await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new StoreImage(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task DescriptionTooLong()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        var request = new StoreImage.Request(user, RandomHelper.String(), RandomHelper.String(QueryValidationHelper.ImageMaxDescriptionLength + 1), RandomHelper.String(), StoreImage.pngImageContentType, pngImage);
        await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new StoreImage(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task DescriptionNotTrimmed()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        var request = new StoreImage.Request(user, RandomHelper.String(), "   " + RandomHelper.String(), RandomHelper.String(), StoreImage.pngImageContentType, pngImage);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new StoreImage(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task SourceTooShort()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        var request = new StoreImage.Request(user, RandomHelper.String(), RandomHelper.String(), RandomHelper.String(QueryValidationHelper.ImageMinSourceLength - 1), StoreImage.pngImageContentType, pngImage);
        await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new StoreImage(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task SourceTooLong()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        var request = new StoreImage.Request(user, RandomHelper.String(), RandomHelper.String(), RandomHelper.String(QueryValidationHelper.ImageMaxSourceLength + 1), StoreImage.pngImageContentType, pngImage);
        await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new StoreImage(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task SourceNotTrimmed()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        var request = new StoreImage.Request(user, RandomHelper.String(), RandomHelper.String(), "\n" + RandomHelper.String(), StoreImage.pngImageContentType, pngImage);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new StoreImage(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task UnsupportedImageType()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        var request = new StoreImage.Request(user, RandomHelper.String(), RandomHelper.String(), RandomHelper.String(), RandomHelper.String(), pngImage);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new StoreImage(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task ImageTypeMismatch()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        var request = new StoreImage.Request(user, RandomHelper.String(), RandomHelper.String(), RandomHelper.String(), StoreImage.svgImageContentType, pngImage);
        await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new StoreImage(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task BlobTooShort()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        var request = new StoreImage.Request(user, RandomHelper.String(), RandomHelper.String(), RandomHelper.String(), StoreImage.pngImageContentType, RandomHelper.Bytes(StoreImage.Request.minBlobLength - 1));
        await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new StoreImage(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task BlobTooLong()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        var request = new StoreImage.Request(user, RandomHelper.String(), RandomHelper.String(), RandomHelper.String(), StoreImage.pngImageContentType, RandomHelper.Bytes(StoreImage.Request.maxBlobLength + 1));
        await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new StoreImage(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task ImageWithNameExists()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var name = RandomHelper.String();
        await ImageHelper.CreateAsync(db, user, name: name);

        using var dbContext = new MemCheckDbContext(db);
        var request = new StoreImage.Request(user, name, RandomHelper.String(), RandomHelper.String(), StoreImage.pngImageContentType, pngImage);
        await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new StoreImage(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task ImageWithNameInOtherCasingExists()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
#pragma warning disable CA1308 // Normalize strings to uppercase
        var name = RandomHelper.String().ToLowerInvariant();
#pragma warning restore CA1308 // Normalize strings to uppercase
        await ImageHelper.CreateAsync(db, user, name: name);

        using var dbContext = new MemCheckDbContext(db);
        var request = new StoreImage.Request(user, name.ToUpperInvariant(), RandomHelper.String(), RandomHelper.String(), StoreImage.pngImageContentType, pngImage);
        var errorString = RandomHelper.String();
        var localizer = new TestLocalizer("AlreadyExistsCaseInsensitive".PairedWith(errorString));
        var e = await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new StoreImage(dbContext.AsCallContext(localizer)).RunAsync(request));
        StringAssert.Contains(e.Message, errorString);
    }
    [DataTestMethod, DataRow('<'), DataRow('>'), DataRow('['), DataRow(']'), DataRow(','), DataRow('\t')]
    public async Task InvalidChar(char c)
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var name = RandomHelper.String() + c + RandomHelper.String();

        using var dbContext = new MemCheckDbContext(db);
        var errorString = RandomHelper.String();
        var localizer = new TestLocalizer("IsForbidden".PairedWith(errorString));
        var request = new StoreImage.Request(user, name, RandomHelper.String(), RandomHelper.String(), StoreImage.pngImageContentType, pngImage);
        var e = await Assert.ThrowsExceptionAsync<InvalidImageNameCharException>(async () => await new StoreImage(dbContext.AsCallContext(localizer)).RunAsync(request));
        StringAssert.Contains(e.Message, errorString);
        StringAssert.Contains(e.Message, c.ToString());
    }
    [DataTestMethod, DataRow("With space"), DataRow("#ss"), DataRow("sy-"), DataRow("d.d"), DataRow("fh("), DataRow("JKL)"), DataRow("78;"), DataRow("!!!!!"), DataRow("@oo"),
        DataRow("v&l"), DataRow("m=p"), DataRow("a+2"), DataRow("$33"), DataRow("pp/"), DataRow("%%%"), DataRow("2#&"), DataRow("aéo"), DataRow("aaï"), DataRow("Môle"), DataRow("Duc-d'Albe")]
    public async Task SuccessWithSpecialName(string name)
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var description = RandomHelper.String();
        var source = RandomHelper.String();
        var uploadDate = RandomHelper.Date();
        var versionDesc = RandomHelper.String();

        using (var dbContext = new MemCheckDbContext(db))
        {
            var request = new StoreImage.Request(user, name, description, source, StoreImage.pngImageContentType, pngImage);
            var localizer = new TestLocalizer("InitialImageVersionCreation".PairedWith(versionDesc));
            await new StoreImage(dbContext.AsCallContext(localizer), uploadDate).RunAsync(request);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var image = await dbContext.Images.Include(img => img.Owner).SingleAsync();
            Assert.AreEqual(user, image.Owner.Id);
            Assert.AreEqual(name, image.Name);
            Assert.AreEqual(description, image.Description);
            Assert.AreEqual(source, image.Source);
            Assert.AreEqual(uploadDate, image.InitialUploadUtcDate);
            Assert.AreEqual(uploadDate, image.LastChangeUtcDate);
            Assert.AreEqual(versionDesc, image.VersionDescription);
            Assert.AreEqual(ImageVersionType.Creation, image.VersionType);
            Assert.AreEqual(StoreImage.pngImageContentType, image.OriginalContentType);
            Assert.AreEqual(pngImage.Length, image.OriginalSize);
            Assert.IsTrue(pngImage.SequenceEqual(image.OriginalBlob));
            Assert.AreEqual(20, image.OriginalBlobSha1.Length);
            Assert.IsTrue(image.SmallBlobSize > 0);
            Assert.AreEqual(image.SmallBlobSize, image.SmallBlob.Length);
            Assert.IsTrue(image.MediumBlobSize > 0);
            Assert.AreEqual(image.MediumBlobSize, image.MediumBlob.Length);
            Assert.IsTrue(image.BigBlobSize > 0);
            Assert.AreEqual(image.BigBlobSize, image.BigBlob.Length);
            Assert.IsNull(image.PreviousVersion);
        }
    }
    [TestMethod()]
    public async Task SuccessWithRandomName()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var name = RandomHelper.String();
        var description = RandomHelper.String();
        var source = RandomHelper.String();
        var uploadDate = RandomHelper.Date();
        var versionDesc = RandomHelper.String();

        using (var dbContext = new MemCheckDbContext(db))
        {
            var request = new StoreImage.Request(user, name, description, source, StoreImage.pngImageContentType, pngImage);
            var localizer = new TestLocalizer("InitialImageVersionCreation".PairedWith(versionDesc));
            await new StoreImage(dbContext.AsCallContext(localizer), uploadDate).RunAsync(request);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var image = await dbContext.Images.Include(img => img.Owner).SingleAsync();
            Assert.AreEqual(user, image.Owner.Id);
            Assert.AreEqual(name, image.Name);
            Assert.AreEqual(description, image.Description);
            Assert.AreEqual(source, image.Source);
            Assert.AreEqual(uploadDate, image.InitialUploadUtcDate);
            Assert.AreEqual(uploadDate, image.LastChangeUtcDate);
            Assert.AreEqual(versionDesc, image.VersionDescription);
            Assert.AreEqual(ImageVersionType.Creation, image.VersionType);
            Assert.AreEqual(StoreImage.pngImageContentType, image.OriginalContentType);
            Assert.AreEqual(pngImage.Length, image.OriginalSize);
            Assert.IsTrue(pngImage.SequenceEqual(image.OriginalBlob));
            Assert.AreEqual(20, image.OriginalBlobSha1.Length);
            Assert.IsTrue(image.SmallBlobSize > 0);
            Assert.AreEqual(image.SmallBlobSize, image.SmallBlob.Length);
            Assert.IsTrue(image.MediumBlobSize > 0);
            Assert.AreEqual(image.MediumBlobSize, image.MediumBlob.Length);
            Assert.IsTrue(image.BigBlobSize > 0);
            Assert.AreEqual(image.BigBlobSize, image.BigBlob.Length);
            Assert.IsNull(image.PreviousVersion);
        }
    }
    [TestMethod()]
    public async Task SuccessWithOtherOneExisting()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);

        var firstImageGuid = await ImageHelper.CreateAsync(db, user);

        var name = RandomHelper.String();
        using (var dbContext = new MemCheckDbContext(db))
        {
            var request = new StoreImage.Request(user, name, RandomHelper.String(), RandomHelper.String(), StoreImage.pngImageContentType, pngImage);
            await new StoreImage(dbContext.AsCallContext()).RunAsync(request);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var secondImage = await dbContext.Images.Include(img => img.Owner).Where(img => img.Name == name).SingleAsync();
            Assert.AreEqual(user, secondImage.Owner.Id);

            var firstImage = await dbContext.Images.Where(img => img.Id == firstImageGuid).SingleAsync();

            Assert.IsFalse(firstImage.OriginalBlobSha1.SequenceEqual(secondImage.OriginalBlobSha1));
        }
    }
    [TestMethod()]
    public async Task FailureOnReAdding()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);

        var firstImageName = RandomHelper.String();

        using (var dbContext = new MemCheckDbContext(db))
        {
            var request = new StoreImage.Request(user, firstImageName, RandomHelper.String(), RandomHelper.String(), StoreImage.pngImageContentType, pngImage);
            await new StoreImage(dbContext.AsCallContext()).RunAsync(request);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var request = new StoreImage.Request(user, RandomHelper.String(), RandomHelper.String(), RandomHelper.String(), StoreImage.pngImageContentType, pngImage);
            var e = await Assert.ThrowsExceptionAsync<IOException>(async () => await new StoreImage(dbContext.AsCallContext()).RunAsync(request));
            StringAssert.Contains(e.Message, firstImageName);
        }
    }
}

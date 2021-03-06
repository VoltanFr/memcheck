﻿using MemCheck.Application.QueryValidation;
using MemCheck.Application.Tests.Helpers;
using MemCheck.Basics;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MemCheck.Application.Images
{
    [TestClass()]
    public class StoreImageTests
    {
        #region Fields
        private static readonly ImmutableArray<byte> pngImage = GetPngImage();
        #endregion
        #region Private methods
        private static ImmutableArray<byte> GetPngImage()
        {
            using Stream resFilestream = typeof(StoreImageTests).Assembly.GetManifestResourceStream("MemCheck.Application.Resources.Gray.png")!;
            byte[] result = new byte[resFilestream.Length];
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
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new StoreImage(dbContext, new TestLocalizer()).RunAsync(request));
        }
        [TestMethod()]
        public async Task UserDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            var request = new StoreImage.Request(Guid.NewGuid(), RandomHelper.String(), RandomHelper.String(), RandomHelper.String(), StoreImage.pngImageContentType, pngImage);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new StoreImage(dbContext, new TestLocalizer()).RunAsync(request));
        }
        [TestMethod()]
        public async Task NameTooShort()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            var request = new StoreImage.Request(user, RandomHelper.String(QueryValidationHelper.ImageMinNameLength - 1), RandomHelper.String(), RandomHelper.String(), StoreImage.pngImageContentType, pngImage);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new StoreImage(dbContext, new TestLocalizer()).RunAsync(request));
        }
        [TestMethod()]
        public async Task NameTooLong()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            var request = new StoreImage.Request(user, RandomHelper.String(QueryValidationHelper.ImageMaxNameLength + 1), RandomHelper.String(), RandomHelper.String(), StoreImage.pngImageContentType, pngImage);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new StoreImage(dbContext, new TestLocalizer()).RunAsync(request));
        }
        [TestMethod()]
        public async Task NameNotTrimmed()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            var request = new StoreImage.Request(user, "   " + RandomHelper.String(), RandomHelper.String(), RandomHelper.String(), StoreImage.pngImageContentType, pngImage);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new StoreImage(dbContext, new TestLocalizer()).RunAsync(request));
        }
        [TestMethod()]
        public async Task DescriptionTooShort()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            var request = new StoreImage.Request(user, RandomHelper.String(), RandomHelper.String(QueryValidationHelper.ImageMinDescriptionLength - 1), RandomHelper.String(), StoreImage.pngImageContentType, pngImage);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new StoreImage(dbContext, new TestLocalizer()).RunAsync(request));
        }
        [TestMethod()]
        public async Task DescriptionTooLong()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            var request = new StoreImage.Request(user, RandomHelper.String(), RandomHelper.String(QueryValidationHelper.ImageMaxDescriptionLength + 1), RandomHelper.String(), StoreImage.pngImageContentType, pngImage);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new StoreImage(dbContext, new TestLocalizer()).RunAsync(request));
        }
        [TestMethod()]
        public async Task DescriptionNotTrimmed()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            var request = new StoreImage.Request(user, RandomHelper.String(), "   " + RandomHelper.String(), RandomHelper.String(), StoreImage.pngImageContentType, pngImage);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new StoreImage(dbContext, new TestLocalizer()).RunAsync(request));
        }
        [TestMethod()]
        public async Task SourceTooShort()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            var request = new StoreImage.Request(user, RandomHelper.String(), RandomHelper.String(), RandomHelper.String(QueryValidationHelper.ImageMinSourceLength - 1), StoreImage.pngImageContentType, pngImage);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new StoreImage(dbContext, new TestLocalizer()).RunAsync(request));
        }
        [TestMethod()]
        public async Task SourceTooLong()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            var request = new StoreImage.Request(user, RandomHelper.String(), RandomHelper.String(), RandomHelper.String(QueryValidationHelper.ImageMaxSourceLength + 1), StoreImage.pngImageContentType, pngImage);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new StoreImage(dbContext, new TestLocalizer()).RunAsync(request));
        }
        [TestMethod()]
        public async Task SourceNotTrimmed()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            var request = new StoreImage.Request(user, RandomHelper.String(), RandomHelper.String(), "\n" + RandomHelper.String(), StoreImage.pngImageContentType, pngImage);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new StoreImage(dbContext, new TestLocalizer()).RunAsync(request));
        }
        [TestMethod()]
        public async Task UnsupportedImageType()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            var request = new StoreImage.Request(user, RandomHelper.String(), RandomHelper.String(), RandomHelper.String(), RandomHelper.String(), pngImage);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new StoreImage(dbContext, new TestLocalizer()).RunAsync(request));
        }
        [TestMethod()]
        public async Task ImageTypeMismatch()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            var request = new StoreImage.Request(user, RandomHelper.String(), RandomHelper.String(), RandomHelper.String(), StoreImage.svgImageContentType, pngImage);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new StoreImage(dbContext, new TestLocalizer()).RunAsync(request));
        }
        [TestMethod()]
        public async Task BlobTooShort()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            var request = new StoreImage.Request(user, RandomHelper.String(), RandomHelper.String(), RandomHelper.String(), StoreImage.pngImageContentType, RandomHelper.Bytes(StoreImage.Request.minBlobLength - 1));
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new StoreImage(dbContext, new TestLocalizer()).RunAsync(request));
        }
        [TestMethod()]
        public async Task BlobTooLong()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            var request = new StoreImage.Request(user, RandomHelper.String(), RandomHelper.String(), RandomHelper.String(), StoreImage.pngImageContentType, RandomHelper.Bytes(StoreImage.Request.maxBlobLength + 1));
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new StoreImage(dbContext, new TestLocalizer()).RunAsync(request));
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
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new StoreImage(dbContext, new TestLocalizer()).RunAsync(request));
        }
        [TestMethod()]
        public async Task Success()
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
                var localizer = new TestLocalizer(new KeyValuePair<string, string>("InitialImageVersionCreation", versionDesc).AsArray());
                await new StoreImage(dbContext, localizer).RunAsync(request, uploadDate);
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var image = await dbContext.Images.Include(img => img.Owner).Include(img => img.Cards).SingleAsync();
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
                Assert.IsTrue(image.SmallBlobSize > 0);
                Assert.AreEqual(image.SmallBlobSize, image.SmallBlob.Length);
                Assert.IsTrue(image.MediumBlobSize > 0);
                Assert.AreEqual(image.MediumBlobSize, image.MediumBlob.Length);
                Assert.IsTrue(image.BigBlobSize > 0);
                Assert.AreEqual(image.BigBlobSize, image.BigBlob.Length);
                Assert.IsFalse(image.Cards.Any());
                Assert.IsNull(image.PreviousVersion);
            }
        }
        [TestMethod()]
        public async Task SuccessWithOtherOneExisting()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            await ImageHelper.CreateAsync(db, user);

            var name = RandomHelper.String();
            using (var dbContext = new MemCheckDbContext(db))
            {
                var request = new StoreImage.Request(user, name, RandomHelper.String(), RandomHelper.String(), StoreImage.pngImageContentType, pngImage);
                await new StoreImage(dbContext, new TestLocalizer()).RunAsync(request);
            }

            using (var dbContext = new MemCheckDbContext(db))
                Assert.IsTrue(dbContext.Images.Any(img => img.Owner.Id == user));
        }
    }
}

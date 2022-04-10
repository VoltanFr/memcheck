﻿using MemCheck.Application.Cards;
using MemCheck.Application.Helpers;
using MemCheck.Application.QueryValidation;
using MemCheck.Basics;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Images
{
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
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new DeleteImage(dbContext.AsCallContext()).RunAsync(new DeleteImage.Request(Guid.Empty, image, RandomHelper.String())));
        }
        [TestMethod()]
        public async Task UserDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var image = await ImageHelper.CreateAsync(db, user);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new DeleteImage(dbContext.AsCallContext()).RunAsync(new DeleteImage.Request(Guid.NewGuid(), image, RandomHelper.String())));
        }
        [TestMethod()]
        public async Task ImageDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new DeleteImage(dbContext.AsCallContext()).RunAsync(new DeleteImage.Request(user, Guid.NewGuid(), RandomHelper.String())));
        }
        [TestMethod()]
        public async Task DescriptionTooShort()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var image = await ImageHelper.CreateAsync(db, user);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new DeleteImage(dbContext.AsCallContext()).RunAsync(new DeleteImage.Request(user, image, RandomHelper.String(DeleteImage.Request.MinDescriptionLength - 1))));
        }
        [TestMethod()]
        public async Task DescriptionTooLong()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var image = await ImageHelper.CreateAsync(db, user);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new DeleteImage(dbContext.AsCallContext()).RunAsync(new DeleteImage.Request(user, image, RandomHelper.String(DeleteImage.Request.MaxDescriptionLength + 1))));
        }
        [TestMethod()]
        public async Task DescriptionNotTrimmed()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var image = await ImageHelper.CreateAsync(db, user);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new DeleteImage(dbContext.AsCallContext()).RunAsync(new DeleteImage.Request(user, image, RandomHelper.String(DeleteImage.Request.MinDescriptionLength) + Environment.NewLine)));
        }
        [TestMethod()]
        public async Task UsedInCard()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var image = await ImageHelper.CreateAsync(db, user);
            await CardHelper.CreateAsync(db, user, additionalSideImages: image.AsArray());

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new DeleteImage(dbContext.AsCallContext()).RunAsync(new DeleteImage.Request(user, image, RandomHelper.String())));
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
                await new DeleteImage(dbContext.AsCallContext(), deletionDate).RunAsync(new DeleteImage.Request(user, image, deletionDescription));

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
        [TestMethod()]
        public async Task Complex()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var otherImage = await ImageHelper.CreateAsync(db, user);
            var imageName = RandomHelper.String();
            var imageCreationVersionDescription = RandomHelper.String();
            var imageUploadDate = RandomHelper.Date();
            var image = await ImageHelper.CreateAsync(db, user, imageName, imageCreationVersionDescription, lastChangeUtcDate: imageUploadDate);
            var card = await CardHelper.CreateAsync(db, user, frontSideImages: otherImage.AsArray(), additionalSideImages: image.AsArray());

            using (var dbContext = new MemCheckDbContext(db))
                await new DeleteCards(dbContext.AsCallContext()).RunAsync(new DeleteCards.Request(user, card.Id.AsArray()));

            var deletionDescription = RandomHelper.String();
            var deletionDate = RandomHelper.Date(imageUploadDate);
            using (var dbContext = new MemCheckDbContext(db))
                await new DeleteImage(dbContext.AsCallContext(), deletionDate).RunAsync(new DeleteImage.Request(user, image, deletionDescription));

            using (var dbContext = new MemCheckDbContext(db))
            {
                Assert.AreEqual(otherImage, dbContext.Images.Single().Id);

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

                Assert.IsFalse(await dbContext.ImagesInCardPreviousVersions.Where(imageInCardPreviousVersions => imageInCardPreviousVersions.ImageId == image).AnyAsync());
                Assert.IsTrue(await dbContext.ImagesInCardPreviousVersions.Where(imageInCardPreviousVersions => imageInCardPreviousVersions.ImageId == otherImage).AnyAsync());
            }
        }
    }
}

using MemCheck.Application.Tests.Helpers;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Images
{
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
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetImageInfoFromId(dbContext).RunAsync(new GetImageInfoFromId.Request(Guid.NewGuid())));
        }
        [TestMethod()]
        public async Task NotUsedInCards()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var name = RandomHelper.String();
            var source = RandomHelper.String();
            var description = RandomHelper.String();
            var uploadDate = RandomHelper.Date();
            var versionDescription = RandomHelper.String();
            var image = await ImageHelper.CreateAsync(db, user, name: name, source: source, description: description, lastChangeUtcDate: uploadDate, versionDescription: versionDescription);

            using var dbContext = new MemCheckDbContext(db);
            var loaded = await new GetImageInfoFromId(dbContext).RunAsync(new GetImageInfoFromId.Request(image));
            Assert.AreEqual(user, loaded.Owner.Id);
            Assert.AreEqual(name, loaded.Name);
            Assert.AreEqual(description, loaded.Description);
            Assert.AreEqual(source, loaded.Source);
            Assert.AreEqual(0, loaded.CardCount);
            Assert.AreEqual(uploadDate, loaded.InitialUploadUtcDate);
            Assert.AreEqual(uploadDate, loaded.LastChangeUtcDate);
            Assert.AreEqual(versionDescription, loaded.CurrentVersionDescription);
        }
        [TestMethod()]
        public async Task UsedInCards()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var name = RandomHelper.String();
            var source = RandomHelper.String();
            var description = RandomHelper.String();
            var uploadDate = RandomHelper.Date();
            var versionDescription = RandomHelper.String();
            var image = await ImageHelper.CreateAsync(db, user, name: name, source: source, description: description, lastChangeUtcDate: uploadDate, versionDescription: versionDescription);
            var otherImage = await ImageHelper.CreateAsync(db, user);
            await CardHelper.CreateAsync(db, user, frontSideImages: new[] { image, otherImage });
            await CardHelper.CreateAsync(db, user, frontSideImages: new[] { image });
            await CardHelper.CreateAsync(db, user, frontSideImages: new[] { otherImage });

            using var dbContext = new MemCheckDbContext(db);
            var loaded = await new GetImageInfoFromId(dbContext).RunAsync(new GetImageInfoFromId.Request(image));
            Assert.AreEqual(user, loaded.Owner.Id);
            Assert.AreEqual(name, loaded.Name);
            Assert.AreEqual(description, loaded.Description);
            Assert.AreEqual(source, loaded.Source);
            Assert.AreEqual(2, loaded.CardCount);
            Assert.AreEqual(uploadDate, loaded.InitialUploadUtcDate);
            Assert.AreEqual(uploadDate, loaded.LastChangeUtcDate);
            Assert.AreEqual(versionDescription, loaded.CurrentVersionDescription);
        }
    }
}

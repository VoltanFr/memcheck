using MemCheck.Application.Helpers;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Images
{
    [TestClass()]
    public class GetImageTests
    {
        [TestMethod()]
        public async Task ImageDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var image = await ImageHelper.CreateAsync(db, user);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetImage(dbContext.AsCallContext()).RunAsync(new GetImage.Request(Guid.NewGuid(), GetImage.Request.ImageSize.Medium)));
        }
        [TestMethod()]
        public async Task SmallBlob()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var image = await ImageHelper.CreateAsync(db, user);

            using var dbContext = new MemCheckDbContext(db);
            var loaded = await new GetImage(dbContext.AsCallContext()).RunAsync(new GetImage.Request(image, GetImage.Request.ImageSize.Small));
            Assert.AreEqual(1, loaded.ImageBytes.Length);
        }
        [TestMethod()]
        public async Task MediumBlob()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var image = await ImageHelper.CreateAsync(db, user);

            using var dbContext = new MemCheckDbContext(db);
            var loaded = await new GetImage(dbContext.AsCallContext()).RunAsync(new GetImage.Request(image, GetImage.Request.ImageSize.Medium));
            Assert.AreEqual(2, loaded.ImageBytes.Length);
        }
        [TestMethod()]
        public async Task BigBlob()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var image = await ImageHelper.CreateAsync(db, user);

            using var dbContext = new MemCheckDbContext(db);
            var loaded = await new GetImage(dbContext.AsCallContext()).RunAsync(new GetImage.Request(image, GetImage.Request.ImageSize.Big));
            Assert.AreEqual(3, loaded.ImageBytes.Length);
        }
    }
}

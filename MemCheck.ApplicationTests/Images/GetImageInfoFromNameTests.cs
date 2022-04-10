using MemCheck.Application.Helpers;
using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Images
{
    [TestClass()]
    public class GetImageInfoFromNameTests
    {
        [TestMethod()]
        public async Task NameEmpty()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var image = await ImageHelper.CreateAsync(db, user);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new GetImageInfoFromName(dbContext.AsCallContext()).RunAsync(new GetImageInfoFromName.Request("")));
        }
        [TestMethod()]
        public async Task NameNotTrimmed()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var image = await ImageHelper.CreateAsync(db, user);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetImageInfoFromName(dbContext.AsCallContext()).RunAsync(new GetImageInfoFromName.Request(RandomHelper.String() + ' ')));
        }
        [TestMethod()]
        public async Task ImageDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var image = await ImageHelper.CreateAsync(db, user);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new GetImageInfoFromName(dbContext.AsCallContext()).RunAsync(new GetImageInfoFromName.Request(RandomHelper.String())));
        }
        [TestMethod()]
        public async Task Success()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var name = RandomHelper.String();
            var source = RandomHelper.String();
            var image = await ImageHelper.CreateAsync(db, user, name: name, source: source);

            using var dbContext = new MemCheckDbContext(db);
            var loaded = await new GetImageInfoFromName(dbContext.AsCallContext()).RunAsync(new GetImageInfoFromName.Request(name));
            Assert.AreEqual(image, loaded.ImageId);
            Assert.AreEqual(name, loaded.Name);
            Assert.AreEqual(source, loaded.Source);
        }
        [TestMethod()]
        public async Task CaseInsensitivity()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var name = RandomHelper.String().ToLowerInvariant();
            Assert.AreNotEqual(name, name.ToUpperInvariant());
            var image = await ImageHelper.CreateAsync(db, user, name: name.ToLowerInvariant());

            using var dbContext = new MemCheckDbContext(db);
            var loaded = await new GetImageInfoFromName(dbContext.AsCallContext()).RunAsync(new GetImageInfoFromName.Request(name));
            Assert.AreEqual(image, loaded.ImageId);
            Assert.AreEqual(name, loaded.Name);
        }
    }
}

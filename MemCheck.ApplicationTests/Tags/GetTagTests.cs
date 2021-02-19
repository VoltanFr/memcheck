using MemCheck.Application.Tests.Helpers;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Tags
{
    [TestClass()]
    public class GetTagTests
    {
        [TestMethod()]
        public async Task DoesNotExist()
        {
            using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetTag(dbContext).RunAsync(new GetTag.Request(Guid.NewGuid())));
        }
        [TestMethod()]
        public async Task TagNotUsedInCards()
        {
            var db = DbHelper.GetEmptyTestDB();
            var tagName = RandomHelper.String();
            var description = RandomHelper.String();
            var tag = await TagHelper.CreateAsync(db, tagName, description);

            using var dbContext = new MemCheckDbContext(db);
            var loadedTag = await new GetTag(dbContext).RunAsync(new GetTag.Request(tag));

            Assert.AreEqual(tag, loadedTag.TagId);
            Assert.AreEqual(tagName, loadedTag.TagName);
            Assert.AreEqual(description, loadedTag.Description);
            Assert.AreEqual(0, loadedTag.CardCount);
        }
        [TestMethod()]
        public async Task TagUsedInCards()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var otherTag = await TagHelper.CreateAsync(db);
            var tagName = RandomHelper.String();
            var description = RandomHelper.String();
            var tag = await TagHelper.CreateAsync(db, tagName, description);

            await CardHelper.CreateAsync(db, user, tagIds: new[] { tag, otherTag });
            await CardHelper.CreateAsync(db, user, tagIds: new[] { tag });
            await CardHelper.CreateAsync(db, user);

            using var dbContext = new MemCheckDbContext(db);
            var loadedTag = await new GetTag(dbContext).RunAsync(new GetTag.Request(tag));

            Assert.AreEqual(tag, loadedTag.TagId);
            Assert.AreEqual(tagName, loadedTag.TagName);
            Assert.AreEqual(description, loadedTag.Description);
            Assert.AreEqual(2, loadedTag.CardCount);
        }
    }
}

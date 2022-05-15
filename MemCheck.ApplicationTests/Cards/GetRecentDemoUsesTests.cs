using MemCheck.Application.Helpers;
using MemCheck.Basics;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards
{
    [TestClass()]
    public class GetRecentDemoUsesTests
    {
        [TestMethod()]
        public async Task NoRecentUse()
        {
            var db = DbHelper.GetEmptyTestDB();

            var user = await UserHelper.CreateInDbAsync(db);
            var tagId = await TagHelper.CreateAsync(db);
            await CardHelper.CreateIdAsync(db, user, tagIds: tagId.AsArray());

            var demoRunDate = RandomHelper.Date();

            using (var dbContext = new MemCheckDbContext(db))
                await new GetCardsForDemo(dbContext.AsCallContext(), demoRunDate).RunAsync(new GetCardsForDemo.Request(tagId, Array.Empty<Guid>(), 10));

            using (var dbContext = new MemCheckDbContext(db))
            {
                var request = new GetRecentDemoUses.Request(1);
                var result = await new GetRecentDemoUses(dbContext.AsCallContext(), demoRunDate.AddDays(2)).RunAsync(request);
                Assert.AreEqual(0, result.Entries.Length);
            }
        }
        [TestMethod()]
        public async Task OneRecentUseReturnedACard()
        {
            var db = DbHelper.GetEmptyTestDB();

            var user = await UserHelper.CreateInDbAsync(db);
            var tagId = await TagHelper.CreateAsync(db);
            await CardHelper.CreateIdAsync(db, user, tagIds: tagId.AsArray());

            var demoRunDate = RandomHelper.Date();

            using (var dbContext = new MemCheckDbContext(db))
                await new GetCardsForDemo(dbContext.AsCallContext(), demoRunDate).RunAsync(new GetCardsForDemo.Request(tagId, Array.Empty<Guid>(), 10));

            using (var dbContext = new MemCheckDbContext(db))
            {
                var request = new GetRecentDemoUses.Request(1);
                var result = await new GetRecentDemoUses(dbContext.AsCallContext(), demoRunDate.AddHours(12)).RunAsync(request);
                Assert.AreEqual(1, result.Entries.Length);
                var entry = result.Entries.Single();
                Assert.AreEqual(demoRunDate, entry.DownloadUtcDate);
                Assert.AreEqual(tagId, entry.TagId);
                Assert.AreEqual(1, entry.CountOfCardsReturned);
            }
        }
        [TestMethod()]
        public async Task OneRecentUseReturnedNoCard()
        {
            var db = DbHelper.GetEmptyTestDB();

            var user = await UserHelper.CreateInDbAsync(db);
            var tagOnCardId = await TagHelper.CreateAsync(db);
            await CardHelper.CreateIdAsync(db, user, tagIds: tagOnCardId.AsArray());

            var demoRunDate = RandomHelper.Date();
            var tagUsedInDemoId = await TagHelper.CreateAsync(db);

            using (var dbContext = new MemCheckDbContext(db))
                await new GetCardsForDemo(dbContext.AsCallContext(), demoRunDate).RunAsync(new GetCardsForDemo.Request(tagUsedInDemoId, Array.Empty<Guid>(), 10));

            using (var dbContext = new MemCheckDbContext(db))
            {
                var request = new GetRecentDemoUses.Request(1);
                var result = await new GetRecentDemoUses(dbContext.AsCallContext(), demoRunDate.AddHours(12)).RunAsync(request);
                Assert.AreEqual(1, result.Entries.Length);
                var entry = result.Entries.Single();
                Assert.AreEqual(demoRunDate, entry.DownloadUtcDate);
                Assert.AreEqual(tagUsedInDemoId, entry.TagId);
                Assert.AreEqual(0, entry.CountOfCardsReturned);
            }
        }
        [TestMethod()]
        public async Task ComplexCase()
        {
            var db = DbHelper.GetEmptyTestDB();

            var user = await UserHelper.CreateInDbAsync(db);
            var cardCount = RandomHelper.Int(50, 100);

            var tag1 = await TagHelper.CreateAsync(db);
            var tag2 = await TagHelper.CreateAsync(db);

            await CardHelper.CreateIdAsync(db, user, tagIds: tag1.AsArray());
            await CardHelper.CreateIdAsync(db, user, tagIds: tag2.AsArray());

            var countOfCardsWithTag1 = 1;
            var countOfCardsWithTag2 = 1;

            for (int cardIndex = 0; cardIndex < cardCount; cardIndex++)
            {
                var tagChoice = RandomHelper.Int(1, 4);
                switch (tagChoice)
                {
                    case 1:
                        await CardHelper.CreateIdAsync(db, user, tagIds: tag1.AsArray());
                        countOfCardsWithTag1++;
                        break;
                    case 2:
                        await CardHelper.CreateIdAsync(db, user, tagIds: tag2.AsArray());
                        countOfCardsWithTag2++;
                        break;
                    case 3:
                        await CardHelper.CreateIdAsync(db, user, tagIds: new[] { tag1, tag2 });
                        countOfCardsWithTag1++;
                        countOfCardsWithTag2++;
                        break;
                    default:
                        await CardHelper.CreateIdAsync(db, user, tagIds: Array.Empty<Guid>());
                        break;
                }
            }

            var getRunDate = RandomHelper.Date();

            var demoRunDate1 = getRunDate.AddDays(-3.5);
            var countRequestedInDemo1OnTag1 = Math.Min(5, countOfCardsWithTag1);
            using (var dbContext = new MemCheckDbContext(db))
            {
                await new GetCardsForDemo(dbContext.AsCallContext(), demoRunDate1).RunAsync(new GetCardsForDemo.Request(tag1, Array.Empty<Guid>(), countRequestedInDemo1OnTag1));
                await new GetCardsForDemo(dbContext.AsCallContext(), demoRunDate1).RunAsync(new GetCardsForDemo.Request(tag2, Array.Empty<Guid>(), GetCardsForDemo.Request.MaxCount));
            }

            var demoRunDate2 = getRunDate.AddDays(-1.5);
            using (var dbContext = new MemCheckDbContext(db))
            {
                await new GetCardsForDemo(dbContext.AsCallContext(), demoRunDate2).RunAsync(new GetCardsForDemo.Request(tag1, Array.Empty<Guid>(), GetCardsForDemo.Request.MaxCount));
                await new GetCardsForDemo(dbContext.AsCallContext(), demoRunDate2).RunAsync(new GetCardsForDemo.Request(tag2, Array.Empty<Guid>(), GetCardsForDemo.Request.MaxCount));
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var request = new GetRecentDemoUses.Request(1);
                var result = await new GetRecentDemoUses(dbContext.AsCallContext(), getRunDate).RunAsync(request);
                Assert.IsFalse(result.Entries.Any());
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var request = new GetRecentDemoUses.Request(2);
                var result = await new GetRecentDemoUses(dbContext.AsCallContext(), getRunDate).RunAsync(request);
                Assert.AreEqual(2, result.Entries.Length);

                var entryForTag1 = result.Entries.Single(entry => entry.TagId == tag1);
                Assert.AreEqual(demoRunDate2, entryForTag1.DownloadUtcDate);
                Assert.AreEqual(countOfCardsWithTag1, entryForTag1.CountOfCardsReturned);

                var entryForTag2 = result.Entries.Single(entry => entry.TagId == tag2);
                Assert.AreEqual(demoRunDate2, entryForTag2.DownloadUtcDate);
                Assert.AreEqual(countOfCardsWithTag2, entryForTag2.CountOfCardsReturned);
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var request = new GetRecentDemoUses.Request(4);
                var result = await new GetRecentDemoUses(dbContext.AsCallContext(), getRunDate).RunAsync(request);
                Assert.AreEqual(4, result.Entries.Length);

                var entryForTag1Date2 = result.Entries.Single(entry => entry.TagId == tag1 && entry.DownloadUtcDate == demoRunDate2);
                Assert.AreEqual(countOfCardsWithTag1, entryForTag1Date2.CountOfCardsReturned);

                var entryForTag2Date2 = result.Entries.Single(entry => entry.TagId == tag2 && entry.DownloadUtcDate == demoRunDate2);
                Assert.AreEqual(countOfCardsWithTag2, entryForTag2Date2.CountOfCardsReturned);

                var entryForTag1Date1 = result.Entries.Single(entry => entry.TagId == tag1 && entry.DownloadUtcDate == demoRunDate1);
                Assert.AreEqual(countRequestedInDemo1OnTag1, entryForTag1Date1.CountOfCardsReturned);

                var entryForTag2Date1 = result.Entries.Single(entry => entry.TagId == tag2 && entry.DownloadUtcDate == demoRunDate2);
                Assert.AreEqual(countOfCardsWithTag2, entryForTag2Date1.CountOfCardsReturned);
            }
        }
    }
}

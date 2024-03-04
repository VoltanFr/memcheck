using MemCheck.Application.Helpers;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards;

[TestClass()]
public class PreviousCardDiscussionEntryVersionCreatorTests
{
    [TestMethod()]
    public async Task TestSameUserCreatesVersionOfDiscussionEntry_NoPreviousVersion()
    {
        var db = DbHelper.GetEmptyTestDB();

        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId);

        var entry = await CardDiscussionHelper.CreateEntryAsync(db, userId, cardId);

        using (var dbContext = new MemCheckDbContext(db))
        {
            var request = new PreviousCardDiscussionEntryVersionCreator(dbContext);
            await request.RunAsync(entry.Id);
            await dbContext.SaveChangesAsync();
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var previousVersion = await dbContext.CardDiscussionEntryPreviousVersions
                .Include(entry => entry.Creator)
                .Include(entry => entry.PreviousVersion)
                .Where(previous => previous.Card == cardId)
                .SingleAsync();
            Assert.AreNotEqual(Guid.Empty, previousVersion.Id);
            Assert.AreNotEqual(cardId, previousVersion.Id);
            Assert.AreEqual(cardId, previousVersion.Card);
            Assert.AreEqual(userId, previousVersion.Creator.Id);
            Assert.AreEqual(entry.Text, previousVersion.Text);
            Assert.AreEqual(entry.CreationUtcDate, previousVersion.CreationUtcDate);
            Assert.IsNull(previousVersion.PreviousVersion);
        }
    }
    [TestMethod()]
    public async Task TestSameUserCreatesVersionOfDiscussionEntry_OnePreviousVersionExists()
    {
        var db = DbHelper.GetEmptyTestDB();

        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId);

        var originalEntry = await CardDiscussionHelper.CreateEntryAsync(db, userId, cardId);

        var editText = RandomHelper.String();
        var editDate = RandomHelper.Date();
        using (var dbContext = new MemCheckDbContext(db))
            await new EditCardDiscussionEntry(dbContext.AsCallContext(), editDate).RunAsync(new EditCardDiscussionEntry.Request(userId, originalEntry.Id, editText));

        using (var dbContext = new MemCheckDbContext(db))
        {
            var request = new PreviousCardDiscussionEntryVersionCreator(dbContext);
            await request.RunAsync(originalEntry.Id);
            await dbContext.SaveChangesAsync();
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var card = await dbContext.Cards.Include(card => card.LatestDiscussionEntry).SingleAsync();
            Assert.IsNotNull(card.LatestDiscussionEntry);

            var currentVersion = await dbContext.CardDiscussionEntries
                .Include(entry => entry.Creator)
                .Include(entry => entry.PreviousVersion)
                .Where(entry => entry.Id == card.LatestDiscussionEntry.Id)
                .SingleAsync();
            Assert.IsNotNull(currentVersion.PreviousVersion);

            var previousVersion = await dbContext.CardDiscussionEntryPreviousVersions
                .Include(entry => entry.Creator)
                .Include(entry => entry.PreviousVersion)
                .Where(entry => entry.Id == currentVersion.PreviousVersion.Id)
                .SingleAsync();
            Assert.IsNotNull(previousVersion.PreviousVersion);

            var previousPreviousVersion = await dbContext.CardDiscussionEntryPreviousVersions
                .Include(entry => entry.Creator)
                .Include(entry => entry.PreviousVersion)
                .Where(entry => entry.Id == previousVersion.PreviousVersion.Id)
                .SingleAsync();
            Assert.IsNull(previousPreviousVersion.PreviousVersion);
        }
    }
    [TestMethod()]
    public async Task TestSameUserCreatesVersionOfDiscussionEntry_TwoPreviousVersionExist()
    {
        var db = DbHelper.GetEmptyTestDB();

        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId);

        var originalEntry = await CardDiscussionHelper.CreateEntryAsync(db, userId, cardId);

        var editText = RandomHelper.String();
        var editDate = RandomHelper.Date();
        using (var dbContext = new MemCheckDbContext(db))
            await new EditCardDiscussionEntry(dbContext.AsCallContext(), editDate).RunAsync(new EditCardDiscussionEntry.Request(userId, originalEntry.Id, editText));

        using (var dbContext = new MemCheckDbContext(db))
        {
            var request = new PreviousCardDiscussionEntryVersionCreator(dbContext);
            await request.RunAsync(originalEntry.Id);
            await dbContext.SaveChangesAsync();
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var request = new PreviousCardDiscussionEntryVersionCreator(dbContext);
            await request.RunAsync(originalEntry.Id);
            await dbContext.SaveChangesAsync();
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var card = await dbContext.Cards.Include(card => card.LatestDiscussionEntry).SingleAsync();
            Assert.IsNotNull(card.LatestDiscussionEntry);

            var currentVersion = await dbContext.CardDiscussionEntries
                .Include(entry => entry.Creator)
                .Include(entry => entry.PreviousVersion)
                .Where(entry => entry.Id == card.LatestDiscussionEntry.Id)
                .SingleAsync();
            Assert.IsNotNull(currentVersion.PreviousVersion);

            var previousVersion = await dbContext.CardDiscussionEntryPreviousVersions
                .Include(entry => entry.Creator)
                .Include(entry => entry.PreviousVersion)
                .Where(entry => entry.Id == currentVersion.PreviousVersion.Id)
                .SingleAsync();
            Assert.IsNotNull(previousVersion.PreviousVersion);

            var previousPreviousVersion = await dbContext.CardDiscussionEntryPreviousVersions
                .Include(entry => entry.Creator)
                .Include(entry => entry.PreviousVersion)
                .Where(entry => entry.Id == previousVersion.PreviousVersion.Id)
                .SingleAsync();
            Assert.IsNotNull(previousPreviousVersion.PreviousVersion);

            var previousPreviousPreviousVersion = await dbContext.CardDiscussionEntryPreviousVersions
                .Include(entry => entry.Creator)
                .Include(entry => entry.PreviousVersion)
                .Where(entry => entry.Id == previousPreviousVersion.PreviousVersion.Id)
                .SingleAsync();
            Assert.IsNull(previousPreviousPreviousVersion.PreviousVersion);
        }
    }
}

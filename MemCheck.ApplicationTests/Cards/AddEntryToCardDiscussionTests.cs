using MemCheck.Application.Helpers;
using MemCheck.Application.QueryValidation;
using MemCheck.Basics;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards;

[TestClass()]
public class AddEntryToCardDiscussionTests
{
    [TestMethod()]
    public async Task UserNotLoggedIn()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<NonexistentUserException>(async () => await new AddEntryToCardDiscussion(dbContext.AsCallContext()).RunAsync(new AddEntryToCardDiscussion.Request(Guid.Empty, cardId, RandomHelper.String())));
    }
    [TestMethod()]
    public async Task UserDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<NonexistentUserException>(async () => await new AddEntryToCardDiscussion(dbContext.AsCallContext()).RunAsync(new AddEntryToCardDiscussion.Request(RandomHelper.Guid(), cardId, RandomHelper.String())));
    }
    [TestMethod()]
    public async Task UserUnregistered()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId);
        await UserHelper.DeleteAsync(db, userId);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<NonexistentUserException>(async () => await new AddEntryToCardDiscussion(dbContext.AsCallContext()).RunAsync(new AddEntryToCardDiscussion.Request(RandomHelper.Guid(), cardId, RandomHelper.String())));
    }
    [TestMethod()]
    public async Task CardDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<NonexistentCardException>(async () => await new AddEntryToCardDiscussion(dbContext.AsCallContext()).RunAsync(new AddEntryToCardDiscussion.Request(userId, RandomHelper.Guid(), RandomHelper.String())));
    }
    [TestMethod()]
    public async Task CardIsDeleted()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId, userWithViewIds: userId.AsArray());

        await CardDeletionHelper.DeleteCardAsync(db, userId, cardId);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<NonexistentCardException>(async () => await new AddEntryToCardDiscussion(dbContext.AsCallContext()).RunAsync(new AddEntryToCardDiscussion.Request(userId, cardId, RandomHelper.String())));
    }
    [TestMethod()]
    public async Task CardIsNotViewableByUser()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId, userWithViewIds: userId.AsArray());
        var otherUserId = await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<UserNotAllowedToAccessCardException>(async () => await new AddEntryToCardDiscussion(dbContext.AsCallContext()).RunAsync(new AddEntryToCardDiscussion.Request(otherUserId, cardId, RandomHelper.String())));
    }
    [TestMethod()]
    public async Task EmptyText()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId, userWithViewIds: userId.AsArray());

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<TextTooShortException>(async () => await new AddEntryToCardDiscussion(dbContext.AsCallContext()).RunAsync(new AddEntryToCardDiscussion.Request(userId, cardId, "")));
    }
    [TestMethod()]
    public async Task TextStartsWithBlanks()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId, userWithViewIds: userId.AsArray());

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<TextNotTrimmedException>(async () => await new AddEntryToCardDiscussion(dbContext.AsCallContext()).RunAsync(new AddEntryToCardDiscussion.Request(userId, cardId, " " + RandomHelper.String())));
        await Assert.ThrowsExceptionAsync<TextNotTrimmedException>(async () => await new AddEntryToCardDiscussion(dbContext.AsCallContext()).RunAsync(new AddEntryToCardDiscussion.Request(userId, cardId, "\t " + RandomHelper.String())));
    }
    [TestMethod()]
    public async Task TextEndsWithBlanks()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId, userWithViewIds: userId.AsArray());

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<TextNotTrimmedException>(async () => await new AddEntryToCardDiscussion(dbContext.AsCallContext()).RunAsync(new AddEntryToCardDiscussion.Request(userId, cardId, RandomHelper.String() + "\n\r")));
        await Assert.ThrowsExceptionAsync<TextNotTrimmedException>(async () => await new AddEntryToCardDiscussion(dbContext.AsCallContext()).RunAsync(new AddEntryToCardDiscussion.Request(userId, cardId, RandomHelper.String() + " \t")));
    }
    [TestMethod()]
    public async Task TextTooLong()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId, userWithViewIds: userId.AsArray());

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<TextTooLongException>(async () => await new AddEntryToCardDiscussion(dbContext.AsCallContext()).RunAsync(new AddEntryToCardDiscussion.Request(userId, cardId, RandomHelper.String(QueryValidationHelper.CardDiscussionMaxTextLength + 1))));
    }
    [TestMethod()]
    public async Task TextSuccess_FirstEntryForCard()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId, userWithViewIds: userId.AsArray());
        var text = RandomHelper.String();
        var runDate = RandomHelper.Date();

        Guid entryId;
        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new AddEntryToCardDiscussion(dbContext.AsCallContext(), runDate).RunAsync(new AddEntryToCardDiscussion.Request(userId, cardId, text));
            Assert.AreEqual(1, result.EntryCountForCard);
            entryId = result.EntryId;
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var discussionEntryFromDb = await dbContext.CardDiscussionEntries.Include(entry => entry.Creator).SingleAsync();
            Assert.AreEqual(entryId, discussionEntryFromDb.Id);
            Assert.AreEqual(cardId, discussionEntryFromDb.Card);
            Assert.AreEqual(userId, discussionEntryFromDb.Creator.Id);
            Assert.AreEqual(text, discussionEntryFromDb.Text);
            Assert.AreEqual(runDate, discussionEntryFromDb.CreationUtcDate);
            Assert.IsNull(discussionEntryFromDb.PreviousVersion);
            Assert.IsNull(discussionEntryFromDb.PreviousEntry);

            var cardFromDb = await dbContext.Cards.SingleAsync();
            Assert.IsNotNull(cardFromDb.LatestDiscussionEntry);
            Assert.AreEqual(discussionEntryFromDb.Id, cardFromDb.LatestDiscussionEntry.Id);
            Assert.AreEqual(runDate, cardFromDb.LatestDiscussionEntry.CreationUtcDate);
        }
    }
    [TestMethod()]
    public async Task TextSuccess_SecondEntryForCard()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId);
        var oldestEntryText = RandomHelper.String();
        var oldestEntryRunDate = RandomHelper.Date();
        var newestEntryText = RandomHelper.String();
        var newestEntryRunDate = RandomHelper.Date(oldestEntryRunDate);

        Guid oldestEntryId;
        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new AddEntryToCardDiscussion(dbContext.AsCallContext(), oldestEntryRunDate).RunAsync(new AddEntryToCardDiscussion.Request(userId, cardId, oldestEntryText));
            oldestEntryId = result.EntryId;
        }

        Guid newestEntryId;
        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new AddEntryToCardDiscussion(dbContext.AsCallContext(), newestEntryRunDate).RunAsync(new AddEntryToCardDiscussion.Request(userId, cardId, newestEntryText));
            newestEntryId = result.EntryId;
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var cardFromDb = await dbContext.Cards.Include(card => card.LatestDiscussionEntry).SingleAsync();
            Assert.IsNotNull(cardFromDb.LatestDiscussionEntry);

            Assert.AreEqual(2, dbContext.CardDiscussionEntries.Count());

            var newestDiscussionEntryFromDb = await dbContext.CardDiscussionEntries.Include(entry => entry.Creator).Include(entry => entry.PreviousEntry).Where(entry => entry.Id == cardFromDb.LatestDiscussionEntry.Id).SingleAsync();
            {
                Assert.AreEqual(newestEntryId, newestDiscussionEntryFromDb.Id);
                Assert.AreEqual(cardId, newestDiscussionEntryFromDb.Card);
                Assert.AreEqual(userId, newestDiscussionEntryFromDb.Creator.Id);
                Assert.AreEqual(newestEntryText, newestDiscussionEntryFromDb.Text);
                Assert.AreEqual(newestEntryRunDate, newestDiscussionEntryFromDb.CreationUtcDate);
                Assert.IsNull(newestDiscussionEntryFromDb.PreviousVersion);
                Assert.IsNotNull(newestDiscussionEntryFromDb.PreviousEntry);
            }
            {
                var oldestDiscussionEntryFromDb = await dbContext.CardDiscussionEntries.Include(entry => entry.Creator).Where(entry => entry.Id == newestDiscussionEntryFromDb.PreviousEntry.Id).SingleAsync();
                Assert.AreEqual(oldestEntryId, oldestDiscussionEntryFromDb.Id);
                Assert.AreEqual(cardId, oldestDiscussionEntryFromDb.Card);
                Assert.AreEqual(userId, oldestDiscussionEntryFromDb.Creator.Id);
                Assert.AreEqual(oldestEntryText, oldestDiscussionEntryFromDb.Text);
                Assert.AreEqual(oldestEntryRunDate, oldestDiscussionEntryFromDb.CreationUtcDate);
                Assert.IsNull(oldestDiscussionEntryFromDb.PreviousVersion);
                Assert.IsNull(oldestDiscussionEntryFromDb.PreviousEntry);
            }
        }
    }
    [TestMethod()]
    public async Task TextSuccess_ThirdEntryForCard_TwoUsers()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user1Id = await UserHelper.CreateInDbAsync(db);
        var user2Id = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, user1Id);
        var oldestEntryText = RandomHelper.String();
        var oldestEntryRunDate = RandomHelper.Date();
        var intermediaryEntryText = RandomHelper.String();
        var intermediaryEntryRunDate = RandomHelper.Date(oldestEntryRunDate);
        var newestEntryText = RandomHelper.String();
        var newestEntryRunDate = RandomHelper.Date(intermediaryEntryRunDate);

        Guid oldestEntryId;
        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new AddEntryToCardDiscussion(dbContext.AsCallContext(), oldestEntryRunDate).RunAsync(new AddEntryToCardDiscussion.Request(user1Id, cardId, oldestEntryText));
            oldestEntryId = result.EntryId;
        }

        Guid intermediaryEntryId;
        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new AddEntryToCardDiscussion(dbContext.AsCallContext(), intermediaryEntryRunDate).RunAsync(new AddEntryToCardDiscussion.Request(user2Id, cardId, intermediaryEntryText));
            intermediaryEntryId = result.EntryId;
        }

        Guid newestEntryId;
        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new AddEntryToCardDiscussion(dbContext.AsCallContext(), newestEntryRunDate).RunAsync(new AddEntryToCardDiscussion.Request(user1Id, cardId, newestEntryText));
            newestEntryId = result.EntryId;
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var cardFromDb = await dbContext.Cards.Include(card => card.LatestDiscussionEntry).SingleAsync();
            Assert.IsNotNull(cardFromDb.LatestDiscussionEntry);
            Assert.AreEqual(newestEntryId, cardFromDb.LatestDiscussionEntry.Id);

            Assert.AreEqual(3, dbContext.CardDiscussionEntries.Count());

            var newestDiscussionEntryFromDb = await dbContext.CardDiscussionEntries.Include(entry => entry.Creator).Include(entry => entry.PreviousEntry).Where(entry => entry.Id == cardFromDb.LatestDiscussionEntry.Id).SingleAsync();
            {
                Assert.AreEqual(newestEntryId, newestDiscussionEntryFromDb.Id);
                Assert.AreEqual(cardId, newestDiscussionEntryFromDb.Card);
                Assert.AreEqual(user1Id, newestDiscussionEntryFromDb.Creator.Id);
                Assert.AreEqual(newestEntryText, newestDiscussionEntryFromDb.Text);
                Assert.AreEqual(newestEntryRunDate, newestDiscussionEntryFromDb.CreationUtcDate);
                Assert.IsNull(newestDiscussionEntryFromDb.PreviousVersion);
                Assert.IsNotNull(newestDiscussionEntryFromDb.PreviousEntry);
            }
            var intermediaryDiscussionEntryFromDb = await dbContext.CardDiscussionEntries.Include(entry => entry.Creator).Include(entry => entry.PreviousEntry).Where(entry => entry.Id == newestDiscussionEntryFromDb.PreviousEntry.Id).SingleAsync();
            {
                Assert.AreEqual(intermediaryEntryId, intermediaryDiscussionEntryFromDb.Id);
                Assert.AreEqual(cardId, intermediaryDiscussionEntryFromDb.Card);
                Assert.AreEqual(user2Id, intermediaryDiscussionEntryFromDb.Creator.Id);
                Assert.AreEqual(intermediaryEntryText, intermediaryDiscussionEntryFromDb.Text);
                Assert.AreEqual(intermediaryEntryRunDate, intermediaryDiscussionEntryFromDb.CreationUtcDate);
                Assert.IsNull(intermediaryDiscussionEntryFromDb.PreviousVersion);
                Assert.IsNotNull(intermediaryDiscussionEntryFromDb.PreviousEntry);
            }
            {
                var oldestDiscussionEntryFromDb = await dbContext.CardDiscussionEntries.Include(entry => entry.Creator).Where(entry => entry.Id == intermediaryDiscussionEntryFromDb.PreviousEntry.Id).SingleAsync();
                Assert.AreEqual(oldestEntryId, oldestDiscussionEntryFromDb.Id);
                Assert.AreEqual(cardId, oldestDiscussionEntryFromDb.Card);
                Assert.AreEqual(user1Id, oldestDiscussionEntryFromDb.Creator.Id);
                Assert.AreEqual(oldestEntryText, oldestDiscussionEntryFromDb.Text);
                Assert.AreEqual(oldestEntryRunDate, oldestDiscussionEntryFromDb.CreationUtcDate);
                Assert.IsNull(oldestDiscussionEntryFromDb.PreviousVersion);
                Assert.IsNull(oldestDiscussionEntryFromDb.PreviousEntry);
            }
        }
    }
}

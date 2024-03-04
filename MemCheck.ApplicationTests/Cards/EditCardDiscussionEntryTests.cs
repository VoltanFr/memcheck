using MemCheck.Application.Helpers;
using MemCheck.Application.QueryValidation;
using MemCheck.Basics;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards;

[TestClass()]
public class EditCardDiscussionEntryTests
{
    [TestMethod()]
    public async Task UserNotLoggedIn()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId);
        var entryId = (await CardDiscussionHelper.CreateEntryAsync(db, userId, cardId)).Id;

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<NonexistentUserException>(async () => await new EditCardDiscussionEntry(dbContext.AsCallContext()).RunAsync(new EditCardDiscussionEntry.Request(Guid.Empty, entryId, RandomHelper.String())));
    }
    [TestMethod()]
    public async Task UserDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId);
        var entryId = (await CardDiscussionHelper.CreateEntryAsync(db, userId, cardId)).Id;

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<NonexistentUserException>(async () => await new EditCardDiscussionEntry(dbContext.AsCallContext()).RunAsync(new EditCardDiscussionEntry.Request(Guid.Empty, entryId, RandomHelper.String())));
    }
    [TestMethod()]
    public async Task UserUnregistered()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId);
        var entryId = (await CardDiscussionHelper.CreateEntryAsync(db, userId, cardId)).Id;

        await UserHelper.DeleteAsync(db, userId);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<NonexistentUserException>(async () => await new EditCardDiscussionEntry(dbContext.AsCallContext()).RunAsync(new EditCardDiscussionEntry.Request(Guid.Empty, entryId, RandomHelper.String())));
    }
    [TestMethod()]
    public async Task EntryDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<NonexistentCardDiscussionEntryException>(async () => await new EditCardDiscussionEntry(dbContext.AsCallContext()).RunAsync(new EditCardDiscussionEntry.Request(userId, RandomHelper.Guid(), RandomHelper.String())));
    }
    [TestMethod()]
    public async Task CardIsDeleted()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId, userWithViewIds: userId.AsArray());
        var entryId = (await CardDiscussionHelper.CreateEntryAsync(db, userId, cardId)).Id;

        await CardDeletionHelper.DeleteCardAsync(db, userId, cardId);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<NonexistentCardException>(async () => await new EditCardDiscussionEntry(dbContext.AsCallContext()).RunAsync(new EditCardDiscussionEntry.Request(userId, entryId, RandomHelper.String())));
    }
    [TestMethod()]
    public async Task CardIsNotViewableByUser()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId, userWithViewIds: userId.AsArray());
        var entryId = (await CardDiscussionHelper.CreateEntryAsync(db, userId, cardId)).Id;
        var otherUserId = await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<UserNotAllowedToAccessCardException>(async () => await new EditCardDiscussionEntry(dbContext.AsCallContext()).RunAsync(new EditCardDiscussionEntry.Request(otherUserId, entryId, RandomHelper.String())));
    }
    [TestMethod()]
    public async Task UserIsNotAuthorOfEntry()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId);
        var entryId = (await CardDiscussionHelper.CreateEntryAsync(db, userId, cardId)).Id;
        var otherUserId = await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<UserNotAllowedToEditDiscussionEntryException>(async () => await new EditCardDiscussionEntry(dbContext.AsCallContext()).RunAsync(new EditCardDiscussionEntry.Request(otherUserId, entryId, RandomHelper.String())));
    }
    [TestMethod()]
    public async Task EmptyText()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId, userWithViewIds: userId.AsArray());
        var entryId = (await CardDiscussionHelper.CreateEntryAsync(db, userId, cardId)).Id;

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<TextTooShortException>(async () => await new EditCardDiscussionEntry(dbContext.AsCallContext()).RunAsync(new EditCardDiscussionEntry.Request(userId, entryId, "")));
    }
    [TestMethod()]
    public async Task TextStartsWithBlanks()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId, userWithViewIds: userId.AsArray());
        var entryId = (await CardDiscussionHelper.CreateEntryAsync(db, userId, cardId)).Id;

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<TextNotTrimmedException>(async () => await new EditCardDiscussionEntry(dbContext.AsCallContext()).RunAsync(new EditCardDiscussionEntry.Request(userId, entryId, " " + RandomHelper.String())));
        await Assert.ThrowsExceptionAsync<TextNotTrimmedException>(async () => await new EditCardDiscussionEntry(dbContext.AsCallContext()).RunAsync(new EditCardDiscussionEntry.Request(userId, entryId, "\t " + RandomHelper.String())));
    }
    [TestMethod()]
    public async Task TextEndsWithBlanks()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId, userWithViewIds: userId.AsArray());
        var entryId = (await CardDiscussionHelper.CreateEntryAsync(db, userId, cardId)).Id;

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<TextNotTrimmedException>(async () => await new EditCardDiscussionEntry(dbContext.AsCallContext()).RunAsync(new EditCardDiscussionEntry.Request(userId, entryId, RandomHelper.String() + " ")));
        await Assert.ThrowsExceptionAsync<TextNotTrimmedException>(async () => await new EditCardDiscussionEntry(dbContext.AsCallContext()).RunAsync(new EditCardDiscussionEntry.Request(userId, entryId, RandomHelper.String() + "\t ")));
    }
    [TestMethod()]
    public async Task TextTooLong()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId, userWithViewIds: userId.AsArray());
        var entryId = (await CardDiscussionHelper.CreateEntryAsync(db, userId, cardId)).Id;

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<TextTooLongException>(async () => await new EditCardDiscussionEntry(dbContext.AsCallContext()).RunAsync(new EditCardDiscussionEntry.Request(userId, entryId, RandomHelper.String(QueryValidationHelper.CardDiscussionMaxTextLength + 1))));
    }
    [TestMethod()]
    public async Task Success_SingleEditOfSingleEntry_SameUser()
    {
        // We could consider allowing administrators to edit discussion entries, but this is not implemented yet

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
            var discussionEntryFromDb = await dbContext.CardDiscussionEntries
                .Include(entry => entry.Creator)
                .Include(entry => entry.PreviousVersion)
                .SingleAsync();
            Assert.AreEqual(originalEntry.Id, discussionEntryFromDb.Id);
            Assert.AreEqual(cardId, discussionEntryFromDb.Card);
            Assert.AreEqual(userId, discussionEntryFromDb.Creator.Id);
            Assert.AreEqual(editText, discussionEntryFromDb.Text);
            Assert.AreEqual(editDate, discussionEntryFromDb.CreationUtcDate);
            Assert.IsNotNull(discussionEntryFromDb.PreviousVersion);
            Assert.IsNull(discussionEntryFromDb.PreviousEntry);

            var cardFromDb = await dbContext.Cards.SingleAsync();
            Assert.IsNotNull(cardFromDb.LatestDiscussionEntry);
            Assert.AreEqual(discussionEntryFromDb.Id, cardFromDb.LatestDiscussionEntry.Id);
            Assert.AreEqual(editDate, cardFromDb.LatestDiscussionEntry.CreationUtcDate);
            Assert.AreEqual(editText, cardFromDb.LatestDiscussionEntry.Text);

            var previousVersionOfEntry = await dbContext.CardDiscussionEntryPreviousVersions.SingleAsync();
            Assert.IsNull(previousVersionOfEntry.PreviousVersion);
            Assert.AreEqual(cardId, previousVersionOfEntry.Card);
            Assert.AreEqual(userId, previousVersionOfEntry.Creator.Id);
            Assert.AreEqual(originalEntry.Text, previousVersionOfEntry.Text);
            Assert.AreEqual(originalEntry.CreationUtcDate, previousVersionOfEntry.CreationUtcDate);
        }
    }
    [TestMethod()]
    public async Task ComplexCase()
    {
        // In this test:
        // - card1 is visible to user1 and user2
        // - card2 is public

        var db = DbHelper.GetEmptyTestDB();
        var user1Id = await UserHelper.CreateInDbAsync(db);
        var user2Id = await UserHelper.CreateInDbAsync(db);
        var user3Id = await UserHelper.CreateInDbAsync(db);

        var card1Id = await CardHelper.CreateIdAsync(db, user1Id, userWithViewIds: new[] { user1Id, user2Id });
        var card2Id = await CardHelper.CreateIdAsync(db, user2Id);

        var Card1Entry1OriginalVersion = await CardDiscussionHelper.CreateEntryAsync(db, user1Id, card1Id); // Will be edited once
        var Card1Entry2OriginalVersion = await CardDiscussionHelper.CreateEntryAsync(db, user2Id, card1Id); // Will be edited once
        var Card1Entry3OriginalVersion = await CardDiscussionHelper.CreateEntryAsync(db, user1Id, card1Id); // Will not be edited

        var Card2Entry1OriginalVersion = await CardDiscussionHelper.CreateEntryAsync(db, user3Id, card2Id); // Will be edited once
        var Card2Entry2OriginalVersion = await CardDiscussionHelper.CreateEntryAsync(db, user2Id, card2Id); // Will not be edited
        var Card2Entry3OriginalVersion = await CardDiscussionHelper.CreateEntryAsync(db, user3Id, card2Id); // Will be edited twice
        var Card2Entry4OriginalVersion = await CardDiscussionHelper.CreateEntryAsync(db, user1Id, card2Id); // Will not be edited
        var Card2Entry5OriginalVersion = await CardDiscussionHelper.CreateEntryAsync(db, user3Id, card2Id); // Will be edited three times


        var Card1Entry1EditText = RandomHelper.String();
        var Card1Entry1EditDate = RandomHelper.Date();
        using (var dbContext = new MemCheckDbContext(db))
            await new EditCardDiscussionEntry(dbContext.AsCallContext(), Card1Entry1EditDate).RunAsync(new EditCardDiscussionEntry.Request(user1Id, Card1Entry1OriginalVersion.Id, Card1Entry1EditText));

        var Card1Entry2EditText = RandomHelper.String();
        var Card1Entry2EditDate = RandomHelper.Date();
        using (var dbContext = new MemCheckDbContext(db))
            await new EditCardDiscussionEntry(dbContext.AsCallContext(), Card1Entry2EditDate).RunAsync(new EditCardDiscussionEntry.Request(user2Id, Card1Entry2OriginalVersion.Id, Card1Entry2EditText));

        var Card2Entry1EditText = RandomHelper.String();
        var Card2Entry1EditDate = RandomHelper.Date();
        using (var dbContext = new MemCheckDbContext(db))
            await new EditCardDiscussionEntry(dbContext.AsCallContext(), Card2Entry1EditDate).RunAsync(new EditCardDiscussionEntry.Request(user3Id, Card2Entry1OriginalVersion.Id, Card2Entry1EditText));

        var Card2Entry3Edit1Text = RandomHelper.String();
        var Card2Entry3Edit1Date = RandomHelper.Date();
        using (var dbContext = new MemCheckDbContext(db))
            await new EditCardDiscussionEntry(dbContext.AsCallContext(), Card2Entry3Edit1Date).RunAsync(new EditCardDiscussionEntry.Request(user3Id, Card2Entry3OriginalVersion.Id, Card2Entry3Edit1Text));

        var Card2Entry3Edit2Text = RandomHelper.String();
        var Card2Entry3Edit2Date = RandomHelper.Date();
        using (var dbContext = new MemCheckDbContext(db))
            await new EditCardDiscussionEntry(dbContext.AsCallContext(), Card2Entry3Edit2Date).RunAsync(new EditCardDiscussionEntry.Request(user3Id, Card2Entry3OriginalVersion.Id, Card2Entry3Edit2Text));

        var Card2Entry5Edit1Text = RandomHelper.String();
        var Card2Entry5Edit1Date = RandomHelper.Date();
        using (var dbContext = new MemCheckDbContext(db))
            await new EditCardDiscussionEntry(dbContext.AsCallContext(), Card2Entry5Edit1Date).RunAsync(new EditCardDiscussionEntry.Request(user3Id, Card2Entry5OriginalVersion.Id, Card2Entry5Edit1Text));

        var Card2Entry5Edit2Text = RandomHelper.String();
        var Card2Entry5Edit2Date = RandomHelper.Date();
        using (var dbContext = new MemCheckDbContext(db))
            await new EditCardDiscussionEntry(dbContext.AsCallContext(), Card2Entry5Edit2Date).RunAsync(new EditCardDiscussionEntry.Request(user3Id, Card2Entry5OriginalVersion.Id, Card2Entry5Edit2Text));

        var Card2Entry5Edit3Text = RandomHelper.String();
        var Card2Entry5Edit3Date = RandomHelper.Date();
        using (var dbContext = new MemCheckDbContext(db))
            await new EditCardDiscussionEntry(dbContext.AsCallContext(), Card2Entry5Edit3Date).RunAsync(new EditCardDiscussionEntry.Request(user3Id, Card2Entry5OriginalVersion.Id, Card2Entry5Edit3Text));

        // Forbidden edit attempts
        {
            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<UserNotAllowedToAccessCardException>(async () => await new EditCardDiscussionEntry(dbContext.AsCallContext()).RunAsync(new EditCardDiscussionEntry.Request(user3Id, Card1Entry2OriginalVersion.Id, RandomHelper.String())));
            await Assert.ThrowsExceptionAsync<UserNotAllowedToEditDiscussionEntryException>(async () => await new EditCardDiscussionEntry(dbContext.AsCallContext()).RunAsync(new EditCardDiscussionEntry.Request(user2Id, Card1Entry1OriginalVersion.Id, RandomHelper.String())));
            await Assert.ThrowsExceptionAsync<UserNotAllowedToEditDiscussionEntryException>(async () => await new EditCardDiscussionEntry(dbContext.AsCallContext()).RunAsync(new EditCardDiscussionEntry.Request(user1Id, Card1Entry2OriginalVersion.Id, RandomHelper.String())));
        }
        // Check Card1Entry1
        {
            using var dbContext = new MemCheckDbContext(db);
            var discussionEntryFromDb = await dbContext.CardDiscussionEntries
                .Include(entry => entry.Creator)
                .Include(entry => entry.PreviousVersion)
                .Include(entry => entry.PreviousEntry)
                .SingleAsync(entry => entry.Id == Card1Entry1OriginalVersion.Id);
            Assert.AreEqual(Card1Entry1OriginalVersion.Id, discussionEntryFromDb.Id);
            Assert.AreEqual(card1Id, discussionEntryFromDb.Card);
            Assert.AreEqual(user1Id, discussionEntryFromDb.Creator.Id);
            Assert.AreEqual(Card1Entry1EditText, discussionEntryFromDb.Text);
            Assert.AreEqual(Card1Entry1EditDate, discussionEntryFromDb.CreationUtcDate);
            Assert.IsNotNull(discussionEntryFromDb.PreviousVersion);
            Assert.IsNull(discussionEntryFromDb.PreviousEntry);

            Assert.AreEqual(Card1Entry1OriginalVersion.CreationUtcDate, discussionEntryFromDb.PreviousVersion.CreationUtcDate);
            Assert.AreEqual(Card1Entry1OriginalVersion.Text, discussionEntryFromDb.PreviousVersion.Text);
            Assert.AreNotEqual(Card1Entry1OriginalVersion.Id, discussionEntryFromDb.PreviousVersion.Id);
            Assert.AreEqual(card1Id, discussionEntryFromDb.PreviousVersion.Card);
            Assert.AreEqual(user1Id, discussionEntryFromDb.PreviousVersion.Creator.Id);
            Assert.IsNull(discussionEntryFromDb.PreviousVersion.PreviousVersion);

            var card = await dbContext.Cards.Include(card => card.LatestDiscussionEntry).SingleAsync(card => card.Id == discussionEntryFromDb.Card);
            Assert.IsNotNull(card);
            Assert.IsNotNull(card.LatestDiscussionEntry);
            Assert.AreEqual(Card1Entry3OriginalVersion.Id, card.LatestDiscussionEntry!.Id);
        }
        // Check Card1Entry2
        {
            using var dbContext = new MemCheckDbContext(db);
            var discussionEntryFromDb = await dbContext.CardDiscussionEntries
                .Include(entry => entry.Creator)
                .Include(entry => entry.PreviousVersion)
                .Include(entry => entry.PreviousEntry)
                .SingleAsync(entry => entry.Id == Card1Entry2OriginalVersion.Id);
            Assert.AreEqual(Card1Entry2OriginalVersion.Id, discussionEntryFromDb.Id);
            Assert.AreEqual(card1Id, discussionEntryFromDb.Card);
            Assert.AreEqual(user2Id, discussionEntryFromDb.Creator.Id);
            Assert.AreEqual(Card1Entry2EditText, discussionEntryFromDb.Text);
            Assert.AreEqual(Card1Entry2EditDate, discussionEntryFromDb.CreationUtcDate);
            Assert.IsNotNull(discussionEntryFromDb.PreviousVersion);
            Assert.AreEqual(Card1Entry1OriginalVersion.Id, discussionEntryFromDb.PreviousEntry!.Id);

            Assert.AreEqual(Card1Entry2OriginalVersion.CreationUtcDate, discussionEntryFromDb.PreviousVersion.CreationUtcDate);
            Assert.AreEqual(Card1Entry2OriginalVersion.Text, discussionEntryFromDb.PreviousVersion.Text);
            Assert.AreNotEqual(Card1Entry2OriginalVersion.Id, discussionEntryFromDb.PreviousVersion.Id);
            Assert.AreEqual(card1Id, discussionEntryFromDb.PreviousVersion.Card);
            Assert.AreEqual(user2Id, discussionEntryFromDb.PreviousVersion.Creator.Id);
            Assert.IsNull(discussionEntryFromDb.PreviousVersion.PreviousVersion);

            var card = await dbContext.Cards.Include(card => card.LatestDiscussionEntry).SingleAsync(card => card.Id == discussionEntryFromDb.Card);
            Assert.IsNotNull(card);
            Assert.IsNotNull(card.LatestDiscussionEntry);
            Assert.AreEqual(Card1Entry3OriginalVersion.Id, card.LatestDiscussionEntry!.Id);
        }
        // Check Card1Entry3
        {
            using var dbContext = new MemCheckDbContext(db);
            var discussionEntryFromDb = await dbContext.CardDiscussionEntries
                .Include(entry => entry.Creator)
                .Include(entry => entry.PreviousVersion)
                .Include(entry => entry.PreviousEntry)
                .SingleAsync(entry => entry.Id == Card1Entry3OriginalVersion.Id);
            Assert.AreEqual(Card1Entry3OriginalVersion.Id, discussionEntryFromDb.Id);
            Assert.AreEqual(card1Id, discussionEntryFromDb.Card);
            Assert.AreEqual(user1Id, discussionEntryFromDb.Creator.Id);
            Assert.AreEqual(Card1Entry3OriginalVersion.Text, discussionEntryFromDb.Text);
            Assert.AreEqual(Card1Entry3OriginalVersion.CreationUtcDate, discussionEntryFromDb.CreationUtcDate);
            Assert.IsNull(discussionEntryFromDb.PreviousVersion);
            Assert.AreEqual(Card1Entry2OriginalVersion.Id, discussionEntryFromDb.PreviousEntry!.Id);


            var card = await dbContext.Cards.Include(card => card.LatestDiscussionEntry).SingleAsync(card => card.Id == discussionEntryFromDb.Card);
            Assert.IsNotNull(card);
            Assert.IsNotNull(card.LatestDiscussionEntry);
            Assert.AreEqual(Card1Entry3OriginalVersion.Id, card.LatestDiscussionEntry!.Id);
        }
        // Check Card2Entry1
        {
            using var dbContext = new MemCheckDbContext(db);
            var discussionEntryFromDb = await dbContext.CardDiscussionEntries
                .Include(entry => entry.Creator)
                .Include(entry => entry.PreviousVersion)
                .Include(entry => entry.PreviousEntry)
                .SingleAsync(entry => entry.Id == Card2Entry1OriginalVersion.Id);
            Assert.AreEqual(Card2Entry1OriginalVersion.Id, discussionEntryFromDb.Id);
            Assert.AreEqual(card2Id, discussionEntryFromDb.Card);
            Assert.AreEqual(user3Id, discussionEntryFromDb.Creator.Id);
            Assert.AreEqual(Card2Entry1EditText, discussionEntryFromDb.Text);
            Assert.AreEqual(Card2Entry1EditDate, discussionEntryFromDb.CreationUtcDate);
            Assert.IsNotNull(discussionEntryFromDb.PreviousVersion);
            Assert.IsNull(discussionEntryFromDb.PreviousEntry);

            Assert.AreEqual(Card2Entry1OriginalVersion.CreationUtcDate, discussionEntryFromDb.PreviousVersion.CreationUtcDate);
            Assert.AreEqual(Card2Entry1OriginalVersion.Text, discussionEntryFromDb.PreviousVersion.Text);
            Assert.AreNotEqual(Card1Entry1OriginalVersion.Id, discussionEntryFromDb.PreviousVersion.Id);
            Assert.AreEqual(card2Id, discussionEntryFromDb.PreviousVersion.Card);
            Assert.AreEqual(user3Id, discussionEntryFromDb.PreviousVersion.Creator.Id);
            Assert.IsNull(discussionEntryFromDb.PreviousVersion.PreviousVersion);

            var card = await dbContext.Cards.Include(card => card.LatestDiscussionEntry).SingleAsync(card => card.Id == discussionEntryFromDb.Card);
            Assert.IsNotNull(card);
            Assert.IsNotNull(card.LatestDiscussionEntry);
            Assert.AreEqual(Card2Entry5OriginalVersion.Id, card.LatestDiscussionEntry!.Id);
        }
        // Check Card2Entry2
        {
            using var dbContext = new MemCheckDbContext(db);
            var discussionEntryFromDb = await dbContext.CardDiscussionEntries
                .Include(entry => entry.Creator)
                .Include(entry => entry.PreviousVersion)
                .Include(entry => entry.PreviousEntry)
                .SingleAsync(entry => entry.Id == Card2Entry2OriginalVersion.Id);
            Assert.AreEqual(Card2Entry2OriginalVersion.Id, discussionEntryFromDb.Id);
            Assert.AreEqual(card2Id, discussionEntryFromDb.Card);
            Assert.AreEqual(user2Id, discussionEntryFromDb.Creator.Id);
            Assert.AreEqual(Card2Entry2OriginalVersion.Text, discussionEntryFromDb.Text);
            Assert.AreEqual(Card2Entry2OriginalVersion.CreationUtcDate, discussionEntryFromDb.CreationUtcDate);
            Assert.IsNull(discussionEntryFromDb.PreviousVersion);
            Assert.AreEqual(Card2Entry1OriginalVersion.Id, discussionEntryFromDb.PreviousEntry!.Id);

            var card = await dbContext.Cards.Include(card => card.LatestDiscussionEntry).SingleAsync(card => card.Id == discussionEntryFromDb.Card);
            Assert.IsNotNull(card);
            Assert.IsNotNull(card.LatestDiscussionEntry);
            Assert.AreEqual(Card2Entry5OriginalVersion.Id, card.LatestDiscussionEntry!.Id);
        }
        // Check Card2Entry3
        {
            using var dbContext = new MemCheckDbContext(db);
            var discussionEntryFromDb = await dbContext.CardDiscussionEntries
                .Include(entry => entry.Creator)
                .Include(entry => entry.PreviousVersion)
                .Include(entry => entry.PreviousEntry)
                .SingleAsync(entry => entry.Id == Card2Entry3OriginalVersion.Id);
            Assert.AreEqual(Card2Entry3OriginalVersion.Id, discussionEntryFromDb.Id);
            Assert.AreEqual(card2Id, discussionEntryFromDb.Card);
            Assert.AreEqual(user3Id, discussionEntryFromDb.Creator.Id);
            Assert.AreEqual(Card2Entry3Edit2Text, discussionEntryFromDb.Text);
            Assert.AreEqual(Card2Entry3Edit2Date, discussionEntryFromDb.CreationUtcDate);
            Assert.IsNotNull(discussionEntryFromDb.PreviousVersion);
            Assert.AreEqual(Card2Entry2OriginalVersion.Id, discussionEntryFromDb.PreviousEntry!.Id);

            var previousVersionFromDb = await dbContext.CardDiscussionEntryPreviousVersions
                .Include(previousVersion => previousVersion.Creator)
                .Include(previousVersion => previousVersion.PreviousVersion)
                .SingleAsync(previousVersion => previousVersion.Id == discussionEntryFromDb.PreviousVersion.Id);
            Assert.AreEqual(Card2Entry3Edit1Date, previousVersionFromDb.CreationUtcDate);
            Assert.AreEqual(Card2Entry3Edit1Text, previousVersionFromDb.Text);
            Assert.AreEqual(card2Id, previousVersionFromDb.Card);
            Assert.AreEqual(user3Id, previousVersionFromDb.Creator.Id);
            Assert.IsNotNull(previousVersionFromDb.PreviousVersion);

            var originalVersionFromDb = await dbContext.CardDiscussionEntryPreviousVersions
                .Include(previousVersion => previousVersion.Creator)
                .Include(previousVersion => previousVersion.PreviousVersion)
                .SingleAsync(previousVersion => previousVersion.Id == previousVersionFromDb.PreviousVersion.Id);
            Assert.AreEqual(Card2Entry3OriginalVersion.CreationUtcDate, originalVersionFromDb.CreationUtcDate);
            Assert.AreEqual(Card2Entry3OriginalVersion.Text, originalVersionFromDb.Text);
            Assert.AreEqual(card2Id, originalVersionFromDb.Card);
            Assert.AreEqual(user3Id, originalVersionFromDb.Creator.Id);
            Assert.IsNull(originalVersionFromDb.PreviousVersion);

            var card = await dbContext.Cards.Include(card => card.LatestDiscussionEntry).SingleAsync(card => card.Id == discussionEntryFromDb.Card);
            Assert.IsNotNull(card);
            Assert.IsNotNull(card.LatestDiscussionEntry);
            Assert.AreEqual(Card2Entry5OriginalVersion.Id, card.LatestDiscussionEntry!.Id);
        }
        // Check Card2Entry4
        {
            using var dbContext = new MemCheckDbContext(db);
            var discussionEntryFromDb = await dbContext.CardDiscussionEntries
                .Include(entry => entry.Creator)
                .Include(entry => entry.PreviousVersion)
                .Include(entry => entry.PreviousEntry)
                .SingleAsync(entry => entry.Id == Card2Entry4OriginalVersion.Id);
            Assert.AreEqual(Card2Entry4OriginalVersion.Id, discussionEntryFromDb.Id);
            Assert.AreEqual(card2Id, discussionEntryFromDb.Card);
            Assert.AreEqual(user1Id, discussionEntryFromDb.Creator.Id);
            Assert.AreEqual(Card2Entry4OriginalVersion.Text, discussionEntryFromDb.Text);
            Assert.AreEqual(Card2Entry4OriginalVersion.CreationUtcDate, discussionEntryFromDb.CreationUtcDate);
            Assert.IsNull(discussionEntryFromDb.PreviousVersion);
            Assert.AreEqual(Card2Entry3OriginalVersion.Id, discussionEntryFromDb.PreviousEntry!.Id);

            var card = await dbContext.Cards.Include(card => card.LatestDiscussionEntry).SingleAsync(card => card.Id == discussionEntryFromDb.Card);
            Assert.IsNotNull(card);
            Assert.IsNotNull(card.LatestDiscussionEntry);
            Assert.AreEqual(Card2Entry5OriginalVersion.Id, card.LatestDiscussionEntry!.Id);
        }
        // Check Card2Entry5
        {
            using var dbContext = new MemCheckDbContext(db);
            var currentDiscussionEntryFromDb = await dbContext.CardDiscussionEntries
                .Include(entry => entry.Creator)
                .Include(entry => entry.PreviousVersion)
                .Include(entry => entry.PreviousEntry)
                .SingleAsync(entry => entry.Id == Card2Entry5OriginalVersion.Id);
            Assert.AreEqual(Card2Entry5OriginalVersion.Id, currentDiscussionEntryFromDb.Id);
            Assert.AreEqual(card2Id, currentDiscussionEntryFromDb.Card);
            Assert.AreEqual(user3Id, currentDiscussionEntryFromDb.Creator.Id);
            Assert.AreEqual(Card2Entry5Edit3Text, currentDiscussionEntryFromDb.Text);
            Assert.AreEqual(Card2Entry5Edit3Date, currentDiscussionEntryFromDb.CreationUtcDate);
            Assert.IsNotNull(currentDiscussionEntryFromDb.PreviousVersion);
            Assert.AreEqual(Card2Entry4OriginalVersion.Id, currentDiscussionEntryFromDb.PreviousEntry!.Id);

            var edit2VersionFromDb = await dbContext.CardDiscussionEntryPreviousVersions
                .Include(previousVersion => previousVersion.Creator)
                .Include(previousVersion => previousVersion.PreviousVersion)
                .SingleAsync(previousVersion => previousVersion.Id == currentDiscussionEntryFromDb.PreviousVersion.Id);
            Assert.AreEqual(Card2Entry5Edit2Date, edit2VersionFromDb.CreationUtcDate);
            Assert.AreEqual(Card2Entry5Edit2Text, edit2VersionFromDb.Text);
            Assert.AreEqual(card2Id, edit2VersionFromDb.Card);
            Assert.AreEqual(user3Id, edit2VersionFromDb.Creator.Id);
            Assert.IsNotNull(edit2VersionFromDb.PreviousVersion);

            var edit1VersionFromDb = await dbContext.CardDiscussionEntryPreviousVersions
                .Include(previousVersion => previousVersion.Creator)
                .Include(previousVersion => previousVersion.PreviousVersion)
                .SingleAsync(previousVersion => previousVersion.Id == edit2VersionFromDb.PreviousVersion.Id);
            Assert.AreEqual(Card2Entry5Edit1Date, edit1VersionFromDb.CreationUtcDate);
            Assert.AreEqual(Card2Entry5Edit1Text, edit1VersionFromDb.Text);
            Assert.AreEqual(card2Id, edit1VersionFromDb.Card);
            Assert.AreEqual(user3Id, edit1VersionFromDb.Creator.Id);
            Assert.IsNotNull(edit1VersionFromDb.PreviousVersion);

            var originalVersionFromDb = await dbContext.CardDiscussionEntryPreviousVersions
                .Include(previousVersion => previousVersion.Creator)
                .Include(previousVersion => previousVersion.PreviousVersion)
                .SingleAsync(previousVersion => previousVersion.Id == edit1VersionFromDb.PreviousVersion.Id);
            Assert.AreEqual(Card2Entry5OriginalVersion.CreationUtcDate, originalVersionFromDb.CreationUtcDate);
            Assert.AreEqual(Card2Entry5OriginalVersion.Text, originalVersionFromDb.Text);
            Assert.AreEqual(card2Id, edit1VersionFromDb.Card);
            Assert.AreEqual(user3Id, edit1VersionFromDb.Creator.Id);
            Assert.IsNull(originalVersionFromDb.PreviousVersion);

            var card = await dbContext.Cards.Include(card => card.LatestDiscussionEntry).SingleAsync(card => card.Id == currentDiscussionEntryFromDb.Card);
            Assert.IsNotNull(card);
            Assert.IsNotNull(card.LatestDiscussionEntry);
            Assert.AreEqual(Card2Entry5OriginalVersion.Id, card.LatestDiscussionEntry!.Id);
        }
    }
    //[TestMethod()]
    //public async Task TextSuccess_SecondEntryForCard()
    //{
    //    var db = DbHelper.GetEmptyTestDB();
    //    var userId = await UserHelper.CreateInDbAsync(db);
    //    var cardId = await CardHelper.CreateIdAsync(db, userId);
    //    var oldestEntryText = RandomHelper.String();
    //    var oldestEntryRunDate = RandomHelper.Date();
    //    var newestEntryText = RandomHelper.String();
    //    var newestEntryRunDate = RandomHelper.Date(oldestEntryRunDate);

    //    Guid oldestEntryId;
    //    using (var dbContext = new MemCheckDbContext(db))
    //    {
    //        var result = await new AddEntryToCardDiscussion(dbContext.AsCallContext(), oldestEntryRunDate).RunAsync(new AddEntryToCardDiscussion.Request(userId, cardId, oldestEntryText));
    //        oldestEntryId = result.EntryId;
    //    }

    //    Guid newestEntryId;
    //    using (var dbContext = new MemCheckDbContext(db))
    //    {
    //        var result = await new AddEntryToCardDiscussion(dbContext.AsCallContext(), newestEntryRunDate).RunAsync(new AddEntryToCardDiscussion.Request(userId, cardId, newestEntryText));
    //        newestEntryId = result.EntryId;
    //    }

    //    using (var dbContext = new MemCheckDbContext(db))
    //    {
    //        var cardFromDb = await dbContext.Cards.Include(card => card.LatestDiscussionEntry).SingleAsync();
    //        Assert.IsNotNull(cardFromDb.LatestDiscussionEntry);

    //        Assert.AreEqual(2, dbContext.CardDiscussionEntries.Count());

    //        var newestDiscussionEntryFromDb = await dbContext.CardDiscussionEntries.Include(entry => entry.Creator).Include(entry => entry.PreviousEntry).Where(entry => entry.Id == cardFromDb.LatestDiscussionEntry.Id).SingleAsync();
    //        {
    //            Assert.AreEqual(newestEntryId, newestDiscussionEntryFromDb.Id);
    //            Assert.AreEqual(cardId, newestDiscussionEntryFromDb.Card);
    //            Assert.AreEqual(userId, newestDiscussionEntryFromDb.Creator.Id);
    //            Assert.AreEqual(newestEntryText, newestDiscussionEntryFromDb.Text);
    //            Assert.AreEqual(newestEntryRunDate, newestDiscussionEntryFromDb.CreationUtcDate);
    //            Assert.IsNull(newestDiscussionEntryFromDb.PreviousVersion);
    //            Assert.IsNotNull(newestDiscussionEntryFromDb.PreviousEntry);
    //        }
    //        {
    //            var oldestDiscussionEntryFromDb = await dbContext.CardDiscussionEntries.Include(entry => entry.Creator).Where(entry => entry.Id == newestDiscussionEntryFromDb.PreviousEntry.Id).SingleAsync();
    //            Assert.AreEqual(oldestEntryId, oldestDiscussionEntryFromDb.Id);
    //            Assert.AreEqual(cardId, oldestDiscussionEntryFromDb.Card);
    //            Assert.AreEqual(userId, oldestDiscussionEntryFromDb.Creator.Id);
    //            Assert.AreEqual(oldestEntryText, oldestDiscussionEntryFromDb.Text);
    //            Assert.AreEqual(oldestEntryRunDate, oldestDiscussionEntryFromDb.CreationUtcDate);
    //            Assert.IsNull(oldestDiscussionEntryFromDb.PreviousVersion);
    //            Assert.IsNull(oldestDiscussionEntryFromDb.PreviousEntry);
    //        }
    //    }
    //}
    //[TestMethod()]
    //public async Task TextSuccess_ThirdEntryForCard_TwoUsers()
    //{
    //    var db = DbHelper.GetEmptyTestDB();
    //    var user1Id = await UserHelper.CreateInDbAsync(db);
    //    var user2Id = await UserHelper.CreateInDbAsync(db);
    //    var cardId = await CardHelper.CreateIdAsync(db, user1Id);
    //    var oldestEntryText = RandomHelper.String();
    //    var oldestEntryRunDate = RandomHelper.Date();
    //    var intermediaryEntryText = RandomHelper.String();
    //    var intermediaryEntryRunDate = RandomHelper.Date(oldestEntryRunDate);
    //    var newestEntryText = RandomHelper.String();
    //    var newestEntryRunDate = RandomHelper.Date(intermediaryEntryRunDate);

    //    Guid oldestEntryId;
    //    using (var dbContext = new MemCheckDbContext(db))
    //    {
    //        var result = await new AddEntryToCardDiscussion(dbContext.AsCallContext(), oldestEntryRunDate).RunAsync(new AddEntryToCardDiscussion.Request(user1Id, cardId, oldestEntryText));
    //        oldestEntryId = result.EntryId;
    //    }

    //    Guid intermediaryEntryId;
    //    using (var dbContext = new MemCheckDbContext(db))
    //    {
    //        var result = await new AddEntryToCardDiscussion(dbContext.AsCallContext(), intermediaryEntryRunDate).RunAsync(new AddEntryToCardDiscussion.Request(user2Id, cardId, intermediaryEntryText));
    //        intermediaryEntryId = result.EntryId;
    //    }

    //    Guid newestEntryId;
    //    using (var dbContext = new MemCheckDbContext(db))
    //    {
    //        var result = await new AddEntryToCardDiscussion(dbContext.AsCallContext(), newestEntryRunDate).RunAsync(new AddEntryToCardDiscussion.Request(user1Id, cardId, newestEntryText));
    //        newestEntryId = result.EntryId;
    //    }

    //    using (var dbContext = new MemCheckDbContext(db))
    //    {
    //        var cardFromDb = await dbContext.Cards.Include(card => card.LatestDiscussionEntry).SingleAsync();
    //        Assert.IsNotNull(cardFromDb.LatestDiscussionEntry);
    //        Assert.AreEqual(newestEntryId, cardFromDb.LatestDiscussionEntry.Id);

    //        Assert.AreEqual(3, dbContext.CardDiscussionEntries.Count());

    //        var newestDiscussionEntryFromDb = await dbContext.CardDiscussionEntries.Include(entry => entry.Creator).Include(entry => entry.PreviousEntry).Where(entry => entry.Id == cardFromDb.LatestDiscussionEntry.Id).SingleAsync();
    //        {
    //            Assert.AreEqual(newestEntryId, newestDiscussionEntryFromDb.Id);
    //            Assert.AreEqual(cardId, newestDiscussionEntryFromDb.Card);
    //            Assert.AreEqual(user1Id, newestDiscussionEntryFromDb.Creator.Id);
    //            Assert.AreEqual(newestEntryText, newestDiscussionEntryFromDb.Text);
    //            Assert.AreEqual(newestEntryRunDate, newestDiscussionEntryFromDb.CreationUtcDate);
    //            Assert.IsNull(newestDiscussionEntryFromDb.PreviousVersion);
    //            Assert.IsNotNull(newestDiscussionEntryFromDb.PreviousEntry);
    //        }
    //        var intermediaryDiscussionEntryFromDb = await dbContext.CardDiscussionEntries.Include(entry => entry.Creator).Include(entry => entry.PreviousEntry).Where(entry => entry.Id == newestDiscussionEntryFromDb.PreviousEntry.Id).SingleAsync();
    //        {
    //            Assert.AreEqual(intermediaryEntryId, intermediaryDiscussionEntryFromDb.Id);
    //            Assert.AreEqual(cardId, intermediaryDiscussionEntryFromDb.Card);
    //            Assert.AreEqual(user2Id, intermediaryDiscussionEntryFromDb.Creator.Id);
    //            Assert.AreEqual(intermediaryEntryText, intermediaryDiscussionEntryFromDb.Text);
    //            Assert.AreEqual(intermediaryEntryRunDate, intermediaryDiscussionEntryFromDb.CreationUtcDate);
    //            Assert.IsNull(intermediaryDiscussionEntryFromDb.PreviousVersion);
    //            Assert.IsNotNull(intermediaryDiscussionEntryFromDb.PreviousEntry);
    //        }
    //        {
    //            var oldestDiscussionEntryFromDb = await dbContext.CardDiscussionEntries.Include(entry => entry.Creator).Where(entry => entry.Id == intermediaryDiscussionEntryFromDb.PreviousEntry.Id).SingleAsync();
    //            Assert.AreEqual(oldestEntryId, oldestDiscussionEntryFromDb.Id);
    //            Assert.AreEqual(cardId, oldestDiscussionEntryFromDb.Card);
    //            Assert.AreEqual(user1Id, oldestDiscussionEntryFromDb.Creator.Id);
    //            Assert.AreEqual(oldestEntryText, oldestDiscussionEntryFromDb.Text);
    //            Assert.AreEqual(oldestEntryRunDate, oldestDiscussionEntryFromDb.CreationUtcDate);
    //            Assert.IsNull(oldestDiscussionEntryFromDb.PreviousVersion);
    //            Assert.IsNull(oldestDiscussionEntryFromDb.PreviousEntry);
    //        }
    //    }
    //}
}

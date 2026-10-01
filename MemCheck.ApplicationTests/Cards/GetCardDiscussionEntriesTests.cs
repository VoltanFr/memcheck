using MemCheck.Application.Helpers;
using MemCheck.Application.QueryValidation;
using MemCheck.Basics;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards;

[TestClass()]
public class GetCardDiscussionEntriesTests
{
    [TestMethod()]
    public async Task UserNotLoggedIn()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExactlyAsync<NonexistentUserException>(async () => await new GetCardDiscussionEntries(dbContext.AsCallContext()).RunAsync(new GetCardDiscussionEntries.Request(Guid.Empty, cardId, 42, Guid.Empty)));
    }
    [TestMethod()]
    public async Task UserDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExactlyAsync<NonexistentUserException>(async () => await new GetCardDiscussionEntries(dbContext.AsCallContext()).RunAsync(new GetCardDiscussionEntries.Request(RandomHelper.Guid(), cardId, 42, Guid.Empty)));
    }
    [TestMethod()]
    public async Task CardDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExactlyAsync<NonexistentCardException>(async () => await new GetCardDiscussionEntries(dbContext.AsCallContext()).RunAsync(new GetCardDiscussionEntries.Request(userId, RandomHelper.Guid(), 42, Guid.Empty)));
    }
    [TestMethod()]
    public async Task CardIsDeleted()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId, userWithViewIds: userId.AsArray());

        await CardDeletionHelper.DeleteCardAsync(db, userId, cardId);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExactlyAsync<NonexistentCardException>(async () => await new GetCardDiscussionEntries(dbContext.AsCallContext()).RunAsync(new GetCardDiscussionEntries.Request(userId, cardId, 42, Guid.Empty)));
    }
    [TestMethod()]
    public async Task CardIsNotViewableByUser()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId, userWithViewIds: userId.AsArray());
        var otherUserId = await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExactlyAsync<UserNotAllowedToAccessCardException>(async () => await new GetCardDiscussionEntries(dbContext.AsCallContext()).RunAsync(new GetCardDiscussionEntries.Request(otherUserId, cardId, 42, Guid.Empty)));
    }
    [TestMethod()]
    public async Task PageSizeTooSmall()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId, userWithViewIds: userId.AsArray());

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExactlyAsync<PageSizeTooSmallException>(async () => await new GetCardDiscussionEntries(dbContext.AsCallContext()).RunAsync(new GetCardDiscussionEntries.Request(userId, cardId, GetCardDiscussionEntries.Request.MinPageSize - 1, Guid.Empty)));
    }
    [TestMethod()]
    public async Task PageSizeTooBig()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId, userWithViewIds: userId.AsArray());

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExactlyAsync<PageSizeTooBigException>(async () => await new GetCardDiscussionEntries(dbContext.AsCallContext()).RunAsync(new GetCardDiscussionEntries.Request(userId, cardId, GetCardDiscussionEntries.Request.MaxPageSize + 1, Guid.Empty)));
    }
    [TestMethod()]
    public async Task Success_CardHasNoDiscussionEntry_LastObtainedEntryIsZero()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId, userWithViewIds: userId.AsArray());

        using var dbContext = new MemCheckDbContext(db);
        var result = await new GetCardDiscussionEntries(dbContext.AsCallContext()).RunAsync(new GetCardDiscussionEntries.Request(userId, cardId, 2, Guid.Empty));

        Assert.AreEqual(0, result.TotalCount);
        Assert.AreEqual(0, result.PageCount);
        Assert.AreEqual(0, result.Entries.Length);
    }
    [TestMethod()]
    public async Task Success_CardHasNoDiscussionEntry_LastObtainedEntryIsRandom()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId, userWithViewIds: userId.AsArray());

        using var dbContext = new MemCheckDbContext(db);
        var result = await new GetCardDiscussionEntries(dbContext.AsCallContext()).RunAsync(new GetCardDiscussionEntries.Request(userId, cardId, 2, RandomHelper.Guid()));

        Assert.AreEqual(0, result.TotalCount);
        Assert.AreEqual(0, result.PageCount);
        Assert.IsEmpty(result.Entries);
    }
    [TestMethod()]
    public async Task Success_CardHasSingleDiscussionEntryBySameUser_LastObtainedEntryIsZero()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId, userWithViewIds: userId.AsArray());
        var text = RandomHelper.String();
        var runDate = RandomHelper.Date();

        using (var dbContext = new MemCheckDbContext(db))
            await new AddEntryToCardDiscussion(dbContext.AsCallContext(), runDate).RunAsync(new AddEntryToCardDiscussion.Request(userId, cardId, text));

        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new GetCardDiscussionEntries(dbContext.AsCallContext()).RunAsync(new GetCardDiscussionEntries.Request(userId, cardId, 2, Guid.Empty));

            Assert.AreEqual(1, result.TotalCount);
            Assert.AreEqual(1, result.PageCount);
            Assert.HasCount(1, result.Entries);
            Assert.AreEqual(text, result.Entries.Single().Text);
            Assert.IsFalse(result.Entries.Single().HasBeenEdited);
            Assert.AreEqual(userId, result.Entries.Single().Creator.Id);
            Assert.IsTrue(result.Entries.Single().CanBeEditedByCurrentUser);
        }
    }
    [TestMethod()]
    public async Task Success_CardHasSingleDiscussionEntryByOtherUser_LastObtainedEntryIsZero()
    {
        var db = DbHelper.GetEmptyTestDB();
        var authorUserId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, authorUserId);
        var text = RandomHelper.String();
        var runDate = RandomHelper.Date();

        using (var dbContext = new MemCheckDbContext(db))
            await new AddEntryToCardDiscussion(dbContext.AsCallContext(), runDate).RunAsync(new AddEntryToCardDiscussion.Request(authorUserId, cardId, text));

        var readUserId = await UserHelper.CreateInDbAsync(db);

        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new GetCardDiscussionEntries(dbContext.AsCallContext()).RunAsync(new GetCardDiscussionEntries.Request(readUserId, cardId, 2, Guid.Empty));

            Assert.AreEqual(1, result.TotalCount);
            Assert.AreEqual(1, result.PageCount);
            Assert.HasCount(1, result.Entries);
            Assert.AreEqual(text, result.Entries.Single().Text);
            Assert.IsFalse(result.Entries.Single().HasBeenEdited);
            Assert.AreEqual(authorUserId, result.Entries.Single().Creator.Id);
            Assert.IsFalse(result.Entries.Single().CanBeEditedByCurrentUser);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new GetCardDiscussionEntries(dbContext.AsCallContext()).RunAsync(new GetCardDiscussionEntries.Request(authorUserId, cardId, 2, Guid.Empty));

            Assert.AreEqual(1, result.TotalCount);
            Assert.AreEqual(1, result.PageCount);
            Assert.HasCount(1, result.Entries);
            Assert.AreEqual(text, result.Entries.Single().Text);
            Assert.IsFalse(result.Entries.Single().HasBeenEdited);
            Assert.AreEqual(authorUserId, result.Entries.Single().Creator.Id);
            Assert.IsTrue(result.Entries.Single().CanBeEditedByCurrentUser);
        }
    }
    [TestMethod()]
    public async Task Success_CardHasSingleDiscussionEntry_LastObtainedEntryIsRandom()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId, userWithViewIds: userId.AsArray());
        var text = RandomHelper.String();
        var runDate = RandomHelper.Date();

        using (var dbContext = new MemCheckDbContext(db))
            await new AddEntryToCardDiscussion(dbContext.AsCallContext(), runDate).RunAsync(new AddEntryToCardDiscussion.Request(userId, cardId, text));

        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new GetCardDiscussionEntries(dbContext.AsCallContext()).RunAsync(new GetCardDiscussionEntries.Request(userId, cardId, 2, RandomHelper.Guid()));

            Assert.AreEqual(1, result.TotalCount);
            Assert.AreEqual(1, result.PageCount);
            Assert.AreEqual(1, result.Entries.Length);
            Assert.AreEqual(text, result.Entries.Single().Text);
            Assert.IsFalse(result.Entries.Single().HasBeenEdited);
            Assert.AreEqual(userId, result.Entries.Single().Creator.Id);
        }
    }
    [TestMethod()]
    public async Task Success_TwoCards_SingleUser_MultipleEntries()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);

        var card1Id = await CardHelper.CreateIdAsync(db, userId);
        var card1Text1 = RandomHelper.String();
        var card1RunDate1 = RandomHelper.Date();
        var card1Text2 = RandomHelper.String();
        var card1RunDate2 = RandomHelper.Date(card1RunDate1);
        var card1Text3 = RandomHelper.String();
        var card1RunDate3 = RandomHelper.Date(card1RunDate2);
        var card1Text4 = RandomHelper.String();
        var card1RunDate4 = RandomHelper.Date(card1RunDate3);
        var card1Text5 = RandomHelper.String();
        var card1RunDate5 = RandomHelper.Date(card1RunDate4);

        var card2Id = await CardHelper.CreateIdAsync(db, userId);
        var card2Text1 = RandomHelper.String();
        var card2RunDate1 = RandomHelper.Date();
        var card2Text2 = RandomHelper.String();
        var card2RunDate2 = RandomHelper.Date(card2RunDate1);

        using (var dbContext = new MemCheckDbContext(db))
        {
            await new AddEntryToCardDiscussion(dbContext.AsCallContext(), card1RunDate1).RunAsync(new AddEntryToCardDiscussion.Request(userId, card1Id, card1Text1));
            await new AddEntryToCardDiscussion(dbContext.AsCallContext(), card1RunDate2).RunAsync(new AddEntryToCardDiscussion.Request(userId, card1Id, card1Text2));
            await new AddEntryToCardDiscussion(dbContext.AsCallContext(), card1RunDate3).RunAsync(new AddEntryToCardDiscussion.Request(userId, card1Id, card1Text3));
            await new AddEntryToCardDiscussion(dbContext.AsCallContext(), card1RunDate4).RunAsync(new AddEntryToCardDiscussion.Request(userId, card1Id, card1Text4));
            await new AddEntryToCardDiscussion(dbContext.AsCallContext(), card1RunDate5).RunAsync(new AddEntryToCardDiscussion.Request(userId, card1Id, card1Text5));

            await new AddEntryToCardDiscussion(dbContext.AsCallContext(), card2RunDate1).RunAsync(new AddEntryToCardDiscussion.Request(userId, card2Id, card2Text1));
            await new AddEntryToCardDiscussion(dbContext.AsCallContext(), card2RunDate2).RunAsync(new AddEntryToCardDiscussion.Request(userId, card2Id, card2Text2));
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            { // Card 1
                Guid page1LastObtaintedEntry;
                { // Page 1
                    var result = await new GetCardDiscussionEntries(dbContext.AsCallContext()).RunAsync(new GetCardDiscussionEntries.Request(userId, card1Id, 2, Guid.Empty));

                    Assert.AreEqual(5, result.TotalCount);
                    Assert.AreEqual(3, result.PageCount);
                    Assert.AreEqual(2, result.Entries.Length);

                    Assert.AreEqual(card1Text5, result.Entries[0].Text);
                    Assert.AreEqual(card1RunDate5, result.Entries[0].CreationUtcDate);
                    Assert.IsFalse(result.Entries[0].HasBeenEdited);
                    Assert.AreEqual(userId, result.Entries[0].Creator.Id);

                    Assert.AreEqual(card1Text4, result.Entries[1].Text);
                    Assert.AreEqual(card1RunDate4, result.Entries[1].CreationUtcDate);
                    Assert.IsFalse(result.Entries[1].HasBeenEdited);
                    Assert.AreEqual(userId, result.Entries[1].Creator.Id);

                    page1LastObtaintedEntry = result.Entries[1].Id;
                }
                Guid page2LastObtaintedEntry;
                { // Page 2
                    var result = await new GetCardDiscussionEntries(dbContext.AsCallContext()).RunAsync(new GetCardDiscussionEntries.Request(userId, card1Id, 2, page1LastObtaintedEntry));

                    Assert.AreEqual(5, result.TotalCount);
                    Assert.AreEqual(3, result.PageCount);
                    Assert.AreEqual(2, result.Entries.Length);

                    Assert.AreEqual(card1Text3, result.Entries[0].Text);
                    Assert.AreEqual(card1RunDate3, result.Entries[0].CreationUtcDate);
                    Assert.IsFalse(result.Entries[0].HasBeenEdited);
                    Assert.AreEqual(userId, result.Entries[0].Creator.Id);

                    Assert.AreEqual(card1Text2, result.Entries[1].Text);
                    Assert.AreEqual(card1RunDate2, result.Entries[1].CreationUtcDate);
                    Assert.IsFalse(result.Entries[1].HasBeenEdited);
                    Assert.AreEqual(userId, result.Entries[1].Creator.Id);

                    page2LastObtaintedEntry = result.Entries[1].Id;
                }
                { // Page 3
                    var result = await new GetCardDiscussionEntries(dbContext.AsCallContext()).RunAsync(new GetCardDiscussionEntries.Request(userId, card1Id, 2, page2LastObtaintedEntry));

                    Assert.AreEqual(5, result.TotalCount);
                    Assert.AreEqual(3, result.PageCount);
                    Assert.AreEqual(1, result.Entries.Length);

                    Assert.AreEqual(card1Text1, result.Entries[0].Text);
                    Assert.AreEqual(card1RunDate1, result.Entries[0].CreationUtcDate);
                    Assert.IsFalse(result.Entries[0].HasBeenEdited);
                    Assert.AreEqual(userId, result.Entries[0].Creator.Id);
                }
            }
            { // Card 2
                var result = await new GetCardDiscussionEntries(dbContext.AsCallContext()).RunAsync(new GetCardDiscussionEntries.Request(userId, card2Id, 2, Guid.Empty));

                Assert.AreEqual(2, result.TotalCount);
                Assert.AreEqual(1, result.PageCount);
                Assert.AreEqual(2, result.Entries.Length);

                Assert.AreEqual(card2Text2, result.Entries[0].Text);
                Assert.AreEqual(card2RunDate2, result.Entries[0].CreationUtcDate);
                Assert.IsFalse(result.Entries[0].HasBeenEdited);
                Assert.AreEqual(userId, result.Entries[0].Creator.Id);

                Assert.AreEqual(card2Text1, result.Entries[1].Text);
                Assert.AreEqual(card2RunDate1, result.Entries[1].CreationUtcDate);
                Assert.IsFalse(result.Entries[1].HasBeenEdited);
                Assert.AreEqual(userId, result.Entries[1].Creator.Id);
            }
        }
    }
    [TestMethod()]
    public async Task Success_TwoCards_DifferentUsers_MultipleEntries()
    {
        var db = DbHelper.GetEmptyTestDB();
        var card1CreatingUserId = await UserHelper.CreateInDbAsync(db);
        var card2CreatingUserId = await UserHelper.CreateInDbAsync(db);
        var discussionUserId = await UserHelper.CreateInDbAsync(db);

        var card1Id = await CardHelper.CreateIdAsync(db, card1CreatingUserId, userWithViewIds: null);
        var card1Discussion1Text = RandomHelper.String();
        var card1Discussion1Date = RandomHelper.Date();
        var card1Discussion2Text = RandomHelper.String();
        var card1Discussion2Date = RandomHelper.Date(card1Discussion1Date);
        var card1Discussion3Text = RandomHelper.String();
        var card1Discussion3Date = RandomHelper.Date(card1Discussion2Date);
        var card1Discussion4Text = RandomHelper.String();
        var card1Discussion4Date = RandomHelper.Date(card1Discussion3Date);
        var card1Discussion5Text = RandomHelper.String();
        var card1Discussion5Date = RandomHelper.Date(card1Discussion4Date);

        var card2Id = await CardHelper.CreateIdAsync(db, card2CreatingUserId, userWithViewIds: null);
        var card2Discussion1Text = RandomHelper.String();
        var card2Discussion1Date = RandomHelper.Date();
        var card2Discussion2Text = RandomHelper.String();
        var card2Discussion2Date = RandomHelper.Date(card2Discussion1Date);

        using (var dbContext = new MemCheckDbContext(db))
        {
            await new AddEntryToCardDiscussion(dbContext.AsCallContext(), card1Discussion1Date).RunAsync(new AddEntryToCardDiscussion.Request(card1CreatingUserId, card1Id, card1Discussion1Text));
            await new AddEntryToCardDiscussion(dbContext.AsCallContext(), card1Discussion2Date).RunAsync(new AddEntryToCardDiscussion.Request(discussionUserId, card1Id, card1Discussion2Text));
            await new AddEntryToCardDiscussion(dbContext.AsCallContext(), card1Discussion3Date).RunAsync(new AddEntryToCardDiscussion.Request(card1CreatingUserId, card1Id, card1Discussion3Text));
            await new AddEntryToCardDiscussion(dbContext.AsCallContext(), card1Discussion4Date).RunAsync(new AddEntryToCardDiscussion.Request(card1CreatingUserId, card1Id, card1Discussion4Text));
            await new AddEntryToCardDiscussion(dbContext.AsCallContext(), card1Discussion5Date).RunAsync(new AddEntryToCardDiscussion.Request(discussionUserId, card1Id, card1Discussion5Text));

            await new AddEntryToCardDiscussion(dbContext.AsCallContext(), card2Discussion1Date).RunAsync(new AddEntryToCardDiscussion.Request(card2CreatingUserId, card2Id, card2Discussion1Text));
            await new AddEntryToCardDiscussion(dbContext.AsCallContext(), card2Discussion2Date).RunAsync(new AddEntryToCardDiscussion.Request(discussionUserId, card2Id, card2Discussion2Text));
        }

        using var getDbContext = new MemCheckDbContext(db);

        var card1ResultForCard1CreatingUser = await new GetCardDiscussionEntries(getDbContext.AsCallContext()).RunAsync(new GetCardDiscussionEntries.Request(card1CreatingUserId, card1Id, 10, Guid.Empty));

        Assert.AreEqual(5, card1ResultForCard1CreatingUser.TotalCount);
        Assert.AreEqual(1, card1ResultForCard1CreatingUser.PageCount);
        Assert.HasCount(5, card1ResultForCard1CreatingUser.Entries);
        Assert.AreEqual(card1Discussion5Text, card1ResultForCard1CreatingUser.Entries[0].Text);
        Assert.AreEqual(card1Discussion5Date, card1ResultForCard1CreatingUser.Entries[0].CreationUtcDate);
        Assert.AreEqual(discussionUserId, card1ResultForCard1CreatingUser.Entries[0].Creator.Id);
        Assert.IsFalse(card1ResultForCard1CreatingUser.Entries[0].CanBeEditedByCurrentUser);
        Assert.AreEqual(card1Discussion4Text, card1ResultForCard1CreatingUser.Entries[1].Text);
        Assert.AreEqual(card1Discussion4Date, card1ResultForCard1CreatingUser.Entries[1].CreationUtcDate);
        Assert.AreEqual(card1CreatingUserId, card1ResultForCard1CreatingUser.Entries[1].Creator.Id);
        Assert.IsTrue(card1ResultForCard1CreatingUser.Entries[1].CanBeEditedByCurrentUser);
        Assert.AreEqual(card1Discussion3Text, card1ResultForCard1CreatingUser.Entries[2].Text);
        Assert.AreEqual(card1Discussion3Date, card1ResultForCard1CreatingUser.Entries[2].CreationUtcDate);
        Assert.AreEqual(card1CreatingUserId, card1ResultForCard1CreatingUser.Entries[2].Creator.Id);
        Assert.IsTrue(card1ResultForCard1CreatingUser.Entries[2].CanBeEditedByCurrentUser);
        Assert.AreEqual(card1Discussion2Text, card1ResultForCard1CreatingUser.Entries[3].Text);
        Assert.AreEqual(card1Discussion2Date, card1ResultForCard1CreatingUser.Entries[3].CreationUtcDate);
        Assert.AreEqual(discussionUserId, card1ResultForCard1CreatingUser.Entries[3].Creator.Id);
        Assert.IsFalse(card1ResultForCard1CreatingUser.Entries[3].CanBeEditedByCurrentUser);
        Assert.AreEqual(card1Discussion1Text, card1ResultForCard1CreatingUser.Entries[4].Text);
        Assert.AreEqual(card1Discussion1Date, card1ResultForCard1CreatingUser.Entries[4].CreationUtcDate);
        Assert.AreEqual(card1CreatingUserId, card1ResultForCard1CreatingUser.Entries[4].Creator.Id);
        Assert.IsTrue(card1ResultForCard1CreatingUser.Entries[4].CanBeEditedByCurrentUser);

        var card1ResultForCard2CreatingUser = await new GetCardDiscussionEntries(getDbContext.AsCallContext()).RunAsync(new GetCardDiscussionEntries.Request(card2CreatingUserId, card1Id, 10, Guid.Empty));

        Assert.AreEqual(5, card1ResultForCard2CreatingUser.TotalCount);
        Assert.AreEqual(1, card1ResultForCard2CreatingUser.PageCount);
        Assert.HasCount(5, card1ResultForCard2CreatingUser.Entries);
        Assert.IsFalse(card1ResultForCard2CreatingUser.Entries[0].CanBeEditedByCurrentUser);
        Assert.IsFalse(card1ResultForCard2CreatingUser.Entries[1].CanBeEditedByCurrentUser);
        Assert.IsFalse(card1ResultForCard2CreatingUser.Entries[2].CanBeEditedByCurrentUser);
        Assert.IsFalse(card1ResultForCard2CreatingUser.Entries[3].CanBeEditedByCurrentUser);
        Assert.IsFalse(card1ResultForCard2CreatingUser.Entries[4].CanBeEditedByCurrentUser);

        var card1ResultForDiscussionUser = await new GetCardDiscussionEntries(getDbContext.AsCallContext()).RunAsync(new GetCardDiscussionEntries.Request(discussionUserId, card1Id, 10, Guid.Empty));

        Assert.AreEqual(5, card1ResultForDiscussionUser.TotalCount);
        Assert.AreEqual(1, card1ResultForDiscussionUser.PageCount);
        Assert.HasCount(5, card1ResultForDiscussionUser.Entries);
        Assert.IsTrue(card1ResultForDiscussionUser.Entries[0].CanBeEditedByCurrentUser);
        Assert.IsFalse(card1ResultForDiscussionUser.Entries[1].CanBeEditedByCurrentUser);
        Assert.IsFalse(card1ResultForDiscussionUser.Entries[2].CanBeEditedByCurrentUser);
        Assert.IsTrue(card1ResultForDiscussionUser.Entries[3].CanBeEditedByCurrentUser);
        Assert.IsFalse(card1ResultForDiscussionUser.Entries[4].CanBeEditedByCurrentUser);

        var card2ResultForCard2CreatingUser = await new GetCardDiscussionEntries(getDbContext.AsCallContext()).RunAsync(new GetCardDiscussionEntries.Request(card2CreatingUserId, card2Id, 10, Guid.Empty));

        Assert.AreEqual(2, card2ResultForCard2CreatingUser.TotalCount);
        Assert.AreEqual(1, card2ResultForCard2CreatingUser.PageCount);
        Assert.HasCount(2, card2ResultForCard2CreatingUser.Entries);
        Assert.AreEqual(card2Discussion2Text, card2ResultForCard2CreatingUser.Entries[0].Text);
        Assert.AreEqual(card2Discussion2Date, card2ResultForCard2CreatingUser.Entries[0].CreationUtcDate);
        Assert.AreEqual(discussionUserId, card2ResultForCard2CreatingUser.Entries[0].Creator.Id);
        Assert.IsFalse(card2ResultForCard2CreatingUser.Entries[0].CanBeEditedByCurrentUser);
        Assert.AreEqual(card2Discussion1Text, card2ResultForCard2CreatingUser.Entries[1].Text);
        Assert.AreEqual(card2Discussion1Date, card2ResultForCard2CreatingUser.Entries[1].CreationUtcDate);
        Assert.AreEqual(card2CreatingUserId, card2ResultForCard2CreatingUser.Entries[1].Creator.Id);
        Assert.IsTrue(card2ResultForCard2CreatingUser.Entries[1].CanBeEditedByCurrentUser);

        var card2ResultForCard1CreatingUser = await new GetCardDiscussionEntries(getDbContext.AsCallContext()).RunAsync(new GetCardDiscussionEntries.Request(card1CreatingUserId, card2Id, 10, Guid.Empty));

        Assert.AreEqual(2, card2ResultForCard1CreatingUser.TotalCount);
        Assert.AreEqual(1, card2ResultForCard1CreatingUser.PageCount);
        Assert.HasCount(2, card2ResultForCard1CreatingUser.Entries);
        Assert.IsFalse(card2ResultForCard1CreatingUser.Entries[0].CanBeEditedByCurrentUser);
        Assert.IsFalse(card2ResultForCard1CreatingUser.Entries[1].CanBeEditedByCurrentUser);

        var card2ResultForDiscussionUser = await new GetCardDiscussionEntries(getDbContext.AsCallContext()).RunAsync(new GetCardDiscussionEntries.Request(discussionUserId, card2Id, 10, Guid.Empty));

        Assert.AreEqual(2, card2ResultForDiscussionUser.TotalCount);
        Assert.AreEqual(1, card2ResultForDiscussionUser.PageCount);
        Assert.HasCount(2, card2ResultForDiscussionUser.Entries);
        Assert.IsTrue(card2ResultForDiscussionUser.Entries[0].CanBeEditedByCurrentUser);
        Assert.IsFalse(card2ResultForDiscussionUser.Entries[1].CanBeEditedByCurrentUser);
    }
}

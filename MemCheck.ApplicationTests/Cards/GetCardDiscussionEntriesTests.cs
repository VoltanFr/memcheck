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
        await Assert.ThrowsExceptionAsync<NonexistentUserException>(async () => await new GetCardDiscussionEntries(dbContext.AsCallContext()).RunAsync(new GetCardDiscussionEntries.Request(Guid.Empty, cardId, 42, 1)));
    }
    [TestMethod()]
    public async Task UserDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<NonexistentUserException>(async () => await new GetCardDiscussionEntries(dbContext.AsCallContext()).RunAsync(new GetCardDiscussionEntries.Request(RandomHelper.Guid(), cardId, 42, 1)));
    }
    [TestMethod()]
    public async Task CardDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<NonexistentCardException>(async () => await new GetCardDiscussionEntries(dbContext.AsCallContext()).RunAsync(new GetCardDiscussionEntries.Request(userId, RandomHelper.Guid(), 42, 1)));
    }
    [TestMethod()]
    public async Task CardIsDeleted()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId, userWithViewIds: userId.AsArray());

        await CardDeletionHelper.DeleteCardAsync(db, userId, cardId);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<NonexistentCardException>(async () => await new GetCardDiscussionEntries(dbContext.AsCallContext()).RunAsync(new GetCardDiscussionEntries.Request(userId, cardId, 42, 1)));
    }
    [TestMethod()]
    public async Task CardIsNotViewableByUser()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId, userWithViewIds: userId.AsArray());
        var otherUserId = await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<UserNotAllowedToAccessCardException>(async () => await new GetCardDiscussionEntries(dbContext.AsCallContext()).RunAsync(new GetCardDiscussionEntries.Request(otherUserId, cardId, 42, 1)));
    }
    [TestMethod()]
    public async Task PageSizeTooSmall()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId, userWithViewIds: userId.AsArray());

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<PageSizeTooSmallException>(async () => await new GetCardDiscussionEntries(dbContext.AsCallContext()).RunAsync(new GetCardDiscussionEntries.Request(userId, cardId, GetCardDiscussionEntries.Request.MinPageSize - 1, 1)));
    }
    [TestMethod()]
    public async Task PageSizeTooBig()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId, userWithViewIds: userId.AsArray());

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<PageSizeTooBigException>(async () => await new GetCardDiscussionEntries(dbContext.AsCallContext()).RunAsync(new GetCardDiscussionEntries.Request(userId, cardId, GetCardDiscussionEntries.Request.MaxPageSize + 1, 1)));
    }
    [TestMethod()]
    public async Task PageIndexZero()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId, userWithViewIds: userId.AsArray());

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<PageIndexTooSmallException>(async () => await new GetCardDiscussionEntries(dbContext.AsCallContext()).RunAsync(new GetCardDiscussionEntries.Request(userId, cardId, 2, 0)));
    }
    [TestMethod()]
    public async Task Success_CardHasNoDiscussionEntry()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var cardId = await CardHelper.CreateIdAsync(db, userId, userWithViewIds: userId.AsArray());

        using var dbContext = new MemCheckDbContext(db);
        var result = await new GetCardDiscussionEntries(dbContext.AsCallContext()).RunAsync(new GetCardDiscussionEntries.Request(userId, cardId, 2, 1));

        Assert.AreEqual(0, result.TotalCount);
        Assert.AreEqual(0, result.PageCount);
        Assert.AreEqual(0, result.Entries.Length);
    }
    [TestMethod()]
    public async Task Success_CardHasSingleDiscussionEntry()
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
            var result = await new GetCardDiscussionEntries(dbContext.AsCallContext()).RunAsync(new GetCardDiscussionEntries.Request(userId, cardId, 2, 1));

            Assert.AreEqual(1, result.TotalCount);
            Assert.AreEqual(1, result.PageCount);
            Assert.AreEqual(1, result.Entries.Length);
            Assert.AreEqual(text, result.Entries.Single().Text);
            Assert.IsFalse(result.Entries.Single().HasBeenEdited);
            Assert.AreEqual(userId, result.Entries.Single().Creator.Id);
        }
    }
    [TestMethod()]
    public async Task Success_TwoCards_MultipleEntries()
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
                { // Page 1
                    var result = await new GetCardDiscussionEntries(dbContext.AsCallContext()).RunAsync(new GetCardDiscussionEntries.Request(userId, card1Id, 2, 1));

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
                }
                { // Page 2
                    var result = await new GetCardDiscussionEntries(dbContext.AsCallContext()).RunAsync(new GetCardDiscussionEntries.Request(userId, card1Id, 2, 2));

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
                }
                { // Page 3
                    var result = await new GetCardDiscussionEntries(dbContext.AsCallContext()).RunAsync(new GetCardDiscussionEntries.Request(userId, card1Id, 2, 3));

                    Assert.AreEqual(5, result.TotalCount);
                    Assert.AreEqual(3, result.PageCount);
                    Assert.AreEqual(1, result.Entries.Length);

                    Assert.AreEqual(card1Text1, result.Entries[0].Text);
                    Assert.AreEqual(card1RunDate1, result.Entries[0].CreationUtcDate);
                    Assert.IsFalse(result.Entries[0].HasBeenEdited);
                    Assert.AreEqual(userId, result.Entries[0].Creator.Id);
                }
                { // Page 4
                    var result = await new GetCardDiscussionEntries(dbContext.AsCallContext()).RunAsync(new GetCardDiscussionEntries.Request(userId, card1Id, 2, 4));

                    Assert.AreEqual(5, result.TotalCount);
                    Assert.AreEqual(3, result.PageCount);
                    Assert.AreEqual(0, result.Entries.Length);
                }
            }
            { // Card 2
                { // Page 1
                    var result = await new GetCardDiscussionEntries(dbContext.AsCallContext()).RunAsync(new GetCardDiscussionEntries.Request(userId, card2Id, 2, 1));

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
                { // Page 2
                    var result = await new GetCardDiscussionEntries(dbContext.AsCallContext()).RunAsync(new GetCardDiscussionEntries.Request(userId, card2Id, 2, 2));

                    Assert.AreEqual(2, result.TotalCount);
                    Assert.AreEqual(1, result.PageCount);
                    Assert.AreEqual(0, result.Entries.Length);
                }
            }
        }
    }
}

using MemCheck.Application.Helpers;
using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Users;

[TestClass()]
public class GetAllUsersStatsTests
{
    [TestMethod()]
    public async Task UserNotLoggedIn()
    {
        using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetAllUsersStats(dbContext.AsCallContext()).RunAsync(new GetAllUsersStats.Request(Guid.Empty, 1, 0, "")));
    }
    [TestMethod()]
    public async Task UserDoesNotExist()
    {
        using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetAllUsersStats(dbContext.AsCallContext()).RunAsync(new GetAllUsersStats.Request(Guid.NewGuid(), 1, 1, "")));
    }
    [TestMethod()]
    public async Task UserIsNotAdmin()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetAllUsersStats(dbContext.AsCallContext()).RunAsync(new GetAllUsersStats.Request(user, 1, 1, "")));
    }
    [TestMethod()]
    public async Task Page0()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<PageIndexTooSmallException>(async () => await new GetAllUsersStats(dbContext.AsCallContext()).RunAsync(new GetAllUsersStats.Request(user, 1, 0, "")));
    }
    [TestMethod()]
    public async Task PageSize0()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<PageSizeTooSmallException>(async () => await new GetAllUsersStats(dbContext.AsCallContext()).RunAsync(new GetAllUsersStats.Request(user, 0, 1, "")));
    }
    [TestMethod()]
    public async Task PageSizeTooBig()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<PageSizeTooBigException>(async () => await new GetAllUsersStats(dbContext.AsCallContext(new TestRoleChecker(user))).RunAsync(new GetAllUsersStats.Request(user, GetAllUsersStats.Request.MaxPageSize + 1, 1, "")));
    }
    [TestMethod()]
    public async Task OnlyUser_OneDeck()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);

        using (var dbContext = new MemCheckDbContext(db))
        {
            var deckId = dbContext.Decks.Single().Id;
            var cardId1 = await CardHelper.CreateIdAsync(db, user.Id);
            await DeckHelper.AddCardAsync(db, deckId, cardId1);
            var cardId2 = await CardHelper.CreateIdAsync(db, user.Id);
            await DeckHelper.AddCardAsync(db, deckId, cardId2);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var loaded = await new GetAllUsersStats(dbContext.AsCallContext(new TestRoleChecker(user.Id))).RunAsync(new GetAllUsersStats.Request(user.Id, 10, 1, ""));
            Assert.AreEqual(1, loaded.TotalCount);
            Assert.AreEqual(1, loaded.PageCount);
            var loadedUsers = loaded.Users.ToArray();
            Assert.AreEqual(1, loadedUsers.Length);
            var userFromQuery = loadedUsers[0];
            Assert.AreEqual(user.UserName, userFromQuery.UserName);
            Assert.AreEqual(IRoleChecker.AdminRoleName, userFromQuery.Roles);
            Assert.AreEqual(user.Email, userFromQuery.Email);
            Assert.AreEqual(0, userFromQuery.NotifInterval);
            Assert.AreEqual(DateTime.MinValue, userFromQuery.LastNotifUtcDate);
            Assert.AreEqual(1, userFromQuery.Decks.Length);
            Assert.AreEqual(2, userFromQuery.Decks.Single().CardCount);
        }
    }
    [TestMethod()]
    public async Task OnlyUser_TwoDecks()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);

        using (var dbContext = new MemCheckDbContext(db))
        {
            var deck1 = dbContext.Decks.Single().Id;
            var cardId1 = await CardHelper.CreateIdAsync(db, user.Id);
            await DeckHelper.AddCardAsync(db, deck1, cardId1);
            var cardId2 = await CardHelper.CreateIdAsync(db, user.Id);
            await DeckHelper.AddCardAsync(db, deck1, cardId2);
        }

        var deck2Name = RandomHelper.String();
        var deck2 = await DeckHelper.CreateAsync(db, user.Id, deck2Name);
        var cardId3 = await CardHelper.CreateIdAsync(db, user.Id);
        await DeckHelper.AddCardAsync(db, deck2, cardId3);
        var cardId4 = await CardHelper.CreateIdAsync(db, user.Id);
        await DeckHelper.AddCardAsync(db, deck2, cardId4);
        var cardId5 = await CardHelper.CreateIdAsync(db, user.Id);
        await DeckHelper.AddCardAsync(db, deck2, cardId5);

        using (var dbContext = new MemCheckDbContext(db))
        {
            var loaded = await new GetAllUsersStats(dbContext.AsCallContext(new TestRoleChecker(user.Id))).RunAsync(new GetAllUsersStats.Request(user.Id, 10, 1, ""));
            Assert.AreEqual(1, loaded.TotalCount);
            Assert.AreEqual(1, loaded.PageCount);
            var loadedUsers = loaded.Users.ToArray();
            Assert.AreEqual(1, loadedUsers.Length);
            var userFromQuery = loadedUsers[0];
            Assert.AreEqual(user.UserName, userFromQuery.UserName);
            Assert.AreEqual(IRoleChecker.AdminRoleName, userFromQuery.Roles);
            Assert.AreEqual(user.Email, userFromQuery.Email);
            Assert.AreEqual(0, userFromQuery.NotifInterval);
            Assert.AreEqual(DateTime.MinValue, userFromQuery.LastNotifUtcDate);
            Assert.AreEqual(2, userFromQuery.Decks.Length);
            Assert.AreEqual(3, userFromQuery.Decks.Single(deck => deck.Name == deck2Name).CardCount);
        }
    }
    [TestMethod()]
    public async Task Paging()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user1Name = "a" + RandomHelper.String();
        var user1 = await UserHelper.CreateInDbAsync(db, userName: user1Name);
        var user2Name = "b" + RandomHelper.String();
        await UserHelper.CreateInDbAsync(db, userName: user2Name);

        using var dbContext = new MemCheckDbContext(db);

        var loaded = await new GetAllUsersStats(dbContext.AsCallContext(new TestRoleChecker(user1))).RunAsync(new GetAllUsersStats.Request(user1, 1, 1, ""));
        Assert.AreEqual(2, loaded.TotalCount);
        Assert.AreEqual(2, loaded.PageCount);
        Assert.AreEqual(user1Name, loaded.Users.Single().UserName);

        loaded = await new GetAllUsersStats(dbContext.AsCallContext(new TestRoleChecker(user1))).RunAsync(new GetAllUsersStats.Request(user1, 1, 2, ""));
        Assert.AreEqual(2, loaded.TotalCount);
        Assert.AreEqual(2, loaded.PageCount);
        Assert.AreEqual(user2Name, loaded.Users.Single().UserName);

        loaded = await new GetAllUsersStats(dbContext.AsCallContext(new TestRoleChecker(user1))).RunAsync(new GetAllUsersStats.Request(user1, 1, 3, ""));
        Assert.AreEqual(2, loaded.TotalCount);
        Assert.AreEqual(2, loaded.PageCount);
        Assert.IsFalse(loaded.Users.Any());
    }
    [TestMethod()]
    public async Task Filtering()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user1Name = "A User Name";
        var user1 = await UserHelper.CreateInDbAsync(db, userName: user1Name);
        var user2Name = "b" + RandomHelper.String();
        await UserHelper.CreateInDbAsync(db, userName: user2Name);

        using var dbContext = new MemCheckDbContext(db);

        var loaded = await new GetAllUsersStats(dbContext.AsCallContext(new TestRoleChecker(user1))).RunAsync(new GetAllUsersStats.Request(user1, 1, 1, "a useR name"));
        Assert.AreEqual(1, loaded.TotalCount);
        Assert.AreEqual(1, loaded.PageCount);
        Assert.AreEqual(user1Name, loaded.Users.Single().UserName);

        loaded = await new GetAllUsersStats(dbContext.AsCallContext(new TestRoleChecker(user1))).RunAsync(new GetAllUsersStats.Request(user1, 1, 1, user2Name.ToUpperInvariant()));
        Assert.AreEqual(1, loaded.TotalCount);
        Assert.AreEqual(1, loaded.PageCount);
        Assert.AreEqual(user2Name, loaded.Users.Single().UserName);

        loaded = await new GetAllUsersStats(dbContext.AsCallContext(new TestRoleChecker(user1))).RunAsync(new GetAllUsersStats.Request(user1, 1, 1, RandomHelper.String()));
        Assert.AreEqual(0, loaded.TotalCount);
        Assert.AreEqual(0, loaded.PageCount);
        Assert.IsFalse(loaded.Users.Any());
    }
}

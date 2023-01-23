using MemCheck.Application.Heaping;
using MemCheck.Application.Helpers;
using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Decks;

[TestClass()]
public class CreateDeckTests
{
    [TestMethod()]
    public async Task UserNotLoggedIn()
    {
        using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new CreateDeck(dbContext.AsCallContext()).RunAsync(new CreateDeck.Request(Guid.Empty, RandomHelper.String(), 0)));
    }
    [TestMethod()]
    public async Task UserDoesNotExist()
    {
        using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new CreateDeck(dbContext.AsCallContext()).RunAsync(new CreateDeck.Request(Guid.NewGuid(), RandomHelper.String(), 0)));
    }
    [TestMethod()]
    public async Task NameNotTrimmed()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new CreateDeck(dbContext.AsCallContext()).RunAsync(new CreateDeck.Request(user, RandomHelper.String() + '\t', 0)));
    }
    [TestMethod()]
    public async Task NameTooShort()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new CreateDeck(dbContext.AsCallContext()).RunAsync(new CreateDeck.Request(user, RandomHelper.String(QueryValidationHelper.DeckMinNameLength - 1), 0)));
    }
    [TestMethod()]
    public async Task NameTooLong()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new CreateDeck(dbContext.AsCallContext()).RunAsync(new CreateDeck.Request(user, RandomHelper.String(QueryValidationHelper.DeckMaxNameLength + 1), 0)));
    }
    [TestMethod()]
    public async Task DeckWithThisNameExists()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var otherDeckName = RandomHelper.String();
        await DeckHelper.CreateAsync(db, user, otherDeckName);
        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new CreateDeck(dbContext.AsCallContext()).RunAsync(new CreateDeck.Request(user, otherDeckName, RandomHelper.HeapingAlgorithm())));
    }
    [TestMethod()]
    public async Task InexistentAlgorithm()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new CreateDeck(dbContext.AsCallContext()).RunAsync(new CreateDeck.Request(user, RandomHelper.String(), RandomHelper.ValueNotInSet(HeapingAlgorithms.Instance.Ids))));
    }
    [TestMethod()]
    public async Task Success()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var name = RandomHelper.String();
        var algo = RandomHelper.HeapingAlgorithm();

        using (var dbContext = new MemCheckDbContext(db))
            await new CreateDeck(dbContext.AsCallContext()).RunAsync(new CreateDeck.Request(user, name, algo));

        using (var dbContext = new MemCheckDbContext(db))
        {
            Assert.AreEqual(2, await dbContext.Decks.CountAsync());

            var deckFromDb = await dbContext.Decks.SingleAsync(deck => deck.Description == name);
            Assert.AreEqual(algo, deckFromDb.HeapingAlgorithmId);
        }
    }
}

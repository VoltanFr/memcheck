using MemCheck.Application.Helpers;
using MemCheck.Basics;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards;

[TestClass()]
public class CountNonPublicCardsTests
{
    [TestMethod()]
    public async Task EmptyDb()
    {
        var db = DbHelper.GetEmptyTestDB();
        using var dbContext = new MemCheckDbContext(db);
        var result = await new CountNonPublicCards(dbContext.AsCallContext()).RunAsync(new CountNonPublicCards.Request());
        Assert.AreEqual(0, result.Count);
    }
    [TestMethod()]
    public async Task SingleCardVisibleToSingleUser()
    {
        var db = DbHelper.GetEmptyTestDB();
        var cardCreator = await UserHelper.CreateInDbAsync(db);
        await CardHelper.CreateIdAsync(db, cardCreator, userWithViewIds: cardCreator.AsArray());

        using var dbContext = new MemCheckDbContext(db);
        var result = await new CountNonPublicCards(dbContext.AsCallContext()).RunAsync(new CountNonPublicCards.Request());
        Assert.AreEqual(1, result.Count);
    }
    [TestMethod()]
    public async Task SingleCardVisibleToSeveralUsers()
    {
        var db = DbHelper.GetEmptyTestDB();
        var cardCreator = await UserHelper.CreateInDbAsync(db);
        var otherUser = await UserHelper.CreateInDbAsync(db);
        await CardHelper.CreateIdAsync(db, cardCreator, userWithViewIds: new[] { cardCreator, otherUser });

        using var dbContext = new MemCheckDbContext(db);
        var result = await new CountNonPublicCards(dbContext.AsCallContext()).RunAsync(new CountNonPublicCards.Request());
        Assert.AreEqual(1, result.Count);
    }
    [TestMethod()]
    public async Task SinglePublicCard()
    {
        var db = DbHelper.GetEmptyTestDB();
        var cardCreator = await UserHelper.CreateInDbAsync(db);
        await CardHelper.CreateIdAsync(db, cardCreator);

        using var dbContext = new MemCheckDbContext(db);
        var result = await new CountNonPublicCards(dbContext.AsCallContext()).RunAsync(new CountNonPublicCards.Request());
        Assert.AreEqual(0, result.Count);
    }
    [TestMethod()]
    public async Task ComplexCase()
    {
        var db = DbHelper.GetEmptyTestDB();

        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new CountNonPublicCards(dbContext.AsCallContext()).RunAsync(new CountNonPublicCards.Request());
            Assert.AreEqual(0, result.Count);
        }

        var user1 = await UserHelper.CreateInDbAsync(db);
        var user2 = await UserHelper.CreateInDbAsync(db);
        var user3 = await UserHelper.CreateInDbAsync(db);

        await CardHelper.CreateAsync(db, user1); // Public

        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new CountNonPublicCards(dbContext.AsCallContext()).RunAsync(new CountNonPublicCards.Request());
            Assert.AreEqual(0, result.Count);
        }

        await CardHelper.CreateIdAsync(db, user1, userWithViewIds: new[] { user1, user3 }); // Non-public

        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new CountNonPublicCards(dbContext.AsCallContext()).RunAsync(new CountNonPublicCards.Request());
            Assert.AreEqual(1, result.Count);
        }

        await CardHelper.CreateIdAsync(db, user2, userWithViewIds: new[] { user2 }); // Non-public
        await CardHelper.CreateIdAsync(db, user1, userWithViewIds: new[] { user1, user2, user3 }); // Non-public

        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new CountNonPublicCards(dbContext.AsCallContext()).RunAsync(new CountNonPublicCards.Request());
            Assert.AreEqual(3, result.Count);
        }

        await CardHelper.CreateAsync(db, user1); // Public

        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new CountNonPublicCards(dbContext.AsCallContext()).RunAsync(new CountNonPublicCards.Request());
            Assert.AreEqual(3, result.Count);
        }

        await CardHelper.CreateAsync(db, user2); // Public

        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new CountNonPublicCards(dbContext.AsCallContext()).RunAsync(new CountNonPublicCards.Request());
            Assert.AreEqual(3, result.Count);
        }

        await CardHelper.CreateAsync(db, user3); // Public

        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new CountNonPublicCards(dbContext.AsCallContext()).RunAsync(new CountNonPublicCards.Request());
            Assert.AreEqual(3, result.Count);
        }
    }
}

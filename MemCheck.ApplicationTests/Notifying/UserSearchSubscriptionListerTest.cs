using MemCheck.Application.Helpers;
using MemCheck.Application.Notifiying;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Notifying;

[TestClass()]
public class UserSearchSubscriptionListerTest
{
    [TestMethod()]
    public async Task TestWithoutUser()
    {
        using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
        var subscriptions = await new UserSearchSubscriptionLister(dbContext.AsCallContext()).RunAsync(Guid.Empty);
        Assert.AreEqual(0, subscriptions.Length);
    }
    [TestMethod()]
    public async Task TestUserWithoutSubscription()
    {
        var testDB = DbHelper.GetEmptyTestDB();

        var user = await UserHelper.CreateInDbAsync(testDB);

        using var dbContext = new MemCheckDbContext(testDB);
        var subscriptions = await new UserSearchSubscriptionLister(dbContext.AsCallContext()).RunAsync(user);
        Assert.AreEqual(0, subscriptions.Length);
    }
    [TestMethod()]
    public async Task TestUsersWithSubscriptions()
    {
        var testDB = DbHelper.GetEmptyTestDB();

        var user1 = await UserHelper.CreateInDbAsync(testDB);

        var user2 = await UserHelper.CreateInDbAsync(testDB);
        await SearchSubscriptionHelper.CreateAsync(testDB, user2);

        var user3 = await UserHelper.CreateInDbAsync(testDB);
        await SearchSubscriptionHelper.CreateAsync(testDB, user3);
        await SearchSubscriptionHelper.CreateAsync(testDB, user3);
        await SearchSubscriptionHelper.CreateAsync(testDB, user3);

        using var dbContext = new MemCheckDbContext(testDB);
        var counter = new UserSearchSubscriptionLister(dbContext.AsCallContext());

        var user1Subscriptions = await counter.RunAsync(user1);
        Assert.AreEqual(0, user1Subscriptions.Length);

        var user2Subscriptions = await counter.RunAsync(user2);
        Assert.AreEqual(1, user2Subscriptions.Length);

        var user3Subscriptions = await counter.RunAsync(user3);
        Assert.AreEqual(3, user3Subscriptions.Length);
    }
}

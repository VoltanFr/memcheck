using MemCheck.Application.Helpers;
using MemCheck.Application.Notifiying;
using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Notifying;

[TestClass()]
public class SetSearchSubscriptionNameTest
{
    [TestMethod()]
    public async Task TestNoUser()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(testDB);
        var subscription = await SearchSubscriptionHelper.CreateAsync(testDB, userId);

        using var dbContext = new MemCheckDbContext(testDB);
        var request = new SetSearchSubscriptionName.Request(Guid.Empty, subscription.Id, RandomHelper.String());
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new SetSearchSubscriptionName(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task TestInvalidSubscriptionId()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(testDB);

        using var dbContext = new MemCheckDbContext(testDB);
        var request = new SetSearchSubscriptionName.Request(userId, Guid.Empty, RandomHelper.String());
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new SetSearchSubscriptionName(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task UserNotOwnerOfSubscription()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var subscriptionOwnerId = await UserHelper.CreateInDbAsync(testDB);
        var subscription = await SearchSubscriptionHelper.CreateAsync(testDB, subscriptionOwnerId);
        var userId = await UserHelper.CreateInDbAsync(testDB);

        using var dbContext = new MemCheckDbContext(testDB);
        var request = new SetSearchSubscriptionName.Request(userId, subscription.Id, RandomHelper.String());
        await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new SetSearchSubscriptionName(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task TestTooShortName()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(testDB);
        var subscription = await SearchSubscriptionHelper.CreateAsync(testDB, userId);

        using var dbContext = new MemCheckDbContext(testDB);
        var request = new SetSearchSubscriptionName.Request(userId, subscription.Id, "      " + RandomHelper.String(SetSearchSubscriptionName.Request.MinNameLength - 1) + "\t\t");
        await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new SetSearchSubscriptionName(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task TestTooLongName()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(testDB);
        var subscription = await SearchSubscriptionHelper.CreateAsync(testDB, userId);

        using var dbContext = new MemCheckDbContext(testDB);
        var request = new SetSearchSubscriptionName.Request(userId, subscription.Id, RandomHelper.String(SetSearchSubscriptionName.Request.MaxNameLength + 1));
        await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new SetSearchSubscriptionName(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task NameWithMaxLengthDoesNotThrow()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(testDB);
        var subscription = await SearchSubscriptionHelper.CreateAsync(testDB, userId);

        using var dbContext = new MemCheckDbContext(testDB);
        var request = new SetSearchSubscriptionName.Request(userId, subscription.Id, "      " + RandomHelper.String(SetSearchSubscriptionName.Request.MaxNameLength) + "\t\t");
        await new SetSearchSubscriptionName(dbContext.AsCallContext()).RunAsync(request);
    }
    [TestMethod()]
    public async Task TestCorrectRenaming()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(testDB);
        var subscription = await SearchSubscriptionHelper.CreateAsync(testDB, userId);
        var newName = RandomHelper.String();

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var request = new SetSearchSubscriptionName.Request(userId, subscription.Id, "      " + newName + "\t\t");
            await new SetSearchSubscriptionName(dbContext.AsCallContext()).RunAsync(request);
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var request = new GetSearchSubscriptions.Request(userId);
            var subscriptions = await new GetSearchSubscriptions(dbContext.AsCallContext()).RunAsync(request);
            Assert.AreEqual(1, subscriptions.Count());
            Assert.AreEqual(newName, subscriptions.Single().Name);
        }
    }
}

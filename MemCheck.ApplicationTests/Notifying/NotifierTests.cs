﻿using MemCheck.Application.Helpers;
using MemCheck.Application.Notifiying;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Notifying;

[TestClass()]
public class NotifierTests
{
    [TestMethod()]
    public async Task TestRun_NoUserToNotify()
    {
        var db = DbHelper.GetEmptyTestDB();

        var userCardSubscriptionCounter = new Mock<IUserCardSubscriptionCounter>(MockBehavior.Strict);
        var userCardVersionsNotifier = new Mock<IUserCardVersionsNotifier>(MockBehavior.Strict);
        var userCardDeletionsNotifier = new Mock<IUserCardDeletionsNotifier>(MockBehavior.Strict);
        var userLastNotifDateUpdater = new Mock<IUserLastNotifDateUpdater>(MockBehavior.Strict);
        var userSearchSubscriptionLister = new Mock<IUserSearchSubscriptionLister>(MockBehavior.Strict);
        var userSearchNotifier = new Mock<IUserSearchNotifier>(MockBehavior.Strict);

        var usersToNotifyGetter = new Mock<IUsersToNotifyGetter>(MockBehavior.Strict);
        usersToNotifyGetter.Setup(getter => getter.Run(It.IsAny<DateTime?>())).Returns(ImmutableArray<MemCheckUser>.Empty);

        using var dbContext = new MemCheckDbContext(db);

        var notifier = new Notifier(dbContext.AsCallContext(), userCardSubscriptionCounter.Object, userCardVersionsNotifier.Object, userCardDeletionsNotifier.Object, usersToNotifyGetter.Object, userLastNotifDateUpdater.Object, userSearchSubscriptionLister.Object, userSearchNotifier.Object, new List<string>());
        var result = await notifier.RunAsync(new Notifier.Request());
        Assert.AreEqual(0, result.UserNotifications.Length);

        usersToNotifyGetter.VerifyAll();
    }
    [TestMethod()]
    public async Task TestRun_VersionToNotify()
    {
        var db = DbHelper.GetEmptyTestDB();

        var now = new DateTime(2030, 1, 2);
        var user = UserHelper.Create(1, new DateTime(2030, 1, 1));

        var usersToNotifyGetter = new Mock<IUsersToNotifyGetter>(MockBehavior.Strict);
        usersToNotifyGetter.Setup(getter => getter.Run(It.IsAny<DateTime?>())).Returns(ImmutableArray.Create(user));

        var userCardSubscriptionCounter = new Mock<IUserCardSubscriptionCounter>(MockBehavior.Strict);
        userCardSubscriptionCounter.Setup(counter => counter.RunAsync(user.Id)).ReturnsAsync(12);

        var userCardVersionsNotifier = new Mock<IUserCardVersionsNotifier>(MockBehavior.Strict);
        var subscription = new CardNotificationSubscription();
        var cardId = Guid.NewGuid();
        var cardVersion = new IUserCardVersionsNotifier.ResultCardVersion(cardId, RandomHelper.String(), RandomHelper.String(), new DateTime(2029, 12, 15), RandomHelper.String(), true, null, subscription);
        userCardVersionsNotifier.Setup(notifier => notifier.RunAsync(user.Id)).ReturnsAsync(new IUserCardVersionsNotifier.CardVersionsNotifierResult(new IUserCardVersionsNotifier.ResultCard[] { new(cardId, ImmutableArray.Create(cardVersion), ImmutableArray<IUserCardVersionsNotifier.ResultDiscussionEntry>.Empty) }.ToImmutableArray()));

        var userCardDeletionsNotifier = new Mock<IUserCardDeletionsNotifier>(MockBehavior.Strict);
        userCardDeletionsNotifier.Setup(notifier => notifier.RunAsync(user.Id)).ReturnsAsync(ImmutableArray<CardDeletion>.Empty);

        var userLastNotifDateUpdater = new Mock<IUserLastNotifDateUpdater>(MockBehavior.Strict);
        userLastNotifDateUpdater.Setup(updater => updater.RunAsync(user.Id)).Returns(Task.CompletedTask);

        var userSearchSubscriptionLister = new Mock<IUserSearchSubscriptionLister>(MockBehavior.Strict);
        userSearchSubscriptionLister.Setup(lister => lister.RunAsync(user.Id)).ReturnsAsync(ImmutableArray<SearchSubscription>.Empty);

        var userSearchNotifier = new Mock<IUserSearchNotifier>(MockBehavior.Strict);

        using var dbContext = new MemCheckDbContext(db);

        var notifier = new Notifier(dbContext.AsCallContext(), userCardSubscriptionCounter.Object, userCardVersionsNotifier.Object, userCardDeletionsNotifier.Object, usersToNotifyGetter.Object, userLastNotifDateUpdater.Object, userSearchSubscriptionLister.Object, userSearchNotifier.Object, new List<string>(), now);
        var result = await notifier.RunAsync(new Notifier.Request());
        Assert.AreEqual(1, result.UserNotifications.Length);
        Assert.AreEqual(user.UserName, result.UserNotifications[0].UserName);
        Assert.AreEqual(1, result.UserNotifications[0].Cards.Length);
        Assert.AreEqual(cardVersion, result.UserNotifications[0].Cards[0].CardVersions.First());
        Assert.AreEqual(0, result.UserNotifications[0].DeletedCards.Length);
        Assert.AreEqual(12, result.UserNotifications[0].SubscribedCardCount);

        userCardDeletionsNotifier.VerifyAll();
        usersToNotifyGetter.VerifyAll();
        userCardSubscriptionCounter.VerifyAll();
        userCardVersionsNotifier.VerifyAll();
        userLastNotifDateUpdater.VerifyAll();
        userSearchSubscriptionLister.VerifyAll();
    }
    [TestMethod()]
    public async Task TestRun_DeletionsToNotify()
    {
        var db = DbHelper.GetEmptyTestDB();

        var now = new DateTime(2030, 1, 2);
        var user = UserHelper.Create(1, new DateTime(2030, 1, 1));

        var usersToNotifyGetter = new Mock<IUsersToNotifyGetter>(MockBehavior.Strict);
        usersToNotifyGetter.Setup(getter => getter.Run(It.IsAny<DateTime?>())).Returns(ImmutableArray.Create(user));

        var userCardSubscriptionCounter = new Mock<IUserCardSubscriptionCounter>(MockBehavior.Strict);
        userCardSubscriptionCounter.Setup(counter => counter.RunAsync(user.Id)).ReturnsAsync(1);

        var userCardVersionsNotifier = new Mock<IUserCardVersionsNotifier>(MockBehavior.Strict);
        var userCardVersionsNotifierResult = new IUserCardVersionsNotifier.CardVersionsNotifierResult(ImmutableArray<IUserCardVersionsNotifier.ResultCard>.Empty);
        userCardVersionsNotifier.Setup(notifier => notifier.RunAsync(user.Id)).ReturnsAsync(userCardVersionsNotifierResult);

        var userCardDeletionsNotifier = new Mock<IUserCardDeletionsNotifier>(MockBehavior.Strict);
        var cardDeletion = new CardDeletion(RandomHelper.String(), RandomHelper.String(), new DateTime(2029, 12, 15), RandomHelper.String(), true);
        userCardDeletionsNotifier.Setup(notifier => notifier.RunAsync(user.Id)).ReturnsAsync(ImmutableArray.Create(cardDeletion));

        var userLastNotifDateUpdater = new Mock<IUserLastNotifDateUpdater>(MockBehavior.Strict);
        userLastNotifDateUpdater.Setup(updater => updater.RunAsync(user.Id)).Returns(Task.CompletedTask);

        var userSearchSubscriptionLister = new Mock<IUserSearchSubscriptionLister>(MockBehavior.Strict);
        userSearchSubscriptionLister.Setup(lister => lister.RunAsync(user.Id)).ReturnsAsync(ImmutableArray<SearchSubscription>.Empty);

        var userSearchNotifier = new Mock<IUserSearchNotifier>(MockBehavior.Strict);

        using var dbContext = new MemCheckDbContext(db);

        var notifier = new Notifier(dbContext.AsCallContext(), userCardSubscriptionCounter.Object, userCardVersionsNotifier.Object, userCardDeletionsNotifier.Object, usersToNotifyGetter.Object, userLastNotifDateUpdater.Object, userSearchSubscriptionLister.Object, userSearchNotifier.Object, new List<string>(), now);
        var result = await notifier.RunAsync(new Notifier.Request());
        Assert.AreEqual(1, result.UserNotifications.Length);
        Assert.AreEqual(user.UserName, result.UserNotifications[0].UserName);
        Assert.AreEqual(0, result.UserNotifications[0].Cards.Length);
        Assert.AreEqual(1, result.UserNotifications[0].DeletedCards.Length);
        Assert.AreEqual(cardDeletion, result.UserNotifications[0].DeletedCards[0]);
        Assert.AreEqual(1, result.UserNotifications[0].SubscribedCardCount);

        userCardDeletionsNotifier.VerifyAll();
        usersToNotifyGetter.VerifyAll();
        userCardSubscriptionCounter.VerifyAll();
        userCardVersionsNotifier.VerifyAll();
        userLastNotifDateUpdater.VerifyAll();
        userSearchSubscriptionLister.VerifyAll();
    }
}

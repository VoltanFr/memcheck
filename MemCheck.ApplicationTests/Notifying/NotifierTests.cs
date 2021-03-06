﻿using MemCheck.Application.Notifying;
using MemCheck.Application.Tests.Helpers;
using MemCheck.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace MemCheck.Application.Tests.Notifying
{
    [TestClass()]
    public class NotifierTests
    {
        [TestMethod()]
        public async Task TestRun_NoUserToNotify()
        {
            var userCardSubscriptionCounter = new Mock<IUserCardSubscriptionCounter>(MockBehavior.Strict);
            var userCardVersionsNotifier = new Mock<IUserCardVersionsNotifier>(MockBehavior.Strict);
            var userCardDeletionsNotifier = new Mock<IUserCardDeletionsNotifier>(MockBehavior.Strict);
            var userLastNotifDateUpdater = new Mock<IUserLastNotifDateUpdater>(MockBehavior.Strict);
            var userSearchSubscriptionLister = new Mock<IUserSearchSubscriptionLister>(MockBehavior.Strict);
            var userSearchNotifier = new Mock<IUserSearchNotifier>(MockBehavior.Strict);

            var usersToNotifyGetter = new Mock<IUsersToNotifyGetter>(MockBehavior.Strict);
            usersToNotifyGetter.Setup(getter => getter.Run(It.IsAny<DateTime?>())).Returns(ImmutableArray<MemCheckUser>.Empty);

            var notifier = new Notifier(userCardSubscriptionCounter.Object, userCardVersionsNotifier.Object, userCardDeletionsNotifier.Object, usersToNotifyGetter.Object, userLastNotifDateUpdater.Object, userSearchSubscriptionLister.Object, userSearchNotifier.Object, new List<string>());
            var result = await notifier.GetNotificationsAndUpdateLastNotifDatesAsync();
            Assert.AreEqual(0, result.UserNotifications.Length);

            usersToNotifyGetter.VerifyAll();
        }
        [TestMethod()]
        public async Task TestRun_VersionToNotify()
        {
            var now = new DateTime(2030, 1, 2);
            var user = UserHelper.Create(1, new DateTime(2030, 1, 1));

            var usersToNotifyGetter = new Mock<IUsersToNotifyGetter>(MockBehavior.Strict);
            usersToNotifyGetter.Setup(getter => getter.Run(It.IsAny<DateTime?>())).Returns(ImmutableArray.Create(user));

            var userCardSubscriptionCounter = new Mock<IUserCardSubscriptionCounter>(MockBehavior.Strict);
            userCardSubscriptionCounter.Setup(counter => counter.RunAsync(user.Id)).ReturnsAsync(12);

            var userCardVersionsNotifier = new Mock<IUserCardVersionsNotifier>(MockBehavior.Strict);
            var cardVersion = new CardVersion(Guid.NewGuid(), RandomHelper.String(), RandomHelper.String(), new DateTime(2029, 12, 15), RandomHelper.String(), true, null);
            userCardVersionsNotifier.Setup(notifier => notifier.RunAsync(user.Id)).ReturnsAsync(ImmutableArray.Create(cardVersion));

            var userCardDeletionsNotifier = new Mock<IUserCardDeletionsNotifier>(MockBehavior.Strict);
            userCardDeletionsNotifier.Setup(notifier => notifier.RunAsync(user.Id)).ReturnsAsync(ImmutableArray<CardDeletion>.Empty);

            var userLastNotifDateUpdater = new Mock<IUserLastNotifDateUpdater>(MockBehavior.Strict);
            userLastNotifDateUpdater.Setup(updater => updater.RunAsync(user.Id)).Returns(Task.CompletedTask);

            var userSearchSubscriptionLister = new Mock<IUserSearchSubscriptionLister>(MockBehavior.Strict);
            userSearchSubscriptionLister.Setup(lister => lister.RunAsync(user.Id)).ReturnsAsync(ImmutableArray<SearchSubscription>.Empty);

            var userSearchNotifier = new Mock<IUserSearchNotifier>(MockBehavior.Strict);

            var notifier = new Notifier(userCardSubscriptionCounter.Object, userCardVersionsNotifier.Object, userCardDeletionsNotifier.Object, usersToNotifyGetter.Object, userLastNotifDateUpdater.Object, userSearchSubscriptionLister.Object, userSearchNotifier.Object, new List<string>());
            var result = await notifier.GetNotificationsAndUpdateLastNotifDatesAsync(now);
            Assert.AreEqual(1, result.UserNotifications.Length);
            Assert.AreEqual(user.UserName, result.UserNotifications[0].UserName);
            Assert.AreEqual(1, result.UserNotifications[0].CardVersions.Length);
            Assert.AreEqual(cardVersion, result.UserNotifications[0].CardVersions[0]);
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
            var now = new DateTime(2030, 1, 2);
            var user = UserHelper.Create(1, new DateTime(2030, 1, 1));

            var usersToNotifyGetter = new Mock<IUsersToNotifyGetter>(MockBehavior.Strict);
            usersToNotifyGetter.Setup(getter => getter.Run(It.IsAny<DateTime?>())).Returns(ImmutableArray.Create(user));

            var userCardSubscriptionCounter = new Mock<IUserCardSubscriptionCounter>(MockBehavior.Strict);
            userCardSubscriptionCounter.Setup(counter => counter.RunAsync(user.Id)).ReturnsAsync(1);

            var userCardVersionsNotifier = new Mock<IUserCardVersionsNotifier>(MockBehavior.Strict);
            userCardVersionsNotifier.Setup(notifier => notifier.RunAsync(user.Id)).ReturnsAsync(ImmutableArray<CardVersion>.Empty);

            var userCardDeletionsNotifier = new Mock<IUserCardDeletionsNotifier>(MockBehavior.Strict);
            var cardDeletion = new CardDeletion(RandomHelper.String(), RandomHelper.String(), new DateTime(2029, 12, 15), RandomHelper.String(), true);
            userCardDeletionsNotifier.Setup(notifier => notifier.RunAsync(user.Id)).ReturnsAsync(ImmutableArray.Create(cardDeletion));

            var userLastNotifDateUpdater = new Mock<IUserLastNotifDateUpdater>(MockBehavior.Strict);
            userLastNotifDateUpdater.Setup(updater => updater.RunAsync(user.Id)).Returns(Task.CompletedTask);

            var userSearchSubscriptionLister = new Mock<IUserSearchSubscriptionLister>(MockBehavior.Strict);
            userSearchSubscriptionLister.Setup(lister => lister.RunAsync(user.Id)).ReturnsAsync(ImmutableArray<SearchSubscription>.Empty);

            var userSearchNotifier = new Mock<IUserSearchNotifier>(MockBehavior.Strict);

            var notifier = new Notifier(userCardSubscriptionCounter.Object, userCardVersionsNotifier.Object, userCardDeletionsNotifier.Object, usersToNotifyGetter.Object, userLastNotifDateUpdater.Object, userSearchSubscriptionLister.Object, userSearchNotifier.Object, new List<string>());
            var result = await notifier.GetNotificationsAndUpdateLastNotifDatesAsync(now);
            Assert.AreEqual(1, result.UserNotifications.Length);
            Assert.AreEqual(user.UserName, result.UserNotifications[0].UserName);
            Assert.AreEqual(0, result.UserNotifications[0].CardVersions.Length);
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
}

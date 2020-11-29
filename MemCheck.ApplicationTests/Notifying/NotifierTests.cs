using Microsoft.VisualStudio.TestTools.UnitTesting;
using MemCheck.Database;
using MemCheck.Application.Notifying;
using System.Threading.Tasks;
using System.Linq;
using Moq;
using System;
using MemCheck.Domain;
using System.Collections.Immutable;

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

            var usersToNotifyGetter = new Mock<IUsersToNotifyGetter>(MockBehavior.Strict);
            usersToNotifyGetter.Setup(getter => getter.Run(It.IsAny<DateTime?>())).Returns(ImmutableArray<MemCheckUser>.Empty);

            var notifier = new Notifier(userCardSubscriptionCounter.Object, userCardVersionsNotifier.Object, userCardDeletionsNotifier.Object, usersToNotifyGetter.Object, userLastNotifDateUpdater.Object);
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
            var cardVersion = new CardVersion(Guid.NewGuid(), StringServices.RandomString(), StringServices.RandomString(), new DateTime(2029, 12, 15), StringServices.RandomString(), true);
            userCardVersionsNotifier.Setup(notifier => notifier.RunAsync(user, now)).ReturnsAsync(ImmutableArray.Create(cardVersion));

            var userCardDeletionsNotifier = new Mock<IUserCardDeletionsNotifier>(MockBehavior.Strict);
            userCardDeletionsNotifier.Setup(notifier => notifier.RunAsync(user.Id, now)).ReturnsAsync(ImmutableArray<CardDeletion>.Empty);

            var userLastNotifDateUpdater = new Mock<IUserLastNotifDateUpdater>(MockBehavior.Strict);
            userLastNotifDateUpdater.Setup(updater => updater.RunAsync(user.Id, now)).Returns(Task.CompletedTask);

            var notifier = new Notifier(userCardSubscriptionCounter.Object, userCardVersionsNotifier.Object, userCardDeletionsNotifier.Object, usersToNotifyGetter.Object, userLastNotifDateUpdater.Object);
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
            userCardVersionsNotifier.Setup(notifier => notifier.RunAsync(user, now)).ReturnsAsync(ImmutableArray<CardVersion>.Empty);

            var userCardDeletionsNotifier = new Mock<IUserCardDeletionsNotifier>(MockBehavior.Strict);
            var cardDeletion = new CardDeletion(StringServices.RandomString(), StringServices.RandomString(), new DateTime(2029, 12, 15), StringServices.RandomString(), true);
            userCardDeletionsNotifier.Setup(notifier => notifier.RunAsync(user.Id, now)).ReturnsAsync(ImmutableArray.Create(cardDeletion));

            var userLastNotifDateUpdater = new Mock<IUserLastNotifDateUpdater>(MockBehavior.Strict);
            userLastNotifDateUpdater.Setup(updater => updater.RunAsync(user.Id, now)).Returns(Task.CompletedTask); ;

            var notifier = new Notifier(userCardSubscriptionCounter.Object, userCardVersionsNotifier.Object, userCardDeletionsNotifier.Object, usersToNotifyGetter.Object, userLastNotifDateUpdater.Object);
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
        }
    }
}

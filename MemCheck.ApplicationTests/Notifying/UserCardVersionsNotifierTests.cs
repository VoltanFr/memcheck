using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using MemCheck.Database;
using System.Linq;
using System;
using MemCheck.Application.Notifying;
using MemCheck.Domain;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MemCheck.Application.Tests.Notifying
{
    [TestClass()]
    public class UserCardVersionsNotifierTests
    {
        #region Private methods
        private async Task<Card> CreateCardAsync(DbContextOptions<MemCheckDbContext> testDB, Guid versionCreatorId, DateTime versionDate, IEnumerable<Guid>? userWithViewIds = null)
        {
            //userWithViewIds null means public card

            using var dbContext = new MemCheckDbContext(testDB);
            var creator = await dbContext.Users.Where(u => u.Id == versionCreatorId).SingleAsync();

            var result = new Card();
            result.VersionCreator = creator;
            result.FrontSide = StringServices.RandomString();
            result.BackSide = StringServices.RandomString();
            result.AdditionalInfo = StringServices.RandomString();
            result.VersionDescription = StringServices.RandomString();
            result.VersionType = CardVersionType.Creation;
            result.InitialCreationUtcDate = versionDate;
            result.VersionUtcDate = versionDate;
            dbContext.Cards.Add(result);

            var usersWithView = new List<UserWithViewOnCard>();
            if (userWithViewIds != null)
            {
                Assert.IsTrue(userWithViewIds.Any(id => id == versionCreatorId), "Version creator must be allowed to view");
                foreach (var userWithViewId in userWithViewIds)
                {
                    var userWithView = new UserWithViewOnCard();
                    userWithView.CardId = result.Id;
                    userWithView.UserId = userWithViewId;
                    dbContext.UsersWithViewOnCards.Add(userWithView);
                    usersWithView.Add(userWithView);
                }
            }
            result.UsersWithView = usersWithView;

            await dbContext.SaveChangesAsync();
            return result;
        }
        private async Task<CardPreviousVersion> CreateCardPreviousVersionAsync(DbContextOptions<MemCheckDbContext> testDB, Guid versionCreatorId, Guid cardId, DateTime versionDate)
        {
            using var dbContext = new MemCheckDbContext(testDB);
            var creator = await dbContext.Users.Where(u => u.Id == versionCreatorId).SingleAsync();

            var result = new CardPreviousVersion();
            result.Card = cardId;
            result.VersionCreator = creator;
            result.VersionUtcDate = versionDate;
            result.VersionType = CardPreviousVersionType.Creation;
            result.FrontSide = StringServices.RandomString();
            result.BackSide = StringServices.RandomString();
            result.AdditionalInfo = StringServices.RandomString();
            result.VersionDescription = StringServices.RandomString();
            dbContext.CardPreviousVersions.Add(result);

            var card = await dbContext.Cards.Where(c => c.Id == cardId).SingleAsync();
            card.PreviousVersion = result;
            card.VersionType = CardVersionType.Changes;

            await dbContext.SaveChangesAsync();
            return result;
        }
        private async Task<CardPreviousVersion> CreatePreviousVersionPreviousVersionAsync(DbContextOptions<MemCheckDbContext> testDB, Guid versionCreatorId, CardPreviousVersion previousVersion, DateTime versionDate)
        {
            using var dbContext = new MemCheckDbContext(testDB);
            var creator = await dbContext.Users.Where(u => u.Id == versionCreatorId).SingleAsync();

            var result = new CardPreviousVersion();
            result.Card = previousVersion.Card;
            result.VersionCreator = creator;
            result.VersionUtcDate = versionDate;
            result.VersionType = CardPreviousVersionType.Creation;
            result.FrontSide = StringServices.RandomString();
            result.BackSide = StringServices.RandomString();
            result.AdditionalInfo = StringServices.RandomString();
            result.VersionDescription = StringServices.RandomString();
            dbContext.CardPreviousVersions.Add(result);

            previousVersion.PreviousVersion = result;
            previousVersion.VersionType = CardPreviousVersionType.Changes;

            await dbContext.SaveChangesAsync();
            return result;
        }
        private async Task CreateCardNotificationAsync(DbContextOptions<MemCheckDbContext> testDB, Guid subscriberId, Guid cardId, DateTime lastNotificationDate)
        {
            using var dbContext = new MemCheckDbContext(testDB);
            var notif = new CardNotificationSubscription();
            notif.CardId = cardId;
            notif.UserId = subscriberId;
            notif.LastNotificationUtcDate = lastNotificationDate;
            dbContext.CardNotifications.Add(notif);
            await dbContext.SaveChangesAsync();
        }
        #endregion
        [TestMethod()]
        public async Task EmptyDB()
        {
            var testDB = DbServices.GetEmptyTestDB(typeof(UserCardVersionsNotifierTests));
            var user1 = await UserHelper.CreateUserAsync(testDB);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var notifier = new UserCardVersionsNotifier(dbContext);
                var versions = notifier.Run(user1);
                Assert.AreEqual(0, versions.Length);
            }
        }
        [TestMethod()]
        public async Task CardWithoutPreviousVersion_NotToBeNotified()
        {
            var testDB = DbServices.GetEmptyTestDB(typeof(UserCardVersionsNotifierTests));
            var user = await UserHelper.CreateUserAsync(testDB);
            var card = await CreateCardAsync(testDB, user.Id, new DateTime(2020, 11, 2));
            var lastNotificationDate = new DateTime(2020, 11, 3);
            await CreateCardNotificationAsync(testDB, user.Id, card.Id, lastNotificationDate);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var notifier = new UserCardVersionsNotifier(dbContext);
                var versions = notifier.Run(user);
                Assert.AreEqual(0, versions.Length);
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var notif = dbContext.CardNotifications.Single(cn => cn.CardId == card.Id && cn.UserId == user.Id);
                Assert.IsTrue(notif.LastNotificationUtcDate == lastNotificationDate);
            }
        }
        [TestMethod()]
        public async Task CardWithoutPreviousVersion_ToBeNotified()
        {
            var testDB = DbServices.GetEmptyTestDB(typeof(UserCardVersionsNotifierTests));
            var user = await UserHelper.CreateUserAsync(testDB);
            var card = await CreateCardAsync(testDB, user.Id, new DateTime(2020, 11, 2));
            await CreateCardNotificationAsync(testDB, user.Id, card.Id, new DateTime(2020, 11, 1));

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var notifier = new UserCardVersionsNotifier(dbContext);
                var versions = notifier.Run(user);
                Assert.AreEqual(1, versions.Length);
                Assert.AreEqual(card.Id, versions[0].CardId);
                Assert.IsTrue(versions[0].CardIsViewable);
                Assert.AreEqual(card.FrontSide, versions[0].FrontSide);
                Assert.AreEqual(card.VersionDescription, versions[0].VersionDescription);
                await dbContext.SaveChangesAsync();
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var notif = dbContext.CardNotifications.Single(cn => cn.CardId == card.Id && cn.UserId == user.Id);
                Assert.IsTrue(notif.LastNotificationUtcDate > new DateTime(2020, 11, 1));
            }
        }
        [TestMethod()]
        public async Task CardWitOnePreviousVersion_NotToBeNotifiedBecauseOfDate()
        {
            var testDB = DbServices.GetEmptyTestDB(typeof(UserCardVersionsNotifierTests));
            var user1 = await UserHelper.CreateUserAsync(testDB);
            var card = await CreateCardAsync(testDB, user1.Id, new DateTime(2020, 11, 2));
            var user2 = await UserHelper.CreateUserAsync(testDB);
            await CreateCardPreviousVersionAsync(testDB, user2.Id, card.Id, new DateTime(2020, 11, 1));
            var lastNotificationDate = new DateTime(2020, 11, 3);
            await CreateCardNotificationAsync(testDB, user1.Id, card.Id, lastNotificationDate);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var notifier = new UserCardVersionsNotifier(dbContext);
                var versions = notifier.Run(user1);
                Assert.AreEqual(0, versions.Length);
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var notif = dbContext.CardNotifications.Single(cn => cn.CardId == card.Id && cn.UserId == user1.Id);
                Assert.IsTrue(notif.LastNotificationUtcDate == lastNotificationDate);
            }
        }
        [TestMethod()]
        public async Task CardWitOnePreviousVersion_ToBeNotifiedWithoutAccessibility()
        {
            var testDB = DbServices.GetEmptyTestDB(typeof(UserCardVersionsNotifierTests));
            var user1 = await UserHelper.CreateUserAsync(testDB);

            var card = await CreateCardAsync(testDB, user1.Id, new DateTime(2020, 11, 2), new[] { user1.Id });
            await CreateCardPreviousVersionAsync(testDB, user1.Id, card.Id, new DateTime(2020, 11, 1));

            var user2 = await UserHelper.CreateUserAsync(testDB);
            await CreateCardNotificationAsync(testDB, user2.Id, card.Id, new DateTime(2020, 11, 1));
            var now = new DateTime(2020, 11, 2);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var notifier = new UserCardVersionsNotifier(dbContext);

                var user1versions = notifier.Run(user1, now);
                Assert.AreEqual(0, user1versions.Length);

                var user2versions = notifier.Run(user2, now);
                Assert.AreEqual(1, user2versions.Length);
                Assert.AreEqual(card.Id, user2versions[0].CardId);
                Assert.IsFalse(user2versions[0].CardIsViewable);
                Assert.IsNull(user2versions[0].VersionDescription);
                Assert.IsNull(user2versions[0].FrontSide);

                await dbContext.SaveChangesAsync();
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var notif = dbContext.CardNotifications.Single(cn => cn.CardId == card.Id && cn.UserId == user2.Id);
                Assert.AreEqual(now, notif.LastNotificationUtcDate);
            }
        }
        [TestMethod()]
        public async Task CardWitOnePreviousVersion_ToBeNotifiedWithAccessibility()
        {
            var testDB = DbServices.GetEmptyTestDB(typeof(UserCardVersionsNotifierTests));
            var user1 = await UserHelper.CreateUserAsync(testDB);
            var user2 = await UserHelper.CreateUserAsync(testDB);

            var card = await CreateCardAsync(testDB, user1.Id, new DateTime(2020, 11, 2), new Guid[] { user1.Id, user2.Id });
            await CreateCardPreviousVersionAsync(testDB, user1.Id, card.Id, new DateTime(2020, 11, 1));

            await CreateCardNotificationAsync(testDB, user2.Id, card.Id, new DateTime(2020, 11, 1));
            var now = new DateTime(2020, 11, 3);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var notifier = new UserCardVersionsNotifier(dbContext);
                var versions = notifier.Run(user2, now);
                Assert.AreEqual(1, versions.Length);
                Assert.AreEqual(card.Id, versions[0].CardId);
                Assert.IsTrue(versions[0].CardIsViewable);
                await dbContext.SaveChangesAsync();
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var notif = dbContext.CardNotifications.Single(cn => cn.CardId == card.Id && cn.UserId == user2.Id);
                Assert.AreEqual(now, notif.LastNotificationUtcDate);
            }
        }
        [TestMethod()]
        public async Task CardWitOnePreviousVersion_ToBeNotified_LastNotifAfterInitialCreation()
        {
            var testDB = DbServices.GetEmptyTestDB(typeof(UserCardVersionsNotifierTests));
            var user1 = await UserHelper.CreateUserAsync(testDB);
            var card = await CreateCardAsync(testDB, user1.Id, new DateTime(2020, 11, 3));
            var user2 = await UserHelper.CreateUserAsync(testDB);
            await CreateCardPreviousVersionAsync(testDB, user2.Id, card.Id, new DateTime(2020, 11, 1));
            await CreateCardNotificationAsync(testDB, user1.Id, card.Id, new DateTime(2020, 11, 2));
            var now = new DateTime(2020, 11, 4);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var notifier = new UserCardVersionsNotifier(dbContext);
                var versions = notifier.Run(user1, now);
                Assert.AreEqual(1, versions.Length);
                Assert.AreEqual(card.Id, versions[0].CardId);
                Assert.IsTrue(versions[0].CardIsViewable);
                await dbContext.SaveChangesAsync();
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var notif = dbContext.CardNotifications.Single(cn => cn.CardId == card.Id && cn.UserId == user1.Id);
                Assert.AreEqual(now, notif.LastNotificationUtcDate);
            }
        }
        [TestMethod()]
        public async Task CardWitOnePreviousVersion_ToBeNotified_LastNotifBeforeInitialCreation()
        {
            var testDB = DbServices.GetEmptyTestDB(typeof(UserCardVersionsNotifierTests));
            var user1 = await UserHelper.CreateUserAsync(testDB);
            var card = await CreateCardAsync(testDB, user1.Id, new DateTime(2020, 11, 3));
            var user2 = await UserHelper.CreateUserAsync(testDB);
            await CreateCardPreviousVersionAsync(testDB, user2.Id, card.Id, new DateTime(2020, 11, 2));
            await CreateCardNotificationAsync(testDB, user1.Id, card.Id, new DateTime(2020, 11, 1));
            var now = new DateTime(2020, 11, 4);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var notifier = new UserCardVersionsNotifier(dbContext);
                var versions = notifier.Run(user1, now);
                Assert.AreEqual(1, versions.Length);
                Assert.AreEqual(card.Id, versions[0].CardId);
                Assert.IsTrue(versions[0].CardIsViewable);
                await dbContext.SaveChangesAsync();
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var notif = dbContext.CardNotifications.Single(cn => cn.CardId == card.Id && cn.UserId == user1.Id);
                Assert.AreEqual(now, notif.LastNotificationUtcDate);
            }
        }
        [TestMethod()]
        public async Task CardWitPreviousVersions_NotToBeNotified()
        {
            var testDB = DbServices.GetEmptyTestDB(typeof(UserCardVersionsNotifierTests));
            var user1 = await UserHelper.CreateUserAsync(testDB);
            var card = await CreateCardAsync(testDB, user1.Id, new DateTime(2020, 11, 3));
            var user2 = await UserHelper.CreateUserAsync(testDB);
            var previousVersion1 = await CreateCardPreviousVersionAsync(testDB, user2.Id, card.Id, new DateTime(2020, 11, 2));
            await CreatePreviousVersionPreviousVersionAsync(testDB, user2.Id, previousVersion1, new DateTime(2020, 11, 1));
            var lastNotificationDate = new DateTime(2020, 11, 3);
            await CreateCardNotificationAsync(testDB, user1.Id, card.Id, lastNotificationDate);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var notifier = new UserCardVersionsNotifier(dbContext);
                var versions = notifier.Run(user1);
                Assert.AreEqual(0, versions.Length);
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var notif = dbContext.CardNotifications.Single(cn => cn.CardId == card.Id && cn.UserId == user1.Id);
                Assert.AreEqual(lastNotificationDate, notif.LastNotificationUtcDate);
            }
        }
        [TestMethod()]
        public async Task CardWitPreviousVersions_ToBeNotified_LastNotifAfterPreviousVersionCreation()
        {
            var testDB = DbServices.GetEmptyTestDB(typeof(UserCardVersionsNotifierTests));
            var user1 = await UserHelper.CreateUserAsync(testDB);
            var card = await CreateCardAsync(testDB, user1.Id, new DateTime(2020, 11, 5));
            var user2 = await UserHelper.CreateUserAsync(testDB);
            var previousVersion1 = await CreateCardPreviousVersionAsync(testDB, user2.Id, card.Id, new DateTime(2020, 11, 3));
            await CreatePreviousVersionPreviousVersionAsync(testDB, user2.Id, previousVersion1, new DateTime(2020, 11, 1));
            await CreateCardNotificationAsync(testDB, user1.Id, card.Id, new DateTime(2020, 11, 4));
            var now = new DateTime(2020, 11, 5);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var notifier = new UserCardVersionsNotifier(dbContext);
                var versions = notifier.Run(user1, now);
                Assert.AreEqual(1, versions.Length);
                Assert.AreEqual(card.Id, versions[0].CardId);
                Assert.IsTrue(versions[0].CardIsViewable);
                await dbContext.SaveChangesAsync();
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var notif = dbContext.CardNotifications.Single(cn => cn.CardId == card.Id && cn.UserId == user1.Id);
                Assert.AreEqual(now, notif.LastNotificationUtcDate);
            }
        }
        [TestMethod()]
        public async Task CardWitPreviousVersions_ToBeNotified_LastNotifBeforePreviousVersionCreation()
        {
            var testDB = DbServices.GetEmptyTestDB(typeof(UserCardVersionsNotifierTests));
            var user1 = await UserHelper.CreateUserAsync(testDB);
            var card = await CreateCardAsync(testDB, user1.Id, new DateTime(2020, 11, 5));
            var user2 = await UserHelper.CreateUserAsync(testDB);
            var previousVersion1 = await CreateCardPreviousVersionAsync(testDB, user2.Id, card.Id, new DateTime(2020, 11, 3));
            await CreatePreviousVersionPreviousVersionAsync(testDB, user2.Id, previousVersion1, new DateTime(2020, 11, 1));
            await CreateCardNotificationAsync(testDB, user1.Id, card.Id, new DateTime(2020, 11, 2));
            var now = new DateTime(2020, 11, 5);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var notifier = new UserCardVersionsNotifier(dbContext);
                var versions = notifier.Run(user1, now);
                Assert.AreEqual(1, versions.Length);
                Assert.AreEqual(card.Id, versions[0].CardId);
                Assert.IsTrue(versions[0].CardIsViewable);
                await dbContext.SaveChangesAsync();
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var notif = dbContext.CardNotifications.Single(cn => cn.CardId == card.Id && cn.UserId == user1.Id);
                Assert.AreEqual(now, notif.LastNotificationUtcDate);
            }
        }
        [TestMethod()]
        public async Task MultipleVersions()
        {
            var testDB = DbServices.GetEmptyTestDB(typeof(UserCardVersionsNotifierTests));
            var user1 = await UserHelper.CreateUserAsync(testDB);
            var user2 = await UserHelper.CreateUserAsync(testDB);

            var card1 = await CreateCardAsync(testDB, user1.Id, new DateTime(2020, 11, 10));
            var card1PV1 = await CreateCardPreviousVersionAsync(testDB, user2.Id, card1.Id, new DateTime(2020, 11, 5));
            await CreatePreviousVersionPreviousVersionAsync(testDB, user2.Id, card1PV1, new DateTime(2020, 11, 1));
            await CreateCardNotificationAsync(testDB, user1.Id, card1.Id, new DateTime(2020, 11, 2));

            var card2 = await CreateCardAsync(testDB, user1.Id, new DateTime(2020, 11, 10));
            var card2PV1 = await CreateCardPreviousVersionAsync(testDB, user2.Id, card2.Id, new DateTime(2020, 11, 5));
            await CreatePreviousVersionPreviousVersionAsync(testDB, user2.Id, card2PV1, new DateTime(2020, 11, 2));
            await CreateCardNotificationAsync(testDB, user1.Id, card2.Id, new DateTime(2020, 11, 1));

            var card3 = await CreateCardAsync(testDB, user1.Id, new DateTime(2020, 11, 10));
            var card3PV1 = await CreateCardPreviousVersionAsync(testDB, user2.Id, card3.Id, new DateTime(2020, 11, 5));
            await CreatePreviousVersionPreviousVersionAsync(testDB, user2.Id, card3PV1, new DateTime(2020, 11, 2));
            var card3LastNotifDate = new DateTime(2020, 11, 11);
            await CreateCardNotificationAsync(testDB, user1.Id, card3.Id, card3LastNotifDate);

            var card4 = await CreateCardAsync(testDB, user2.Id, new DateTime(2020, 11, 10), new Guid[] { user2.Id });  //Not to be notified because no access for user1
            await CreateCardPreviousVersionAsync(testDB, user2.Id, card4.Id, new DateTime(2020, 11, 5));
            await CreateCardNotificationAsync(testDB, user1.Id, card4.Id, new DateTime(2020, 11, 2));

            var card5 = await CreateCardAsync(testDB, user2.Id, new DateTime(2020, 11, 10), new Guid[] { user1.Id, user2.Id });
            await CreateCardPreviousVersionAsync(testDB, user2.Id, card5.Id, new DateTime(2020, 11, 5));
            await CreateCardNotificationAsync(testDB, user1.Id, card5.Id, new DateTime(2020, 11, 2));

            var now = new DateTime(2020, 11, 5);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var notifier = new UserCardVersionsNotifier(dbContext);
                var user1Versions = notifier.Run(user1, now);
                Assert.AreEqual(4, user1Versions.Length);

                var notifForCard1 = user1Versions.Where(v => v.CardId == card1.Id).Single();
                Assert.IsTrue(notifForCard1.CardIsViewable);

                var notifForCard2 = user1Versions.Where(v => v.CardId == card2.Id).Single();
                Assert.IsTrue(notifForCard2.CardIsViewable);

                Assert.IsFalse(user1Versions.Any(v => v.CardId == card3.Id));

                var notifForCard4 = user1Versions.Where(v => v.CardId == card4.Id).Single();
                Assert.IsFalse(notifForCard4.CardIsViewable);

                var notifForCard5 = user1Versions.Where(v => v.CardId == card5.Id).Single();
                Assert.IsTrue(notifForCard5.CardIsViewable);

                await dbContext.SaveChangesAsync();
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                Assert.AreEqual(now, dbContext.CardNotifications.Single(cn => cn.CardId == card1.Id && cn.UserId == user1.Id).LastNotificationUtcDate);
                Assert.AreEqual(now, dbContext.CardNotifications.Single(cn => cn.CardId == card2.Id && cn.UserId == user1.Id).LastNotificationUtcDate);
                Assert.AreEqual(card3LastNotifDate, dbContext.CardNotifications.Single(cn => cn.CardId == card3.Id && cn.UserId == user1.Id).LastNotificationUtcDate);
                Assert.AreEqual(now, dbContext.CardNotifications.Single(cn => cn.CardId == card4.Id && cn.UserId == user1.Id).LastNotificationUtcDate);
                Assert.AreEqual(now, dbContext.CardNotifications.Single(cn => cn.CardId == card5.Id && cn.UserId == user1.Id).LastNotificationUtcDate);
            }
        }
    }
}
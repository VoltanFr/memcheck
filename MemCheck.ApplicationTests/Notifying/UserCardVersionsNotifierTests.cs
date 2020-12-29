﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using MemCheck.Database;
using System.Linq;
using System;
using MemCheck.Application.Notifying;
using MemCheck.Domain;
using System.Threading.Tasks;
using MemCheck.Application.Tests.Helpers;

namespace MemCheck.Application.Tests.Notifying
{
    [TestClass()]
    public class UserCardVersionsNotifierTests
    {
        #region Private methods
        private async Task<CardPreviousVersion> CreateCardPreviousVersionAsync(DbContextOptions<MemCheckDbContext> testDB, Guid versionCreatorId, Guid cardId, DateTime versionDate)
        {
            using var dbContext = new MemCheckDbContext(testDB);
            var creator = await dbContext.Users.Where(u => u.Id == versionCreatorId).SingleAsync();

            var result = new CardPreviousVersion();
            result.Card = cardId;
            result.VersionCreator = creator;
            result.VersionUtcDate = versionDate;
            result.VersionType = CardPreviousVersionType.Creation;
            result.FrontSide = StringHelper.RandomString();
            result.BackSide = StringHelper.RandomString();
            result.AdditionalInfo = StringHelper.RandomString();
            result.VersionDescription = StringHelper.RandomString();
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
            result.FrontSide = StringHelper.RandomString();
            result.BackSide = StringHelper.RandomString();
            result.AdditionalInfo = StringHelper.RandomString();
            result.VersionDescription = StringHelper.RandomString();
            dbContext.CardPreviousVersions.Add(result);

            previousVersion.PreviousVersion = result;
            previousVersion.VersionType = CardPreviousVersionType.Changes;

            await dbContext.SaveChangesAsync();
            return result;
        }
        #endregion
        [TestMethod()]
        public async Task EmptyDB()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var user1 = await UserHelper.CreateInDbAsync(testDB);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var versions = await new UserCardVersionsNotifier(dbContext, DateTime.UtcNow).RunAsync(user1);
                Assert.AreEqual(0, versions.Length);
            }
        }
        [TestMethod()]
        public async Task CardWithoutPreviousVersion_NotChangedSinceLastNotif()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(testDB);
            var card = await CardHelper.CreateAsync(testDB, user, new DateTime(2020, 11, 2));
            var lastNotificationDate = new DateTime(2020, 11, 3);
            await CardSubscriptionHelper.CreateAsync(testDB, user, card.Id, lastNotificationDate);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var versions = await new UserCardVersionsNotifier(dbContext, new DateTime(2020, 11, 5)).RunAsync(user);
                Assert.AreEqual(0, versions.Length);
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var notif = dbContext.CardNotifications.Single(cn => cn.CardId == card.Id && cn.UserId == user);
                Assert.IsTrue(notif.LastNotificationUtcDate == lastNotificationDate);
            }
        }
        [TestMethod()]
        public async Task CardWithoutPreviousVersion_ToBeNotified()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(testDB);
            var card = await CardHelper.CreateAsync(testDB, user, new DateTime(2020, 11, 2));
            await CardSubscriptionHelper.CreateAsync(testDB, user, card.Id, new DateTime(2020, 11, 1));

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var versions = await new UserCardVersionsNotifier(dbContext, new DateTime(2020, 11, 2)).RunAsync(user);
                Assert.AreEqual(1, versions.Length);
                Assert.AreEqual(card.Id, versions[0].CardId);
                Assert.IsTrue(versions[0].CardIsViewable);
                Assert.AreEqual(card.FrontSide, versions[0].FrontSide);
                Assert.AreEqual(card.VersionDescription, versions[0].VersionDescription);
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var notif = dbContext.CardNotifications.Single(cn => cn.CardId == card.Id && cn.UserId == user);
                Assert.IsTrue(notif.LastNotificationUtcDate > new DateTime(2020, 11, 1));
            }
        }
        [TestMethod()]
        public async Task CardWitOnePreviousVersion_NotToBeNotifiedBecauseOfDate()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var user1 = await UserHelper.CreateInDbAsync(testDB);
            var card = await CardHelper.CreateAsync(testDB, user1, new DateTime(2020, 11, 2));
            var user2 = await UserHelper.CreateInDbAsync(testDB);
            await CreateCardPreviousVersionAsync(testDB, user2, card.Id, new DateTime(2020, 11, 1));
            var lastNotificationDate = new DateTime(2020, 11, 3);
            await CardSubscriptionHelper.CreateAsync(testDB, user1, card.Id, lastNotificationDate);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var versions = await new UserCardVersionsNotifier(dbContext, new DateTime(2020, 11, 5)).RunAsync(user1);
                Assert.AreEqual(0, versions.Length);
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var notif = dbContext.CardNotifications.Single(cn => cn.CardId == card.Id && cn.UserId == user1);
                Assert.IsTrue(notif.LastNotificationUtcDate == lastNotificationDate);
            }
        }
        [TestMethod()]
        public async Task CardWitOnePreviousVersion_ToBeNotifiedWithoutAccessibility()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var user1 = await UserHelper.CreateInDbAsync(testDB);

            var card = await CardHelper.CreateAsync(testDB, user1, new DateTime(2020, 11, 2), new[] { user1 });
            await CreateCardPreviousVersionAsync(testDB, user1, card.Id, new DateTime(2020, 11, 1));

            var user2 = await UserHelper.CreateInDbAsync(testDB);
            await CardSubscriptionHelper.CreateAsync(testDB, user2, card.Id, new DateTime(2020, 11, 1));
            var now = new DateTime(2020, 11, 2);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var notifier = new UserCardVersionsNotifier(dbContext, now);

                var user1versions = await notifier.RunAsync(user1);
                Assert.AreEqual(0, user1versions.Length);

                var user2versions = await notifier.RunAsync(user2);
                Assert.AreEqual(1, user2versions.Length);
                Assert.AreEqual(card.Id, user2versions[0].CardId);
                Assert.IsFalse(user2versions[0].CardIsViewable);
                Assert.IsNull(user2versions[0].VersionDescription);
                Assert.IsNull(user2versions[0].FrontSide);
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var notif = dbContext.CardNotifications.Single(cn => cn.CardId == card.Id && cn.UserId == user2);
                Assert.AreEqual(now, notif.LastNotificationUtcDate);
            }
        }
        [TestMethod()]
        public async Task CardWitOnePreviousVersion_ToBeNotifiedWithAccessibility()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var user1 = await UserHelper.CreateInDbAsync(testDB);
            var user2 = await UserHelper.CreateInDbAsync(testDB);

            var card = await CardHelper.CreateAsync(testDB, user1, new DateTime(2020, 11, 2), new Guid[] { user1, user2 });
            await CreateCardPreviousVersionAsync(testDB, user1, card.Id, new DateTime(2020, 11, 1));

            await CardSubscriptionHelper.CreateAsync(testDB, user2, card.Id, new DateTime(2020, 11, 1));
            var now = new DateTime(2020, 11, 3);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var versions = await new UserCardVersionsNotifier(dbContext, now).RunAsync(user2);
                Assert.AreEqual(1, versions.Length);
                Assert.AreEqual(card.Id, versions[0].CardId);
                Assert.IsTrue(versions[0].CardIsViewable);
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var notif = dbContext.CardNotifications.Single(cn => cn.CardId == card.Id && cn.UserId == user2);
                Assert.AreEqual(now, notif.LastNotificationUtcDate);
            }
        }
        [TestMethod()]
        public async Task CardWitOnePreviousVersion_ToBeNotified_LastNotifAfterInitialCreation()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var user1 = await UserHelper.CreateInDbAsync(testDB);
            var card = await CardHelper.CreateAsync(testDB, user1, new DateTime(2020, 11, 3));
            var user2 = await UserHelper.CreateInDbAsync(testDB);
            await CreateCardPreviousVersionAsync(testDB, user2, card.Id, new DateTime(2020, 11, 1));
            await CardSubscriptionHelper.CreateAsync(testDB, user1, card.Id, new DateTime(2020, 11, 2));
            var now = new DateTime(2020, 11, 4);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var notifier = new UserCardVersionsNotifier(dbContext, now);
                var versions = await notifier.RunAsync(user1);
                Assert.AreEqual(1, versions.Length);
                Assert.AreEqual(card.Id, versions[0].CardId);
                Assert.IsTrue(versions[0].CardIsViewable);
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var notif = dbContext.CardNotifications.Single(cn => cn.CardId == card.Id && cn.UserId == user1);
                Assert.AreEqual(now, notif.LastNotificationUtcDate);
            }
        }
        [TestMethod()]
        public async Task CardWitOnePreviousVersion_ToBeNotified_LastNotifBeforeInitialCreation()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var user1 = await UserHelper.CreateInDbAsync(testDB);
            var card = await CardHelper.CreateAsync(testDB, user1, new DateTime(2020, 11, 3));
            var user2 = await UserHelper.CreateInDbAsync(testDB);
            await CreateCardPreviousVersionAsync(testDB, user2, card.Id, new DateTime(2020, 11, 2));
            await CardSubscriptionHelper.CreateAsync(testDB, user1, card.Id, new DateTime(2020, 11, 1));
            var now = new DateTime(2020, 11, 4);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var notifier = new UserCardVersionsNotifier(dbContext, now);
                var versions = await notifier.RunAsync(user1);
                Assert.AreEqual(1, versions.Length);
                Assert.AreEqual(card.Id, versions[0].CardId);
                Assert.IsTrue(versions[0].CardIsViewable);
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var notif = dbContext.CardNotifications.Single(cn => cn.CardId == card.Id && cn.UserId == user1);
                Assert.AreEqual(now, notif.LastNotificationUtcDate);
            }
        }
        [TestMethod()]
        public async Task CardWitPreviousVersions_NotToBeNotified()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var user1 = await UserHelper.CreateInDbAsync(testDB);
            var card = await CardHelper.CreateAsync(testDB, user1, new DateTime(2020, 11, 3));
            var user2 = await UserHelper.CreateInDbAsync(testDB);
            var previousVersion1 = await CreateCardPreviousVersionAsync(testDB, user2, card.Id, new DateTime(2020, 11, 2));
            await CreatePreviousVersionPreviousVersionAsync(testDB, user2, previousVersion1, new DateTime(2020, 11, 1));
            var lastNotificationDate = new DateTime(2020, 11, 3);
            await CardSubscriptionHelper.CreateAsync(testDB, user1, card.Id, lastNotificationDate);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var versions = await new UserCardVersionsNotifier(dbContext, new DateTime(2020, 11, 5)).RunAsync(user1);
                Assert.AreEqual(0, versions.Length);
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var notif = dbContext.CardNotifications.Single(cn => cn.CardId == card.Id && cn.UserId == user1);
                Assert.AreEqual(lastNotificationDate, notif.LastNotificationUtcDate);
            }
        }
        [TestMethod()]
        public async Task CardWitPreviousVersions_ToBeNotified_LastNotifAfterPreviousVersionCreation()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var user1 = await UserHelper.CreateInDbAsync(testDB);
            var card = await CardHelper.CreateAsync(testDB, user1, new DateTime(2020, 11, 5));
            var user2 = await UserHelper.CreateInDbAsync(testDB);
            var previousVersion1 = await CreateCardPreviousVersionAsync(testDB, user2, card.Id, new DateTime(2020, 11, 3));
            await CreatePreviousVersionPreviousVersionAsync(testDB, user2, previousVersion1, new DateTime(2020, 11, 1));
            await CardSubscriptionHelper.CreateAsync(testDB, user1, card.Id, new DateTime(2020, 11, 4));
            var now = new DateTime(2020, 11, 5);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var versions = await new UserCardVersionsNotifier(dbContext, now).RunAsync(user1);
                Assert.AreEqual(1, versions.Length);
                Assert.AreEqual(card.Id, versions[0].CardId);
                Assert.IsTrue(versions[0].CardIsViewable);
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var notif = dbContext.CardNotifications.Single(cn => cn.CardId == card.Id && cn.UserId == user1);
                Assert.AreEqual(now, notif.LastNotificationUtcDate);
            }
        }
        [TestMethod()]
        public async Task CardWitPreviousVersions_ToBeNotified_LastNotifBeforePreviousVersionCreation()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var user1 = await UserHelper.CreateInDbAsync(testDB);
            var card = await CardHelper.CreateAsync(testDB, user1, new DateTime(2020, 11, 5));
            var user2 = await UserHelper.CreateInDbAsync(testDB);
            var previousVersion1 = await CreateCardPreviousVersionAsync(testDB, user2, card.Id, new DateTime(2020, 11, 3));
            await CreatePreviousVersionPreviousVersionAsync(testDB, user2, previousVersion1, new DateTime(2020, 11, 1));
            await CardSubscriptionHelper.CreateAsync(testDB, user1, card.Id, new DateTime(2020, 11, 2));
            var now = new DateTime(2020, 11, 5);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var versions = await new UserCardVersionsNotifier(dbContext, now).RunAsync(user1);
                Assert.AreEqual(1, versions.Length);
                Assert.AreEqual(card.Id, versions[0].CardId);
                Assert.IsTrue(versions[0].CardIsViewable);
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var notif = dbContext.CardNotifications.Single(cn => cn.CardId == card.Id && cn.UserId == user1);
                Assert.AreEqual(now, notif.LastNotificationUtcDate);
            }
        }
        [TestMethod()]
        public async Task MultipleVersions()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var user1 = await UserHelper.CreateInDbAsync(testDB);
            var user2 = await UserHelper.CreateInDbAsync(testDB);

            var card1 = await CardHelper.CreateAsync(testDB, user1, new DateTime(2020, 11, 10));
            var card1PV1 = await CreateCardPreviousVersionAsync(testDB, user2, card1.Id, new DateTime(2020, 11, 5));
            await CreatePreviousVersionPreviousVersionAsync(testDB, user2, card1PV1, new DateTime(2020, 11, 1));
            await CardSubscriptionHelper.CreateAsync(testDB, user1, card1.Id, new DateTime(2020, 11, 2));

            var card2 = await CardHelper.CreateAsync(testDB, user1, new DateTime(2020, 11, 10));
            var card2PV1 = await CreateCardPreviousVersionAsync(testDB, user2, card2.Id, new DateTime(2020, 11, 5));
            await CreatePreviousVersionPreviousVersionAsync(testDB, user2, card2PV1, new DateTime(2020, 11, 2));
            await CardSubscriptionHelper.CreateAsync(testDB, user1, card2.Id, new DateTime(2020, 11, 1));

            var card3 = await CardHelper.CreateAsync(testDB, user1, new DateTime(2020, 11, 10));
            var card3PV1 = await CreateCardPreviousVersionAsync(testDB, user2, card3.Id, new DateTime(2020, 11, 5));
            await CreatePreviousVersionPreviousVersionAsync(testDB, user2, card3PV1, new DateTime(2020, 11, 2));
            var card3LastNotifDate = new DateTime(2020, 11, 11);
            await CardSubscriptionHelper.CreateAsync(testDB, user1, card3.Id, card3LastNotifDate);

            var card4 = await CardHelper.CreateAsync(testDB, user2, new DateTime(2020, 11, 10), new Guid[] { user2 });  //Not to be notified because no access for user1
            await CreateCardPreviousVersionAsync(testDB, user2, card4.Id, new DateTime(2020, 11, 5));
            await CardSubscriptionHelper.CreateAsync(testDB, user1, card4.Id, new DateTime(2020, 11, 2));

            var card5 = await CardHelper.CreateAsync(testDB, user2, new DateTime(2020, 11, 10), new Guid[] { user1, user2 });
            await CreateCardPreviousVersionAsync(testDB, user2, card5.Id, new DateTime(2020, 11, 5));
            await CardSubscriptionHelper.CreateAsync(testDB, user1, card5.Id, new DateTime(2020, 11, 2));

            var now = new DateTime(2020, 11, 5);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var user1Versions = await new UserCardVersionsNotifier(dbContext, now).RunAsync(user1);
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
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                Assert.AreEqual(now, dbContext.CardNotifications.Single(cn => cn.CardId == card1.Id && cn.UserId == user1).LastNotificationUtcDate);
                Assert.AreEqual(now, dbContext.CardNotifications.Single(cn => cn.CardId == card2.Id && cn.UserId == user1).LastNotificationUtcDate);
                Assert.AreEqual(card3LastNotifDate, dbContext.CardNotifications.Single(cn => cn.CardId == card3.Id && cn.UserId == user1).LastNotificationUtcDate);
                Assert.AreEqual(now, dbContext.CardNotifications.Single(cn => cn.CardId == card4.Id && cn.UserId == user1).LastNotificationUtcDate);
                Assert.AreEqual(now, dbContext.CardNotifications.Single(cn => cn.CardId == card5.Id && cn.UserId == user1).LastNotificationUtcDate);
            }
        }
    }
}
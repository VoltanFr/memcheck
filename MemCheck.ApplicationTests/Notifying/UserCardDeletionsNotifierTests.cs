using MemCheck.Application.Cards;
using MemCheck.Application.Helpers;
using MemCheck.Application.Notifiying;
using MemCheck.Basics;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Notifying
{
    [TestClass()]
    public class UserCardDeletionsNotifierTests
    {
        #region Fields
        private static readonly string DeletionDescription = Guid.NewGuid().ToString();
        #endregion
        #region Private methods
        private static DbContextOptions<MemCheckDbContext> GetEmptyTestDB()
        {
            return DbHelper.GetEmptyTestDB();
        }
        private static async Task<CardPreviousVersion> CreateDeletedCardAsync(DbContextOptions<MemCheckDbContext> testDB, Guid versionCreatorId, DateTime versionDate, IEnumerable<Guid>? userWithViewIds = null)
        {
            //userWithViewIds null means public card

            var cardLanguageId = await CardLanguagHelper.CreateAsync(testDB);

            using var dbContext = new MemCheckDbContext(testDB);
            var creator = await dbContext.Users.Where(u => u.Id == versionCreatorId).SingleAsync();
            var cardLanguage = await dbContext.CardLanguages.SingleAsync(cardLanguage => cardLanguage.Id == cardLanguageId);

            var result = new CardPreviousVersion
            {
                Card = Guid.NewGuid(),
                CardLanguage = cardLanguage,
                VersionCreator = creator,
                FrontSide = RandomHelper.String(),
                BackSide = RandomHelper.String(),
                AdditionalInfo = RandomHelper.String(),
                References = RandomHelper.String(),
                VersionDescription = RandomHelper.String(),
                VersionType = CardPreviousVersionType.Deletion,
                VersionUtcDate = versionDate
            };
            dbContext.CardPreviousVersions.Add(result);

            var usersWithView = new List<UserWithViewOnCardPreviousVersion>();
            if (userWithViewIds != null)
            {
                Assert.IsTrue(userWithViewIds.Any(id => id == versionCreatorId), "Version creator must be allowed to view");
                foreach (var userWithViewId in userWithViewIds)
                {
                    var userWithView = new UserWithViewOnCardPreviousVersion { CardPreviousVersionId = result.Id, AllowedUserId = userWithViewId };
                    dbContext.UsersWithViewOnCardPreviousVersions.Add(userWithView);
                    usersWithView.Add(userWithView);
                }
            }
            result.UsersWithView = usersWithView;

            await dbContext.SaveChangesAsync();
            return result;
        }
        private static async Task DeleteCardAsync(DbContextOptions<MemCheckDbContext> testDB, Guid userId, Guid cardId, DateTime deletionDate)
        {
            using var dbContext = new MemCheckDbContext(testDB);
            var deleter = new DeleteCards(dbContext.AsCallContext(new TestLocalizer(new KeyValuePair<string, string>("Deletion", DeletionDescription).AsArray())), deletionDate);
            var deletionRequest = new DeleteCards.Request(userId, cardId.AsArray());
            await deleter.RunAsync(deletionRequest);
        }
        #endregion
        [TestMethod()]
        public async Task EmptyDB()
        {
            var db = GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            var notifier = new UserCardDeletionsNotifier(dbContext.AsCallContext(), DateTime.UtcNow);
            var versions = await notifier.RunAsync(user);
            Assert.AreEqual(0, versions.Length);
        }
        [TestMethod()]
        public async Task CardWithoutPreviousVersion_NotToBeNotified()
        {
            var db = GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var card = await CardHelper.CreateAsync(db, user, new DateTime(2020, 11, 1));
            await CardSubscriptionHelper.CreateAsync(db, user, card.Id, new DateTime(2020, 11, 3));
            var lastNotificationDate = new DateTime(2020, 11, 3);
            await DeleteCardAsync(db, user, card.Id, lastNotificationDate);

            using (var dbContext = new MemCheckDbContext(db))
            {
                var notifier = new UserCardDeletionsNotifier(dbContext.AsCallContext(), new DateTime(2020, 11, 10));
                var versions = await notifier.RunAsync(user);
                Assert.AreEqual(0, versions.Length);
            }

            using (var dbContext = new MemCheckDbContext(db))
                Assert.IsTrue(dbContext.CardNotifications.Single(cn => cn.CardId == card.Id && cn.UserId == user).LastNotificationUtcDate == lastNotificationDate);
        }
        [TestMethod()]
        public async Task CardWithoutPreviousVersion_ToBeNotifiedWithoutVisibility()
        {
            var db = GetEmptyTestDB();
            var user1 = await UserHelper.CreateInDbAsync(db);
            var deletedCard = await CreateDeletedCardAsync(db, user1, new DateTime(2020, 11, 2), user1.AsArray());

            var user2 = await UserHelper.CreateInDbAsync(db);
            await CardSubscriptionHelper.CreateAsync(db, user2, deletedCard.Card, new DateTime(2020, 11, 1));

            var now = new DateTime(2020, 11, 2);

            using (var dbContext = new MemCheckDbContext(db))
            {
                var notifier = new UserCardDeletionsNotifier(dbContext.AsCallContext(), now);
                var versions = await notifier.RunAsync(user2);
                Assert.AreEqual(1, versions.Length);
                Assert.IsFalse(versions[0].CardIsViewable);
                Assert.IsNull(versions[0].FrontSide);
                Assert.IsNull(versions[0].DeletionDescription);
            }

            using (var dbContext = new MemCheckDbContext(db))
                Assert.AreEqual(now, dbContext.CardNotifications.Single(cn => cn.CardId == deletedCard.Card && cn.UserId == user2).LastNotificationUtcDate);
        }
        [TestMethod()]
        public async Task CardWithoutPreviousVersion_ToBeNotified()
        {
            var db = GetEmptyTestDB();
            var user1 = await UserHelper.CreateInDbAsync(db);
            var user2 = await UserHelper.CreateInDbAsync(db);

            var card = await CardHelper.CreateAsync(db, user1, new DateTime(2020, 11, 1), new[] { user1, user2 });
            await CardSubscriptionHelper.CreateAsync(db, user1, card.Id, new DateTime(2020, 11, 1));
            await CardSubscriptionHelper.CreateAsync(db, user2, card.Id, new DateTime(2020, 11, 1));

            await DeleteCardAsync(db, user1, card.Id, new DateTime(2020, 11, 2));

            var now = new DateTime(2020, 11, 3);

            using (var dbContext = new MemCheckDbContext(db))
            {
                var notifier = new UserCardDeletionsNotifier(dbContext.AsCallContext(), now);

                var user1versions = await notifier.RunAsync(user1);
                Assert.AreEqual(1, user1versions.Length);
                Assert.IsTrue(user1versions[0].CardIsViewable);
                Assert.AreEqual(card.FrontSide, user1versions[0].FrontSide);
                Assert.AreEqual(DeletionDescription, user1versions[0].DeletionDescription);

                var user2versions = await notifier.RunAsync(user2);
                Assert.AreEqual(1, user2versions.Length);
                Assert.IsTrue(user2versions[0].CardIsViewable);
                Assert.AreEqual(card.FrontSide, user2versions[0].FrontSide);
                Assert.AreEqual(DeletionDescription, user2versions[0].DeletionDescription);
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                Assert.AreEqual(now, dbContext.CardNotifications.Single(cn => cn.CardId == card.Id && cn.UserId == user1).LastNotificationUtcDate);
                Assert.AreEqual(now, dbContext.CardNotifications.Single(cn => cn.CardId == card.Id && cn.UserId == user2).LastNotificationUtcDate);
            }
        }
        [TestMethod()]
        public async Task CardWithoutPreviousVersion_ToBeNotified_UsingApplicationDelete()
        {
            var db = GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var card = await CardHelper.CreateAsync(db, user, new DateTime(2020, 11, 1));

            await CardSubscriptionHelper.CreateAsync(db, user, card.Id, new DateTime(2020, 11, 1));

            await DeleteCardAsync(db, user, card.Id, new DateTime(2020, 11, 2));

            var now = new DateTime(2020, 11, 3);

            using (var dbContext = new MemCheckDbContext(db))
            {
                var notifier = new UserCardDeletionsNotifier(dbContext.AsCallContext(), now);

                var deletions = await notifier.RunAsync(user);
                Assert.AreEqual(1, deletions.Length);
                Assert.IsTrue(deletions[0].CardIsViewable);
                Assert.AreEqual(card.FrontSide, deletions[0].FrontSide);
                Assert.AreEqual(DeletionDescription, deletions[0].DeletionDescription);
            }

            using (var dbContext = new MemCheckDbContext(db))
                Assert.AreEqual(now, dbContext.CardNotifications.Single(cn => cn.CardId == card.Id && cn.UserId == user).LastNotificationUtcDate);
        }
    }
}
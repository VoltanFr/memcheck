using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using MemCheck.Database;
using System.Linq;
using System;
using MemCheck.Application.Notifying;
using MemCheck.Domain;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Localization;

namespace MemCheck.Application.Tests.Notifying
{
    [TestClass()]
    public class UserCardDeletionsNotifierTests
    {
        #region Fields
        private static readonly string DeletionDescription = Guid.NewGuid().ToString();
        #endregion
        #region private sealed class EmptyLocalizer
        private sealed class EmptyLocalizer : IStringLocalizer
        {
            public LocalizedString this[string name]
            {
                get
                {
                    if (name == "Deletion")
                        return new LocalizedString(name, DeletionDescription);
                    return new LocalizedString(name, "");
                }
            }
            public LocalizedString this[string name, params object[] arguments] => new LocalizedString(name, "");
            public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            {
                return new LocalizedString[0];
            }
        }
        #endregion
        #region Private methods
        private DbContextOptions<MemCheckDbContext> GetEmptyTestDB()
        {
            return DbServices.GetEmptyTestDB(typeof(UserCardDeletionsNotifierTests));
        }
        private async Task<CardPreviousVersion> CreateDeletedCardAsync(DbContextOptions<MemCheckDbContext> testDB, Guid versionCreatorId, DateTime versionDate, IEnumerable<Guid>? userWithViewIds = null)
        {
            //userWithViewIds null means public card

            using var dbContext = new MemCheckDbContext(testDB);
            var creator = await dbContext.Users.Where(u => u.Id == versionCreatorId).SingleAsync();

            var result = new CardPreviousVersion();
            result.Card = Guid.NewGuid();
            result.VersionCreator = creator;
            result.FrontSide = Guid.NewGuid().ToString();
            result.BackSide = Guid.NewGuid().ToString();
            result.AdditionalInfo = Guid.NewGuid().ToString();
            result.VersionDescription = Guid.NewGuid().ToString();
            result.VersionType = CardPreviousVersionType.Deletion;
            result.VersionUtcDate = versionDate;
            dbContext.CardPreviousVersions.Add(result);

            var usersWithView = new List<UserWithViewOnCardPreviousVersion>();
            if (userWithViewIds != null)
            {
                Assert.IsTrue(userWithViewIds.Any(id => id == versionCreatorId), "Version creator must be allowed to view");
                foreach (var userWithViewId in userWithViewIds)
                {
                    var userWithView = new UserWithViewOnCardPreviousVersion();
                    userWithView.CardPreviousVersionId = result.Id;
                    userWithView.AllowedUserId = userWithViewId;
                    dbContext.UsersWithViewOnCardPreviousVersions.Add(userWithView);
                    usersWithView.Add(userWithView);
                }
            }
            result.UsersWithView = usersWithView;

            await dbContext.SaveChangesAsync();
            return result;
        }
        private async Task DeleteCardAsync(DbContextOptions<MemCheckDbContext> testDB, Guid userId, Guid cardId, DateTime deletionDate)
        {
            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var deleter = new DeleteCards(dbContext, new EmptyLocalizer());
                var deletionRequest = new DeleteCards.Request(dbContext.Users.Where(u => u.Id == userId).Single(), new[] { cardId });
                await deleter.RunAsync(deletionRequest, deletionDate);
            }
        }
        #endregion
        [TestMethod()]
        public async Task EmptyDB()
        {
            var db = GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);

            using (var dbContext = new MemCheckDbContext(db))
            {
                var notifier = new UserCardDeletionsNotifier(dbContext);
                var versions = await notifier.RunAsync(user);
                Assert.AreEqual(0, versions.Length);
            }
        }
        [TestMethod()]
        public async Task CardWithoutPreviousVersion_NotToBeNotified()
        {
            var db = GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var card = await CardHelper.CreateAsync(db, user.Id, new DateTime(2020, 11, 1));
            await CardSubscriptionHelper.CreateAsync(db, user.Id, card.Id, new DateTime(2020, 11, 3));
            var lastNotificationDate = new DateTime(2020, 11, 3);
            await DeleteCardAsync(db, user.Id, card.Id, lastNotificationDate);

            using (var dbContext = new MemCheckDbContext(db))
            {
                var notifier = new UserCardDeletionsNotifier(dbContext);
                var versions = await notifier.RunAsync(user, new DateTime(2020, 11, 10));
                Assert.AreEqual(0, versions.Length);
            }

            using (var dbContext = new MemCheckDbContext(db))
                Assert.IsTrue(dbContext.CardNotifications.Single(cn => cn.CardId == card.Id && cn.UserId == user.Id).LastNotificationUtcDate == lastNotificationDate);
        }
        [TestMethod()]
        public async Task CardWithoutPreviousVersion_ToBeNotifiedWithoutVisibility()
        {
            var db = GetEmptyTestDB();
            var user1 = await UserHelper.CreateInDbAsync(db);
            var deletedCard = await CreateDeletedCardAsync(db, user1.Id, new DateTime(2020, 11, 2), new[] { user1.Id });

            var user2 = await UserHelper.CreateInDbAsync(db);
            await CardSubscriptionHelper.CreateAsync(db, user2.Id, deletedCard.Card, new DateTime(2020, 11, 1));

            var now = new DateTime(2020, 11, 2);

            using (var dbContext = new MemCheckDbContext(db))
            {
                var notifier = new UserCardDeletionsNotifier(dbContext);
                var versions = await notifier.RunAsync(user2, now);
                Assert.AreEqual(1, versions.Length);
                Assert.IsFalse(versions[0].CardIsViewable);
                Assert.IsNull(versions[0].FrontSide);
                Assert.IsNull(versions[0].DeletionDescription);
            }

            using (var dbContext = new MemCheckDbContext(db))
                Assert.AreEqual(now, dbContext.CardNotifications.Single(cn => cn.CardId == deletedCard.Card && cn.UserId == user2.Id).LastNotificationUtcDate);
        }
        [TestMethod()]
        public async Task CardWithoutPreviousVersion_ToBeNotified()
        {
            var db = GetEmptyTestDB();
            var user1 = await UserHelper.CreateInDbAsync(db);
            var user2 = await UserHelper.CreateInDbAsync(db);

            var card = await CardHelper.CreateAsync(db, user1.Id, new DateTime(2020, 11, 1), new[] { user1.Id, user2.Id });
            await CardSubscriptionHelper.CreateAsync(db, user1.Id, card.Id, new DateTime(2020, 11, 1));
            await CardSubscriptionHelper.CreateAsync(db, user2.Id, card.Id, new DateTime(2020, 11, 1));

            await DeleteCardAsync(db, user1.Id, card.Id, new DateTime(2020, 11, 2));

            var now = new DateTime(2020, 11, 3);

            using (var dbContext = new MemCheckDbContext(db))
            {
                var notifier = new UserCardDeletionsNotifier(dbContext);

                var user1versions = await notifier.RunAsync(user1, now);
                Assert.AreEqual(1, user1versions.Length);
                Assert.IsTrue(user1versions[0].CardIsViewable);
                Assert.AreEqual(card.FrontSide, user1versions[0].FrontSide);
                Assert.AreEqual(DeletionDescription, user1versions[0].DeletionDescription);

                var user2versions = await notifier.RunAsync(user2, now);
                Assert.AreEqual(1, user2versions.Length);
                Assert.IsTrue(user2versions[0].CardIsViewable);
                Assert.AreEqual(card.FrontSide, user2versions[0].FrontSide);
                Assert.AreEqual(DeletionDescription, user2versions[0].DeletionDescription);
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                Assert.AreEqual(now, dbContext.CardNotifications.Single(cn => cn.CardId == card.Id && cn.UserId == user1.Id).LastNotificationUtcDate);
                Assert.AreEqual(now, dbContext.CardNotifications.Single(cn => cn.CardId == card.Id && cn.UserId == user2.Id).LastNotificationUtcDate);
            }
        }
        [TestMethod()]
        public async Task CardWithoutPreviousVersion_ToBeNotified_UsingApplicationDelete()
        {
            var db = GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var card = await CardHelper.CreateAsync(db, user.Id, new DateTime(2020, 11, 1));

            await CardSubscriptionHelper.CreateAsync(db, user.Id, card.Id, new DateTime(2020, 11, 1));

            await DeleteCardAsync(db, user.Id, card.Id, new DateTime(2020, 11, 2));

            var now = new DateTime(2020, 11, 3);

            using (var dbContext = new MemCheckDbContext(db))
            {
                var notifier = new UserCardDeletionsNotifier(dbContext);

                var deletions = await notifier.RunAsync(user, now);
                Assert.AreEqual(1, deletions.Length);
                Assert.IsTrue(deletions[0].CardIsViewable);
                Assert.AreEqual(card.FrontSide, deletions[0].FrontSide);
                Assert.AreEqual(DeletionDescription, deletions[0].DeletionDescription);
            }

            using (var dbContext = new MemCheckDbContext(db))
                Assert.AreEqual(now, dbContext.CardNotifications.Single(cn => cn.CardId == card.Id && cn.UserId == user.Id).LastNotificationUtcDate);
        }
    }
}
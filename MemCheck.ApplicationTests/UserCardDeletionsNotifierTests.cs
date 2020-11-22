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

namespace MemCheck.Application.Tests
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
        private DbContextOptions<MemCheckDbContext> OptionsForNewDB()
        {
            return new DbContextOptionsBuilder<MemCheckDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        }
        private async Task<MemCheckUser> CreateUserAsync(DbContextOptions<MemCheckDbContext> db)
        {
            using var dbContext = new MemCheckDbContext(db);
            var result = new MemCheckUser();
            dbContext.Users.Add(result);
            await dbContext.SaveChangesAsync();
            return result;
        }
        private async Task<CardPreviousVersion> CreateDeletedCardAsync(DbContextOptions<MemCheckDbContext> db, Guid versionCreatorId, DateTime versionDate, IEnumerable<Guid>? userWithViewIds = null)
        {
            //userWithViewIds null means public card

            using var dbContext = new MemCheckDbContext(db);
            var creator = await dbContext.Users.Where(u => u.Id == versionCreatorId).SingleAsync();

            var result = new CardPreviousVersion();
            result.Card = Guid.NewGuid();
            result.VersionCreator = creator;
            result.FrontSide = Guid.NewGuid().ToString();
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
        private async Task CreateCardNotificationAsync(DbContextOptions<MemCheckDbContext> db, Guid subscriberId, Guid cardId, DateTime lastNotificationDate)
        {
            using var dbContext = new MemCheckDbContext(db);
            var notif = new CardNotification();
            notif.CardId = cardId;
            notif.UserId = subscriberId;
            notif.LastNotificationUtcDate = lastNotificationDate;
            dbContext.CardNotifications.Add(notif);
            await dbContext.SaveChangesAsync();
        }
        private async Task<Card> CreateCardAsync(DbContextOptions<MemCheckDbContext> db, Guid versionCreatorId, DateTime versionDate, IEnumerable<Guid>? userWithViewIds = null)
        {
            //userWithViewIds null means public card

            using var dbContext = new MemCheckDbContext(db);
            var creator = await dbContext.Users.Where(u => u.Id == versionCreatorId).SingleAsync();

            var result = new Card();
            result.VersionCreator = creator;
            result.FrontSide = Guid.NewGuid().ToString();
            result.VersionDescription = Guid.NewGuid().ToString();
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
        #endregion
        [TestMethod()]
        public async Task EmptyDB()
        {
            var options = OptionsForNewDB();
            var user1 = await CreateUserAsync(options);

            using (var dbContext = new MemCheckDbContext(options))
            {
                var notifier = new UserCardDeletionsNotifier(dbContext);
                var versions = notifier.Run(user1);
                Assert.AreEqual(0, versions.Length);
            }
        }
        [TestMethod()]
        public async Task CardWithoutPreviousVersion_NotToBeNotified()
        {
            var options = OptionsForNewDB();

            var user = await CreateUserAsync(options);
            var deletedCard = await CreateDeletedCardAsync(options, user.Id, new DateTime(2020, 11, 2));
            await CreateCardNotificationAsync(options, user.Id, deletedCard.Id, new DateTime(2020, 11, 3));

            using (var dbContext = new MemCheckDbContext(options))
            {
                var notifier = new UserCardDeletionsNotifier(dbContext);
                var versions = notifier.Run(user);
                Assert.AreEqual(0, versions.Length);
            }
        }
        [TestMethod()]
        public async Task CardWithoutPreviousVersion_ToBeNotifiedWithoutVisibility()
        {
            var options = OptionsForNewDB();

            var user1 = await CreateUserAsync(options);
            var deletedCard = await CreateDeletedCardAsync(options, user1.Id, new DateTime(2020, 11, 2), new[] { user1.Id });

            var user2 = await CreateUserAsync(options);
            await CreateCardNotificationAsync(options, user2.Id, deletedCard.Card, new DateTime(2020, 11, 1));

            using (var dbContext = new MemCheckDbContext(options))
            {
                var notifier = new UserCardDeletionsNotifier(dbContext);
                var versions = notifier.Run(user2);
                Assert.AreEqual(1, versions.Length);
                Assert.IsFalse(versions[0].CardIsViewable);
                Assert.IsNull(versions[0].FrontSide);
                Assert.IsNull(versions[0].DeletionDescription);
            }
        }
        [TestMethod()]
        public async Task CardWithoutPreviousVersion_ToBeNotified()
        {
            var options = OptionsForNewDB();

            var user1 = await CreateUserAsync(options);
            var user2 = await CreateUserAsync(options);
            var deletedCard = await CreateDeletedCardAsync(options, user1.Id, new DateTime(2020, 11, 2), new[] { user1.Id, user2.Id });

            await CreateCardNotificationAsync(options, user1.Id, deletedCard.Card, new DateTime(2020, 11, 1));
            await CreateCardNotificationAsync(options, user2.Id, deletedCard.Card, new DateTime(2020, 11, 1));

            using (var dbContext = new MemCheckDbContext(options))
            {
                var notifier = new UserCardDeletionsNotifier(dbContext);

                var user1versions = notifier.Run(user1);
                Assert.AreEqual(1, user1versions.Length);
                Assert.IsTrue(user1versions[0].CardIsViewable);
                Assert.AreEqual(deletedCard.FrontSide, user1versions[0].FrontSide);
                Assert.AreEqual(deletedCard.VersionDescription, user1versions[0].DeletionDescription);

                var user2versions = notifier.Run(user2);
                Assert.AreEqual(1, user2versions.Length);
                Assert.IsTrue(user2versions[0].CardIsViewable);
                Assert.AreEqual(deletedCard.FrontSide, user2versions[0].FrontSide);
                Assert.AreEqual(deletedCard.VersionDescription, user2versions[0].DeletionDescription);
            }
        }
        [TestMethod()]
        public async Task CardWithoutPreviousVersion_ToBeNotified_UsingApplicationDelete()
        {
            var options = OptionsForNewDB();

            var user1 = await CreateUserAsync(options);
            var card = await CreateCardAsync(options, user1.Id, new DateTime(2020, 11, 1));

            await CreateCardNotificationAsync(options, user1.Id, card.Id, new DateTime(2020, 11, 1));

            using (var dbContext = new MemCheckDbContext(options))
            {
                var deleter = new DeleteCards(dbContext, new EmptyLocalizer());
                var deletionRequest = new DeleteCards.Request(dbContext.Users.Where(u => u.Id == user1.Id).Single(), new[] { card.Id });
                await deleter.RunAsync(deletionRequest);
            }

            using (var dbContext = new MemCheckDbContext(options))
            {
                var notifier = new UserCardDeletionsNotifier(dbContext);

                var deletions = notifier.Run(user1);
                Assert.AreEqual(1, deletions.Length);
                Assert.IsTrue(deletions[0].CardIsViewable);
                Assert.AreEqual(card.FrontSide, deletions[0].FrontSide);
                Assert.AreEqual(DeletionDescription, deletions[0].DeletionDescription);
            }
        }
    }
}
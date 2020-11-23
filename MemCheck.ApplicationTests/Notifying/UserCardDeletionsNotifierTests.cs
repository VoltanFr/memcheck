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
        private static readonly DbContextOptions<MemCheckDbContext> testDB = DbServices.GetEmptyTestDB(typeof(UserCardDeletionsNotifierTests));
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
        private async Task<MemCheckUser> CreateUserAsync()
        {
            using var dbContext = new MemCheckDbContext(testDB);
            var result = new MemCheckUser();
            dbContext.Users.Add(result);
            await dbContext.SaveChangesAsync();
            return result;
        }
        private async Task<CardPreviousVersion> CreateDeletedCardAsync(Guid versionCreatorId, DateTime versionDate, IEnumerable<Guid>? userWithViewIds = null)
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
        private async Task CreateCardNotificationAsync(Guid subscriberId, Guid cardId, DateTime lastNotificationDate)
        {
            using var dbContext = new MemCheckDbContext(testDB);
            var notif = new CardNotificationSubscription();
            notif.CardId = cardId;
            notif.UserId = subscriberId;
            notif.LastNotificationUtcDate = lastNotificationDate;
            dbContext.CardNotifications.Add(notif);
            await dbContext.SaveChangesAsync();
        }
        private async Task<Card> CreateCardAsync(Guid versionCreatorId, DateTime versionDate, IEnumerable<Guid>? userWithViewIds = null)
        {
            //userWithViewIds null means public card

            using var dbContext = new MemCheckDbContext(testDB);
            var creator = await dbContext.Users.Where(u => u.Id == versionCreatorId).SingleAsync();

            var result = new Card();
            result.VersionCreator = creator;
            result.FrontSide = Guid.NewGuid().ToString();
            result.BackSide = Guid.NewGuid().ToString();
            result.AdditionalInfo = Guid.NewGuid().ToString();
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
        private async Task DeleteCardAsync(Guid userId, Guid cardId, DateTime deletionDate)
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
            var user1 = await CreateUserAsync();

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var notifier = new UserCardDeletionsNotifier(dbContext);
                var versions = notifier.Run(user1);
                Assert.AreEqual(0, versions.Length);
            }
        }
        [TestMethod()]
        public async Task CardWithoutPreviousVersion_NotToBeNotified()
        {
            var user = await CreateUserAsync();
            var card = await CreateCardAsync(user.Id, new DateTime(2020, 11, 1));
            await CreateCardNotificationAsync(user.Id, card.Id, new DateTime(2020, 11, 3));
            await DeleteCardAsync(user.Id, card.Id, new DateTime(2020, 11, 2));

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var notifier = new UserCardDeletionsNotifier(dbContext);
                var versions = notifier.Run(user);
                Assert.AreEqual(0, versions.Length);
            }
        }
        [TestMethod()]
        public async Task CardWithoutPreviousVersion_ToBeNotifiedWithoutVisibility()
        {
            var user1 = await CreateUserAsync();
            var deletedCard = await CreateDeletedCardAsync(user1.Id, new DateTime(2020, 11, 2), new[] { user1.Id });

            var user2 = await CreateUserAsync();
            await CreateCardNotificationAsync(user2.Id, deletedCard.Card, new DateTime(2020, 11, 1));

            using (var dbContext = new MemCheckDbContext(testDB))
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
            var user1 = await CreateUserAsync();
            var user2 = await CreateUserAsync();

            var card = await CreateCardAsync(user1.Id, new DateTime(2020, 11, 1), new[] { user1.Id, user2.Id });
            await CreateCardNotificationAsync(user1.Id, card.Id, new DateTime(2020, 11, 1));
            await CreateCardNotificationAsync(user2.Id, card.Id, new DateTime(2020, 11, 1));

            await DeleteCardAsync(user1.Id, card.Id, new DateTime(2020, 11, 2));


            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var notifier = new UserCardDeletionsNotifier(dbContext);

                var user1versions = notifier.Run(user1);
                Assert.AreEqual(1, user1versions.Length);
                Assert.IsTrue(user1versions[0].CardIsViewable);
                Assert.AreEqual(card.FrontSide, user1versions[0].FrontSide);
                Assert.AreEqual(DeletionDescription, user1versions[0].DeletionDescription);

                var user2versions = notifier.Run(user2);
                Assert.AreEqual(1, user2versions.Length);
                Assert.IsTrue(user2versions[0].CardIsViewable);
                Assert.AreEqual(card.FrontSide, user2versions[0].FrontSide);
                Assert.AreEqual(DeletionDescription, user2versions[0].DeletionDescription);
            }
        }
        [TestMethod()]
        public async Task CardWithoutPreviousVersion_ToBeNotified_UsingApplicationDelete()
        {
            var user1 = await CreateUserAsync();
            var card = await CreateCardAsync(user1.Id, new DateTime(2020, 11, 1));

            await CreateCardNotificationAsync(user1.Id, card.Id, new DateTime(2020, 11, 1));

            await DeleteCardAsync(user1.Id, card.Id, new DateTime(2020, 11, 2));

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                Assert.IsTrue(dbContext.CardNotifications.Any(cn => cn.CardId == card.Id)); //DeleteCards must not cascade delete the notifications
            }

            using (var dbContext = new MemCheckDbContext(testDB))
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
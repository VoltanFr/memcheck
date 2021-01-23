using MemCheck.Application.QueryValidation;
using MemCheck.Application.Tests.Helpers;
using MemCheck.Basics;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.CardChanging
{
    [TestClass()]
    public class DeleteCardTests
    {
        [TestMethod()]
        public async Task DeleteCardWhenNotAllowed()
        {
            var db = DbHelper.GetEmptyTestDB();
            var userWithView = await UserHelper.CreateInDbAsync(db);
            var card = await CardHelper.CreateAsync(db, userWithView, new DateTime(2020, 11, 1), userWithViewIds: userWithView.ToEnumerable());
            var otherUser = await UserHelper.CreateInDbAsync(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await CardDeletionHelper.DeleteCardAsync(db, otherUser, card.Id));
        }
        [TestMethod()]
        public async Task DeleteNonExistingCard()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await CardDeletionHelper.DeleteCardAsync(db, user, Guid.NewGuid()));
        }
        [TestMethod()]
        public async Task DeleteDeletedCard()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var card = await CardHelper.CreateAsync(db, user);
            await CardDeletionHelper.DeleteCardAsync(db, user, card.Id);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await CardDeletionHelper.DeleteCardAsync(db, user, card.Id));
        }
        [TestMethod()]
        public async Task DeletingMustNotDeleteCardNotifications()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var card = await CardHelper.CreateAsync(db, user, new DateTime(2020, 11, 1));
            await CardSubscriptionHelper.CreateAsync(db, user, card.Id);

            using (var dbContext = new MemCheckDbContext(db))
                Assert.AreEqual(1, dbContext.CardNotifications.Count());

            await CardDeletionHelper.DeleteCardAsync(db, user, card.Id, new DateTime(2020, 11, 2));

            using (var dbContext = new MemCheckDbContext(db))
                Assert.AreEqual(1, dbContext.CardNotifications.Count());
        }
        [TestMethod()]
        public async Task DeleteSuccessfully()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var language = await CardLanguagHelper.CreateAsync(db);
            var creationDate = DateHelper.Random();
            var card = await CardHelper.CreateAsync(db, user, language: language, versionDate: creationDate);
            var deletionDate = DateHelper.Random();
            await CardDeletionHelper.DeleteCardAsync(db, user, card.Id, deletionDate);

            using var dbContext = new MemCheckDbContext(db);
            Assert.IsFalse(dbContext.Cards.Any());
            Assert.AreEqual(2, dbContext.CardPreviousVersions.Count());

            Assert.AreEqual(1, dbContext.CardPreviousVersions.Count(version => version.Card == card.Id && version.PreviousVersion == null));

            var firstVersion = dbContext.CardPreviousVersions
                .Include(version => version.VersionCreator)
                .Include(version => version.CardLanguage)
                .Include(version => version.Tags)
                .Include(version => version.UsersWithView)
                .Include(version => version.Images)
                .Single(version => version.Card == card.Id && version.PreviousVersion == null);
            Assert.AreEqual(CardPreviousVersionType.Creation, firstVersion.VersionType);
            CardComparisonHelper.AssertSameContent(card, firstVersion, true);
            Assert.AreEqual(creationDate, firstVersion.VersionUtcDate);

            var deletionVersion = dbContext.CardPreviousVersions
                .Include(version => version.VersionCreator)
                .Include(version => version.CardLanguage)
                .Include(version => version.Tags)
                .Include(version => version.UsersWithView)
                .Include(version => version.Images)
                .Single(version => version.Card == card.Id && version.PreviousVersion != null);
            Assert.AreEqual(CardPreviousVersionType.Deletion, deletionVersion.VersionType);
            Assert.AreEqual(firstVersion.Id, deletionVersion.PreviousVersion!.Id);
            CardComparisonHelper.AssertSameContent(card, deletionVersion, true);
            Assert.AreEqual(deletionDate, deletionVersion.VersionUtcDate);
        }
    }
}

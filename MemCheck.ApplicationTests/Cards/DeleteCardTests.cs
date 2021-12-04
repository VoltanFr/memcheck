using MemCheck.Application.QueryValidation;
using MemCheck.Application.Tests.Helpers;
using MemCheck.Application.Users;
using MemCheck.Basics;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards
{
    [TestClass()]
    public class DeleteCardTests
    {
        [TestMethod()]
        public async Task FailIfUserDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var card = await CardHelper.CreateAsync(db, user);
            using var dbContext = new MemCheckDbContext(db);
            var e = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await CardDeletionHelper.DeleteCardAsync(db, Guid.NewGuid(), card.Id));
            Assert.AreEqual("User not found", e.Message);
        }
        [TestMethod()]
        public async Task FailIfNotAllowedToView()
        {
            var db = DbHelper.GetEmptyTestDB();
            var userWithView = await UserHelper.CreateInDbAsync(db);
            var cardCreator = await UserHelper.CreateInDbAsync(db);
            var card = await CardHelper.CreateAsync(db, cardCreator, new DateTime(2020, 11, 1), userWithViewIds: new[] { userWithView, cardCreator });
            using (var dbContext = new MemCheckDbContext(db))
            using (var userManager = UserHelper.GetUserManager(dbContext))
                await new DeleteUserAccount(dbContext.AsCallContext(new TestRoleChecker(userWithView)), userManager).RunAsync(new DeleteUserAccount.Request(userWithView, cardCreator));
            var otherUser = await UserHelper.CreateInDbAsync(db);
            using (var dbContext = new MemCheckDbContext(db))
            {
                var deleter = new DeleteCards(dbContext.AsCallContext(new TestLocalizer(new System.Collections.Generic.KeyValuePair<string, string>("YouAreNotTheCreatorOfCurrentVersion", "YouAreNotTheCreatorOfCurrentVersion"))));
                var e = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await deleter.RunAsync(new DeleteCards.Request(otherUser, card.Id.AsArray())));
                Assert.AreEqual("User not allowed to view card", e.Message);
            }
        }
        [TestMethod()]
        public async Task FailIfDeletedUser()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var card = await CardHelper.CreateAsync(db, user);
            var adminUser = await UserHelper.CreateInDbAsync(db);
            using (var dbContext = new MemCheckDbContext(db))
            using (var userManager = UserHelper.GetUserManager(dbContext))
                await new DeleteUserAccount(dbContext.AsCallContext(new TestRoleChecker(adminUser)), userManager).RunAsync(new DeleteUserAccount.Request(adminUser, user));
            using (var dbContext = new MemCheckDbContext(db))
            {
                var deleter = new DeleteCards(dbContext.AsCallContext());
                var e = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await deleter.RunAsync(new DeleteCards.Request(user, card.Id.AsArray())));
                Assert.AreEqual("User not found", e.Message);
            }
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
            var creationDate = RandomHelper.Date();
            var card = await CardHelper.CreateAsync(db, user, language: language, versionDate: creationDate);
            var deletionDate = RandomHelper.Date();
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
        [TestMethod()]
        public async Task FailIfOtherUserHasCreatedAPreviousVersion()
        {
            var db = DbHelper.GetEmptyTestDB();
            var firstVersionCreator = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, firstVersionCreator, language: languageId);
            var lastVersionCreator = await UserHelper.CreateInDbAsync(db);
            using (var dbContext = new MemCheckDbContext(db))
                await new UpdateCard(dbContext.AsCallContext()).RunAsync(UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String(), lastVersionCreator));
            using (var dbContext = new MemCheckDbContext(db))
            {
                var deleter = new DeleteCards(dbContext.AsCallContext(new TestLocalizer(new System.Collections.Generic.KeyValuePair<string, string>("YouAreNotTheCreatorOfAllPreviousVersions", "YouAreNotTheCreatorOfAllPreviousVersions"))));
                var e = await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await deleter.RunAsync(new DeleteCards.Request(lastVersionCreator, card.Id.AsArray())));
                StringAssert.Contains(e.Message, "YouAreNotTheCreatorOfAllPreviousVersions");
            }
            using (var dbContext = new MemCheckDbContext(db))
            {
                Assert.AreEqual(card.Id, dbContext.Cards.Single().Id);
                Assert.AreEqual(card.Id, dbContext.CardPreviousVersions.Single().Card);
            }
        }
        [TestMethod()]
        public async Task SucceedsIfDeletedUserHasCreatedAPreviousVersion()
        {
            var db = DbHelper.GetEmptyTestDB();
            var firstVersionCreator = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, firstVersionCreator, language: languageId);
            var lastVersionCreator = await UserHelper.CreateInDbAsync(db);
            using (var dbContext = new MemCheckDbContext(db))
                await new UpdateCard(dbContext.AsCallContext()).RunAsync(UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String(), lastVersionCreator));
            using (var dbContext = new MemCheckDbContext(db))
            using (var userManager = UserHelper.GetUserManager(dbContext))
                await new DeleteUserAccount(dbContext.AsCallContext(new TestRoleChecker(lastVersionCreator)), userManager).RunAsync(new DeleteUserAccount.Request(lastVersionCreator, firstVersionCreator));
            using (var dbContext = new MemCheckDbContext(db))
                await new DeleteCards(dbContext.AsCallContext()).RunAsync(new DeleteCards.Request(lastVersionCreator, card.Id.AsArray()));
            using (var dbContext = new MemCheckDbContext(db))
            {
                Assert.IsFalse(dbContext.Cards.Any());
                Assert.AreEqual(3, dbContext.CardPreviousVersions.Count());
            }
        }
        [TestMethod()]
        public async Task FailIfNotCreatorOfCurrentVersion()
        {
            var db = DbHelper.GetEmptyTestDB();
            var firstVersionCreator = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, firstVersionCreator, language: languageId);
            var lastVersionCreator = await UserHelper.CreateInDbAsync(db);
            using (var dbContext = new MemCheckDbContext(db))
                await new UpdateCard(dbContext.AsCallContext()).RunAsync(UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String(), lastVersionCreator));
            using (var dbContext = new MemCheckDbContext(db))
            {
                var deleter = new DeleteCards(dbContext.AsCallContext(new TestLocalizer(new System.Collections.Generic.KeyValuePair<string, string>("YouAreNotTheCreatorOfCurrentVersion", "YouAreNotTheCreatorOfCurrentVersion"))));
                var e = await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await deleter.RunAsync(new DeleteCards.Request(firstVersionCreator, card.Id.AsArray())));
                StringAssert.Contains(e.Message, "YouAreNotTheCreatorOfCurrentVersion");
            }
            using (var dbContext = new MemCheckDbContext(db))
            {
                Assert.AreEqual(card.Id, dbContext.Cards.Single().Id);
                Assert.AreEqual(card.Id, dbContext.CardPreviousVersions.Single().Card);
            }
        }
        [TestMethod()]
        public async Task SucceedsIfCreatorOfCurrentVersionIsDeleted()
        {
            var db = DbHelper.GetEmptyTestDB();
            var firstVersionCreator = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, firstVersionCreator, language: languageId);
            var lastVersionCreator = await UserHelper.CreateInDbAsync(db);
            using (var dbContext = new MemCheckDbContext(db))
                await new UpdateCard(dbContext.AsCallContext()).RunAsync(UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String(), lastVersionCreator));
            await UserHelper.DeleteAsync(db, lastVersionCreator);
            using (var dbContext = new MemCheckDbContext(db))
                await new DeleteCards(dbContext.AsCallContext()).RunAsync(new DeleteCards.Request(firstVersionCreator, card.Id.AsArray()));
            using (var dbContext = new MemCheckDbContext(db))
            {
                Assert.IsFalse(dbContext.Cards.Any());
                Assert.AreEqual(3, dbContext.CardPreviousVersions.Count());
            }
        }
    }
}

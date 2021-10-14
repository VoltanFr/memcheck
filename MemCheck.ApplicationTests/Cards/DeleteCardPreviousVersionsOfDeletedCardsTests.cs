using MemCheck.Application.Tests.Helpers;
using MemCheck.Basics;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards
{
    [TestClass()]
    public class DeleteCardPreviousVersionsOfDeletedCardsTests
    {
        [TestMethod()]
        public async Task FailIfUserDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var request = new DeleteCardPreviousVersionsOfDeletedCards.Request(Guid.NewGuid(), DateTime.MaxValue);
            using var dbContext = new MemCheckDbContext(db);
            var e = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new DeleteCardPreviousVersionsOfDeletedCards(dbContext, new TestRoleChecker()).RunAsync(request));
            Assert.AreEqual("User not found", e.Message);
        }
        [TestMethod()]
        public async Task FailIfUserNotAdmin()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var request = new DeleteCardPreviousVersionsOfDeletedCards.Request(user, DateTime.MaxValue);
            using var dbContext = new MemCheckDbContext(db);
            var e = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new DeleteCardPreviousVersionsOfDeletedCards(dbContext, new TestRoleChecker()).RunAsync(request));
            Assert.AreEqual("User not admin", e.Message);
        }
        [TestMethod()]
        public async Task FailIfUserDeleted()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            await UserHelper.DeleteAsync(db, user);
            var request = new DeleteCardPreviousVersionsOfDeletedCards.Request(user, DateTime.MaxValue);
            using var dbContext = new MemCheckDbContext(db);
            var e = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new DeleteCardPreviousVersionsOfDeletedCards(dbContext, new TestRoleChecker(user)).RunAsync(request));
            Assert.AreEqual("User not found", e.Message);
        }
        [TestMethod()]
        public async Task NoPreviousVersions()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var roleChecker = new TestRoleChecker(user);
            var request = new DeleteCardPreviousVersionsOfDeletedCards.Request(user, DateTime.MaxValue);
            using var dbContext = new MemCheckDbContext(db);
            await new DeleteCardPreviousVersionsOfDeletedCards(dbContext, roleChecker).RunAsync(request);
        }
        [TestMethod()]
        public async Task CardWithPreviousVersionsNotDeleted()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, user, language: languageId);
            await UpdateCardHelper.RunAsync(db, UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String()));
            using (var dbContext = new MemCheckDbContext(db))
                Assert.AreEqual(1, dbContext.CardPreviousVersions.Count());
            using (var dbContext = new MemCheckDbContext(db))
                await new DeleteCardPreviousVersionsOfDeletedCards(dbContext, new TestRoleChecker(user)).RunAsync(new DeleteCardPreviousVersionsOfDeletedCards.Request(user, DateTime.MaxValue));
            using (var dbContext = new MemCheckDbContext(db))
                Assert.AreEqual(1, dbContext.CardPreviousVersions.Count());
        }
        [TestMethod()]
        public async Task CardDeletedButAfterDate()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, user, language: languageId);
            var runDate = RandomHelper.Date();
            using (var dbContext = new MemCheckDbContext(db))
            {
                var deleter = new DeleteCards(FakeMemCheckTelemetryClient.InCallContext(dbContext));
                await deleter.RunAsync(new DeleteCards.Request(user, card.Id.AsArray()), RandomHelper.Date(runDate));
            }
            using (var dbContext = new MemCheckDbContext(db))
                Assert.AreEqual(2, dbContext.CardPreviousVersions.Count());
            using (var dbContext = new MemCheckDbContext(db))
                await new DeleteCardPreviousVersionsOfDeletedCards(dbContext, new TestRoleChecker(user)).RunAsync(new DeleteCardPreviousVersionsOfDeletedCards.Request(user, runDate));
            using (var dbContext = new MemCheckDbContext(db))
                Assert.AreEqual(2, dbContext.CardPreviousVersions.Count());
        }
        [TestMethod()]
        public async Task CardToBeDeletedFromPreviousVersions()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, user, language: languageId);
            var deletionDate = RandomHelper.Date();
            using (var dbContext = new MemCheckDbContext(db))
            {
                var deleter = new DeleteCards(FakeMemCheckTelemetryClient.InCallContext(dbContext));
                await deleter.RunAsync(new DeleteCards.Request(user, card.Id.AsArray()), deletionDate);
            }
            using (var dbContext = new MemCheckDbContext(db))
                Assert.AreEqual(2, dbContext.CardPreviousVersions.Count());
            using (var dbContext = new MemCheckDbContext(db))
                await new DeleteCardPreviousVersionsOfDeletedCards(dbContext, new TestRoleChecker(user)).RunAsync(new DeleteCardPreviousVersionsOfDeletedCards.Request(user, RandomHelper.Date(deletionDate)));
            using (var dbContext = new MemCheckDbContext(db))
                Assert.AreEqual(0, dbContext.CardPreviousVersions.Count());
        }
        [TestMethod()]
        public async Task CardWithIntermediaryVersionsToBeDeletedFromPreviousVersions()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, user, language: languageId);
            await UpdateCardHelper.RunAsync(db, UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String()));
            var runDate = RandomHelper.Date();
            using (var dbContext = new MemCheckDbContext(db))
            {
                var deleter = new DeleteCards(FakeMemCheckTelemetryClient.InCallContext(dbContext));
                await deleter.RunAsync(new DeleteCards.Request(user, card.Id.AsArray()), RandomHelper.DateBefore(runDate));
            }
            using (var dbContext = new MemCheckDbContext(db))
                Assert.AreEqual(3, dbContext.CardPreviousVersions.Count());
            using (var dbContext = new MemCheckDbContext(db))
                await new DeleteCardPreviousVersionsOfDeletedCards(dbContext, new TestRoleChecker(user)).RunAsync(new DeleteCardPreviousVersionsOfDeletedCards.Request(user, runDate));
            using (var dbContext = new MemCheckDbContext(db))
                Assert.AreEqual(0, dbContext.CardPreviousVersions.Count());
        }
        [TestMethod()]
        public async Task MultipleCase()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var runDate = RandomHelper.Date();

            var cardNotDeleted = await CardHelper.CreateAsync(db, user, language: languageId);
            await UpdateCardHelper.RunAsync(db, UpdateCardHelper.RequestForFrontSideChange(cardNotDeleted, RandomHelper.String()));

            using (var dbContext = new MemCheckDbContext(db))
                Assert.AreEqual(1, dbContext.CardPreviousVersions.Count());

            var cardDeletedAfterRunDate = await CardHelper.CreateAsync(db, user, language: languageId);
            using (var dbContext = new MemCheckDbContext(db))
            {
                var deleter = new DeleteCards(FakeMemCheckTelemetryClient.InCallContext(dbContext));
                await deleter.RunAsync(new DeleteCards.Request(user, cardDeletedAfterRunDate.Id.AsArray()), RandomHelper.Date(runDate));
            }

            using (var dbContext = new MemCheckDbContext(db))
                Assert.AreEqual(3, dbContext.CardPreviousVersions.Count());

            var cardDeletedBeforeRunDate = await CardHelper.CreateAsync(db, user, language: languageId);
            using (var dbContext = new MemCheckDbContext(db))
            {
                var deleter = new DeleteCards(FakeMemCheckTelemetryClient.InCallContext(dbContext));
                await deleter.RunAsync(new DeleteCards.Request(user, cardDeletedBeforeRunDate.Id.AsArray()), RandomHelper.DateBefore(runDate));
            }

            using (var dbContext = new MemCheckDbContext(db))
                Assert.AreEqual(5, dbContext.CardPreviousVersions.Count());

            using (var dbContext = new MemCheckDbContext(db))
                await new DeleteCardPreviousVersionsOfDeletedCards(dbContext, new TestRoleChecker(user)).RunAsync(new DeleteCardPreviousVersionsOfDeletedCards.Request(user, runDate));

            using (var dbContext = new MemCheckDbContext(db))
            {
                var previousVersions = await dbContext.CardPreviousVersions.ToListAsync();
                Assert.AreEqual(3, previousVersions.Count);
                Assert.AreEqual(1, previousVersions.Where(pv => pv.Card == cardNotDeleted.Id).Count());
                Assert.AreEqual(2, previousVersions.Where(pv => pv.Card == cardDeletedAfterRunDate.Id).Count());
            }
        }
        [TestMethod()]
        public async Task CascadeDeletionOfTagInPreviousCardVersion()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var tag1 = await TagHelper.CreateAsync(db);
            var tag2 = await TagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, user, language: languageId, tagIds: new[] { tag1, tag2 });
            var tag3 = await TagHelper.CreateAsync(db);
            await UpdateCardHelper.RunAsync(db, UpdateCardHelper.RequestForTagChange(card, tag3.AsArray()));
            var deletionDate = RandomHelper.Date();
            using (var dbContext = new MemCheckDbContext(db))
            {
                var deleter = new DeleteCards(FakeMemCheckTelemetryClient.InCallContext(dbContext));
                await deleter.RunAsync(new DeleteCards.Request(user, card.Id.AsArray()), deletionDate);
            }
            using (var dbContext = new MemCheckDbContext(db))
            {
                Assert.AreEqual(3, dbContext.CardPreviousVersions.Count());
                Assert.AreEqual(4, dbContext.TagInPreviousCardVersions.Count());
            }
            using (var dbContext = new MemCheckDbContext(db))
                await new DeleteCardPreviousVersionsOfDeletedCards(dbContext, new TestRoleChecker(user)).RunAsync(new DeleteCardPreviousVersionsOfDeletedCards.Request(user, RandomHelper.Date(deletionDate)));
            using (var dbContext = new MemCheckDbContext(db))
            {
                Assert.AreEqual(0, dbContext.CardPreviousVersions.Count());
                Assert.AreEqual(0, dbContext.TagInPreviousCardVersions.Count());
            }
        }
        [TestMethod()]
        public async Task CascadeDeletionOfUserWithViewOnCardPreviousVersion()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var otherUser = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, user, language: languageId, userWithViewIds: user.AsArray());
            await UpdateCardHelper.RunAsync(db, UpdateCardHelper.RequestForVisibilityChange(card, new[] { user, otherUser }));
            var deletionDate = RandomHelper.Date();
            using (var dbContext = new MemCheckDbContext(db))
            {
                var deleter = new DeleteCards(FakeMemCheckTelemetryClient.InCallContext(dbContext));
                await deleter.RunAsync(new DeleteCards.Request(user, card.Id.AsArray()), deletionDate);
            }
            using (var dbContext = new MemCheckDbContext(db))
            {
                Assert.AreEqual(3, dbContext.CardPreviousVersions.Count());
                Assert.AreEqual(5, dbContext.UsersWithViewOnCardPreviousVersions.Count());
            }
            using (var dbContext = new MemCheckDbContext(db))
                await new DeleteCardPreviousVersionsOfDeletedCards(dbContext, new TestRoleChecker(user)).RunAsync(new DeleteCardPreviousVersionsOfDeletedCards.Request(user, RandomHelper.Date(deletionDate)));
            using (var dbContext = new MemCheckDbContext(db))
            {
                Assert.AreEqual(0, dbContext.CardPreviousVersions.Count());
                Assert.AreEqual(0, dbContext.UsersWithViewOnCardPreviousVersions.Count());
            }
        }
        [TestMethod()]
        public async Task CascadeDeletionOfImageInCardPreviousVersion()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var image1 = await ImageHelper.CreateAsync(db, user);
            var image2 = await ImageHelper.CreateAsync(db, user);
            var card = await CardHelper.CreateAsync(db, user, language: languageId, frontSideImages: image1.AsArray());
            await UpdateCardHelper.RunAsync(db, UpdateCardHelper.RequestForImageChange(card, Array.Empty<Guid>(), image1.AsArray(), image2.AsArray()));
            var deletionDate = RandomHelper.Date();
            using (var dbContext = new MemCheckDbContext(db))
            {
                var deleter = new DeleteCards(FakeMemCheckTelemetryClient.InCallContext(dbContext));
                await deleter.RunAsync(new DeleteCards.Request(user, card.Id.AsArray()), deletionDate);
            }
            using (var dbContext = new MemCheckDbContext(db))
            {
                Assert.AreEqual(3, dbContext.CardPreviousVersions.Count());
                Assert.AreEqual(5, dbContext.ImagesInCardPreviousVersions.Count());
            }
            using (var dbContext = new MemCheckDbContext(db))
                await new DeleteCardPreviousVersionsOfDeletedCards(dbContext, new TestRoleChecker(user)).RunAsync(new DeleteCardPreviousVersionsOfDeletedCards.Request(user, RandomHelper.Date(deletionDate)));
            using (var dbContext = new MemCheckDbContext(db))
            {
                Assert.AreEqual(0, dbContext.CardPreviousVersions.Count());
                Assert.AreEqual(0, dbContext.ImagesInCardPreviousVersions.Count());
            }
        }
    }
}

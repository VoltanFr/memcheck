using Microsoft.VisualStudio.TestTools.UnitTesting;
using MemCheck.Database;
using System.Threading.Tasks;
using MemCheck.Application.Tests.Helpers;
using System;
using MemCheck.Application.QueryValidation;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MemCheck.Domain;

namespace MemCheck.Application.CardChanging
{
    [TestClass()]
    public class UpdateCardTests
    {
        [TestMethod()]
        public async Task UserNotLoggedIn()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, user, language: languageId);

            using var dbContext = new MemCheckDbContext(db);
            var request = UpdateCardHelper.RequestForFrontSideChanges(card, StringHelper.RandomString(), Guid.Empty);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new UpdateCard(dbContext).RunAsync(request, new TestLocalizer()));
        }
        [TestMethod()]
        public async Task UserDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, user, language: languageId);

            using var dbContext = new MemCheckDbContext(db);
            var r = UpdateCardHelper.RequestForFrontSideChanges(card, StringHelper.RandomString(), Guid.NewGuid());
            await Assert.ThrowsExceptionAsync<ApplicationException>(async () => await new UpdateCard(dbContext).RunAsync(r, new TestLocalizer()));
        }
        [TestMethod()]
        public async Task CardDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            var r = new UpdateCard.Request(Guid.NewGuid(), user, StringHelper.RandomString(), new Guid[0], StringHelper.RandomString(), new Guid[0], StringHelper.RandomString(), new Guid[0], Guid.NewGuid(), new Guid[0], new Guid[0], StringHelper.RandomString());
            await Assert.ThrowsExceptionAsync<ApplicationException>(async () => await new UpdateCard(dbContext).RunAsync(r, new TestLocalizer()));
        }
        [TestMethod()]
        public async Task UserNotAllowedToViewCard()
        {
            var db = DbHelper.GetEmptyTestDB();
            var cardCreator = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, cardCreator, language: languageId, userWithViewIds: new[] { cardCreator });
            var otherUser = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            var r = UpdateCardHelper.RequestForFrontSideChanges(card, StringHelper.RandomString(), otherUser);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new UpdateCard(dbContext).RunAsync(r, new TestLocalizer()));
        }
        [TestMethod()]
        public async Task PublicCard()
        {
            var db = DbHelper.GetEmptyTestDB();
            var cardCreator = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, cardCreator, language: languageId, userWithViewIds: new Guid[0]);
            var otherUser = await UserHelper.CreateInDbAsync(db);
            var newFrontSide = StringHelper.RandomString();

            using (var dbContext = new MemCheckDbContext(db))
            {
                var r = UpdateCardHelper.RequestForFrontSideChanges(card, newFrontSide, otherUser);
                await new UpdateCard(dbContext).RunAsync(r, new TestLocalizer());
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var cardFromDb = dbContext.Cards.Single(c => c.Id == card.Id);
                Assert.AreEqual(newFrontSide, cardFromDb.FrontSide);
            }
        }
        [TestMethod()]
        public async Task UserNotInNewVisibilityList()
        {
            var db = DbHelper.GetEmptyTestDB();
            var cardCreator = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, cardCreator, language: languageId, userWithViewIds: new Guid[0]);
            var newVersionCreator = await UserHelper.CreateInDbAsync(db);

            var r = UpdateCardHelper.RequestForVisibilityChanges(card, new[] { cardCreator }) with { VersionCreatorId = newVersionCreator };

            using (var dbContext = new MemCheckDbContext(db))
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new UpdateCard(dbContext).RunAsync(r, new TestLocalizer()));
        }
        [TestMethod()]
        public async Task UserInVisibilityList()
        {
            var db = DbHelper.GetEmptyTestDB();
            var cardCreator = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var otherUser = await UserHelper.CreateInDbAsync(db);
            var card = await CardHelper.CreateAsync(db, cardCreator, language: languageId, userWithViewIds: new[] { cardCreator, otherUser });
            var newFrontSide = StringHelper.RandomString();

            using (var dbContext = new MemCheckDbContext(db))
            {
                var r = UpdateCardHelper.RequestForFrontSideChanges(card, newFrontSide, otherUser);
                await new UpdateCard(dbContext).RunAsync(r, new TestLocalizer());
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var cardFromDb = dbContext.Cards.Single(c => c.Id == card.Id);
                Assert.AreEqual(newFrontSide, cardFromDb.FrontSide);
            }
        }
        [TestMethod()]
        public async Task UpdateAllFields()
        {
            var db = DbHelper.GetEmptyTestDB();
            var cardCreator = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var originalCard = await CardHelper.CreateAsync(db, cardCreator, language: languageId, userWithViewIds: new Guid[0]);

            var newVersionCreator = await UserHelper.CreateInDbAsync(db);
            var frontSide = StringHelper.RandomString();
            var backSide = StringHelper.RandomString();
            var additionalInfo = StringHelper.RandomString();
            var versionDescription = StringHelper.RandomString();
            var newLanguageId = await CardLanguagHelper.CreateAsync(db);
            var imageOnFrontSideId = await ImageHelper.CreateAsync(db);
            var imageOnBackSide1Id = await ImageHelper.CreateAsync(db);
            var imageOnBackSide2Id = await ImageHelper.CreateAsync(db);
            var imageOnAdditionalInfoId = await ImageHelper.CreateAsync(db);
            var tagId = await TagHelper.CreateAsync(db);

            using (var dbContext = new MemCheckDbContext(db))
            {
                var request = new UpdateCard.Request(
                    originalCard.Id,
                    newVersionCreator,
                    frontSide,
                    new Guid[] { imageOnFrontSideId },
                    backSide,
                    new Guid[] { imageOnBackSide1Id, imageOnBackSide2Id },
                    additionalInfo,
                    new Guid[] { imageOnAdditionalInfoId },
                    languageId,
                    new Guid[] { tagId },
                    new Guid[] { newVersionCreator },
                    versionDescription);
                await new UpdateCard(dbContext).RunAsync(request, new TestLocalizer());
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var updatedCard = dbContext.Cards
                    .Include(c => c.VersionCreator)
                    .Include(c => c.CardLanguage)
                    .Include(c => c.UsersWithView)
                    .Include(c => c.Images)
                    .Include(c => c.TagsInCards)
                    .Single(c => c.Id == originalCard.Id);
                Assert.AreEqual(newVersionCreator, updatedCard.VersionCreator.Id);
                Assert.AreEqual(frontSide, updatedCard.FrontSide);
                Assert.AreEqual(backSide, updatedCard.BackSide);
                Assert.AreEqual(additionalInfo, updatedCard.AdditionalInfo);
                Assert.AreEqual(versionDescription, updatedCard.VersionDescription);
                Assert.AreEqual(languageId, updatedCard.CardLanguage.Id);
                Assert.AreEqual(newVersionCreator, updatedCard.UsersWithView.Single().UserId);
                Assert.AreEqual(ImageInCard.FrontSide, updatedCard.Images.Single(i => i.ImageId == imageOnFrontSideId).CardSide);
                Assert.AreEqual(ImageInCard.BackSide, updatedCard.Images.Single(i => i.ImageId == imageOnBackSide1Id).CardSide);
                Assert.AreEqual(ImageInCard.BackSide, updatedCard.Images.Single(i => i.ImageId == imageOnBackSide2Id).CardSide);
                Assert.AreEqual(ImageInCard.AdditionalInfo, updatedCard.Images.Single(i => i.ImageId == imageOnAdditionalInfoId).CardSide);
                Assert.IsTrue(updatedCard.TagsInCards.Any(t => t.TagId == tagId));
            }
        }
        [TestMethod()]
        public async Task UserSubscribingToCardOnEdit()
        {
            var db = DbHelper.GetEmptyTestDB();

            var cardCreator = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, cardCreator, language: languageId);

            var newVersionCreator = await UserHelper.CreateInDbAsync(db, subscribeToCardOnEdit: true);

            using (var dbContext = new MemCheckDbContext(db))
            {
                var r = UpdateCardHelper.RequestForFrontSideChanges(card, StringHelper.RandomString(), newVersionCreator);
                await new UpdateCard(dbContext).RunAsync(r, new TestLocalizer());
            }

            Assert.IsTrue(await CardSubscriptionHelper.UserIsSubscribedToCardAsync(db, newVersionCreator, card.Id));
        }
        [TestMethod()]
        public async Task UserNotSubscribingToCardOnEdit()
        {
            var db = DbHelper.GetEmptyTestDB();

            var cardCreator = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, cardCreator, language: languageId);

            var newVersionCreator = await UserHelper.CreateInDbAsync(db, subscribeToCardOnEdit: false);

            using (var dbContext = new MemCheckDbContext(db))
            {
                var r = UpdateCardHelper.RequestForFrontSideChanges(card, StringHelper.RandomString(), newVersionCreator);
                await new UpdateCard(dbContext).RunAsync(r, new TestLocalizer());
            }

            Assert.IsFalse(await CardSubscriptionHelper.UserIsSubscribedToCardAsync(db, newVersionCreator, card.Id));
        }

    }
}

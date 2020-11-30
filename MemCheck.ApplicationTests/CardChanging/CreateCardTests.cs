using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using MemCheck.Application.Tests.Notifying;
using MemCheck.Database;
using MemCheck.Application.Tests;
using MemCheck.Application.Tests.BasicHelpers;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace MemCheck.Application.CardChanging
{
    [TestClass()]
    public class CreateCardTests
    {
        [TestMethod()]
        public async Task TestCreationWithAllData()
        {
            var testDB = DbServices.GetEmptyTestDB(typeof(CreateCardTests));

            var userWithViewId = await UserHelper.CreateInDbAsync(testDB);

            var ownerId = await UserHelper.CreateInDbAsync(testDB, subscribeToCardOnEdit: false);
            var frontSide = StringServices.RandomString();
            var backSide = StringServices.RandomString();
            var additionalInfo = StringServices.RandomString();
            var versionDescription = StringServices.RandomString();

            var languageId = await CardLanguagHelper.CreateAsync(testDB);
            var imageOnFrontSideId = await ImageHelper.CreateAsync(testDB);
            var imageOnBackSide1Id = await ImageHelper.CreateAsync(testDB);
            var imageOnBackSide2Id = await ImageHelper.CreateAsync(testDB);
            var imageOnAdditionalInfoId = await ImageHelper.CreateAsync(testDB);
            var tagId = await TagHelper.CreateAsync(testDB);

            Guid cardGuid = Guid.Empty;
            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new CreateCard.Request(
                    ownerId,
                    frontSide,
                    new Guid[] { imageOnFrontSideId },
                    backSide,
                    new Guid[] { imageOnBackSide1Id, imageOnBackSide2Id },
                    additionalInfo,
                    new Guid[] { imageOnAdditionalInfoId },
                    languageId,
                    new Guid[] { tagId },
                    new Guid[] { ownerId, userWithViewId },
                    versionDescription);
                cardGuid = await new CreateCard(dbContext).RunAsync(request, new TestLocalizer());
                Assert.AreNotEqual(Guid.Empty, cardGuid);
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var card = dbContext.Cards
                    .Include(c => c.VersionCreator)
                    .Include(c => c.CardLanguage)
                    .Include(c => c.UsersWithView)
                    .Include(c => c.Images)
                    .Include(c => c.TagsInCards)
                    .Single(c => c.Id == cardGuid);
                Assert.AreEqual(ownerId, card.VersionCreator.Id);
                Assert.AreEqual(frontSide, card.FrontSide);
                Assert.AreEqual(backSide, card.BackSide);
                Assert.AreEqual(additionalInfo, card.AdditionalInfo);
                Assert.AreEqual(versionDescription, card.VersionDescription);
                Assert.AreEqual(languageId, card.CardLanguage.Id);
                Assert.IsTrue(card.UsersWithView.Any(u => u.UserId == ownerId));
                Assert.IsTrue(card.UsersWithView.Any(u => u.UserId == userWithViewId));
                Assert.AreEqual(1, card.Images.Single(i => i.ImageId == imageOnFrontSideId).CardSide);
                Assert.AreEqual(2, card.Images.Single(i => i.ImageId == imageOnBackSide1Id).CardSide);
                Assert.AreEqual(2, card.Images.Single(i => i.ImageId == imageOnBackSide2Id).CardSide);
                Assert.AreEqual(3, card.Images.Single(i => i.ImageId == imageOnAdditionalInfoId).CardSide);
                Assert.IsTrue(card.TagsInCards.Any(t => t.TagId == tagId));
                Assert.IsFalse(dbContext.CardNotifications.Any(cardSubscription => cardSubscription.CardId == cardGuid && cardSubscription.UserId == ownerId));
            }
        }
        [TestMethod()]
        public async Task TestCreationWithUserSubscribingToCardOnEdit()
        {
            var testDB = DbServices.GetEmptyTestDB(typeof(CreateCardTests));

            var ownerId = await UserHelper.CreateInDbAsync(testDB, subscribeToCardOnEdit: true);
            var languageId = await CardLanguagHelper.CreateAsync(testDB);

            Guid cardGuid = Guid.Empty;
            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new CreateCard.Request(
                    ownerId,
                    StringServices.RandomString(),
                    new Guid[0],
                    StringServices.RandomString(),
                    new Guid[0],
                    StringServices.RandomString(),
                    new Guid[0],
                    languageId,
                    new Guid[0],
                    new Guid[0],
                    StringServices.RandomString());
                cardGuid = await new CreateCard(dbContext).RunAsync(request, new TestLocalizer());
            }

            using (var dbContext = new MemCheckDbContext(testDB))
                Assert.IsTrue(dbContext.CardNotifications.Any(cardSubscription => cardSubscription.CardId == cardGuid && cardSubscription.UserId == ownerId));
        }
    }
}

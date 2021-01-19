using MemCheck.Application.Tests.Helpers;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.CardChanging
{
    [TestClass()]
    public class CreateCardTests
    {
        [TestMethod()]
        public async Task TestCreationWithAllData()
        {
            var testDB = DbHelper.GetEmptyTestDB();

            var userWithViewId = await UserHelper.CreateInDbAsync(testDB);

            var ownerId = await UserHelper.CreateInDbAsync(testDB, subscribeToCardOnEdit: false);
            var frontSide = RandomHelper.String();
            var backSide = RandomHelper.String();
            var additionalInfo = RandomHelper.String();
            var versionDescription = RandomHelper.String();

            var languageId = await CardLanguagHelper.CreateAsync(testDB);
            var imageOnFrontSideId = await ImageHelper.CreateAsync(testDB, ownerId);
            var imageOnBackSide1Id = await ImageHelper.CreateAsync(testDB, ownerId);
            var imageOnBackSide2Id = await ImageHelper.CreateAsync(testDB, ownerId);
            var imageOnAdditionalInfoId = await ImageHelper.CreateAsync(testDB, ownerId);
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
                Assert.AreEqual(ImageInCard.FrontSide, card.Images.Single(i => i.ImageId == imageOnFrontSideId).CardSide);
                Assert.AreEqual(ImageInCard.BackSide, card.Images.Single(i => i.ImageId == imageOnBackSide1Id).CardSide);
                Assert.AreEqual(ImageInCard.BackSide, card.Images.Single(i => i.ImageId == imageOnBackSide2Id).CardSide);
                Assert.AreEqual(ImageInCard.AdditionalInfo, card.Images.Single(i => i.ImageId == imageOnAdditionalInfoId).CardSide);
                Assert.IsTrue(card.TagsInCards.Any(t => t.TagId == tagId));
            }
            Assert.IsFalse(await CardSubscriptionHelper.UserIsSubscribedToCardAsync(testDB, ownerId, cardGuid));
        }
        [TestMethod()]
        public async Task TestCreationWithUserSubscribingToCardOnEdit()
        {
            var testDB = DbHelper.GetEmptyTestDB();

            var ownerId = await UserHelper.CreateInDbAsync(testDB, subscribeToCardOnEdit: true);
            var languageId = await CardLanguagHelper.CreateAsync(testDB);

            Guid cardGuid = Guid.Empty;
            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new CreateCard.Request(
                    ownerId,
                    RandomHelper.String(),
                    Array.Empty<Guid>(),
                    RandomHelper.String(),
                    Array.Empty<Guid>(),
                    RandomHelper.String(),
                    Array.Empty<Guid>(),
                    languageId,
                    Array.Empty<Guid>(),
                    Array.Empty<Guid>(),
                    RandomHelper.String());
                cardGuid = await new CreateCard(dbContext).RunAsync(request, new TestLocalizer());
            }

            Assert.IsTrue(await CardSubscriptionHelper.UserIsSubscribedToCardAsync(testDB, ownerId, cardGuid));
        }
        [TestMethod()]
        public async Task TestCreatioFailsIfCreatorNotInVisibilityList()
        {
            var testDB = DbHelper.GetEmptyTestDB();

            var creatorId = await UserHelper.CreateInDbAsync(testDB);
            var otherUser = await UserHelper.CreateInDbAsync(testDB);
            var languageId = await CardLanguagHelper.CreateAsync(testDB);

            Guid cardGuid = Guid.Empty;
            using var dbContext = new MemCheckDbContext(testDB);
            var request = new CreateCard.Request(
                creatorId,
                RandomHelper.String(),
                Array.Empty<Guid>(),
                RandomHelper.String(),
                Array.Empty<Guid>(),
                RandomHelper.String(),
                Array.Empty<Guid>(),
                languageId,
                Array.Empty<Guid>(),
                new Guid[] { otherUser },
                RandomHelper.String());
            var ownerMustHaveVisibility = RandomHelper.String();
            var localizer = new TestLocalizer(new[] { new KeyValuePair<string, string>("OwnerMustHaveVisibility", ownerMustHaveVisibility) });
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new CreateCard(dbContext).RunAsync(request, localizer));
            Assert.AreEqual(ownerMustHaveVisibility, exception.Message);
        }
    }
}

using MemCheck.Application.Loading;
using MemCheck.Application.QueryValidation;
using MemCheck.Application.Tests.Helpers;
using MemCheck.Basics;
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
        public async Task WithAllData()
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
        public async Task WithUserSubscribingToCardOnEdit()
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
        public async Task CreatorNotInVisibilityList()
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
            var localizer = new TestLocalizer(new KeyValuePair<string, string>("OwnerMustHaveVisibility", ownerMustHaveVisibility).ToEnumerable());
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new CreateCard(dbContext).RunAsync(request, localizer));
            Assert.AreEqual(ownerMustHaveVisibility, exception.Message);
        }
        [TestMethod()]
        public async Task MultipleImagesOnAdditionalSide()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var language = await CardLanguagHelper.CreateAsync(db);
            var image1 = await ImageHelper.CreateAsync(db, user);
            var image2 = await ImageHelper.CreateAsync(db, user);

            Guid cardGuid;
            var createRequest = new CreateCard.Request(
                user,
                RandomHelper.String(),
                Array.Empty<Guid>(),
                RandomHelper.String(),
                Array.Empty<Guid>(),
                RandomHelper.String(),
                new Guid[] { image1, image2 },
                language,
                Array.Empty<Guid>(),
                Array.Empty<Guid>(),
                RandomHelper.String());
            using (var dbContext = new MemCheckDbContext(db))
                cardGuid = await new CreateCard(dbContext).RunAsync(createRequest, new TestLocalizer());

            var deck = await DeckHelper.CreateAsync(db, user);
            await DeckHelper.AddCardAsync(db, deck, cardGuid, 1, new DateTime(2000, 1, 1));

            using (var dbContext = new MemCheckDbContext(db))
            {
                var request = new GetCardsToRepeat.Request(user, deck, Array.Empty<Guid>(), Array.Empty<Guid>(), 10);
                var card = (await new GetCardsToRepeat(dbContext).RunAsync(request, new DateTime(2000, 1, 4))).Single();

                var images = card.Images;
                Assert.AreEqual(2, images.Count());

                var first = card.Images.First();
                Assert.AreEqual(ImageInCard.AdditionalInfo, first.CardSide);
                Assert.AreEqual(image1, first.ImageId);

                var last = card.Images.Last();
                Assert.AreEqual(ImageInCard.AdditionalInfo, last.CardSide);
                Assert.AreEqual(image2, last.ImageId);
            }
        }
        [TestMethod()]
        public async Task SameImageUsedTwice()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var language = await CardLanguagHelper.CreateAsync(db);
            var image = await ImageHelper.CreateAsync(db, user);

            var request = new CreateCard.Request(
                user,
                RandomHelper.String(),
                Array.Empty<Guid>(),
                RandomHelper.String(),
                image.ToEnumerable(),
                RandomHelper.String(),
                image.ToEnumerable(),
                language,
                Array.Empty<Guid>(),
                Array.Empty<Guid>(),
                RandomHelper.String());

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new CreateCard(dbContext).RunAsync(request, new TestLocalizer()));
        }
    }
}

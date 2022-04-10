using MemCheck.Application.Helpers;
using MemCheck.Application.QueryValidation;
using MemCheck.Basics;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards
{
    [TestClass()]
    public class CreateCardTests
    {
        [TestMethod()]
        public async Task WithOneImage()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var ownerId = await UserHelper.CreateInDbAsync(testDB, subscribeToCardOnEdit: false);
            var languageId = await CardLanguagHelper.CreateAsync(testDB);
            var imageId = await ImageHelper.CreateAsync(testDB, ownerId);

            Guid cardGuid = Guid.Empty;
            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new CreateCard.Request(
                    ownerId,
                    RandomHelper.String(),
                    new Guid[] { imageId },
                    RandomHelper.String(),
                    Array.Empty<Guid>(),
                    RandomHelper.String(),
                    Array.Empty<Guid>(),
                    RandomHelper.String(),
                    languageId,
                    Array.Empty<Guid>(),
                    Array.Empty<Guid>(),
                    RandomHelper.String());
                cardGuid = (await new CreateCard(dbContext.AsCallContext()).RunAsync(request)).CardId;
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var card = dbContext.Cards.Include(c => c.Images).Single(c => c.Id == cardGuid);
                Assert.AreEqual(ImageInCard.FrontSide, card.Images.Single(i => i.ImageId == imageId).CardSide);
            }
        }
        [TestMethod()]
        public async Task WithAllData()
        {
            var testDB = DbHelper.GetEmptyTestDB();

            var userWithViewId = await UserHelper.CreateInDbAsync(testDB);

            var ownerId = await UserHelper.CreateInDbAsync(testDB, subscribeToCardOnEdit: false);
            var frontSide = RandomHelper.String();
            var backSide = RandomHelper.String();
            var additionalInfo = RandomHelper.String();
            var references = RandomHelper.String();
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
                    references,
                    languageId,
                    new Guid[] { tagId },
                    new Guid[] { ownerId, userWithViewId },
                    versionDescription);
                cardGuid = (await new CreateCard(dbContext.AsCallContext()).RunAsync(request)).CardId;
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
                Assert.AreEqual(references, card.References);
                Assert.AreEqual(versionDescription, card.VersionDescription);
                Assert.AreEqual(languageId, card.CardLanguage.Id);
                Assert.IsTrue(card.UsersWithView.Any(u => u.UserId == ownerId));
                Assert.IsTrue(card.UsersWithView.Any(u => u.UserId == userWithViewId));
                Assert.AreEqual(ImageInCard.FrontSide, card.Images.Single(i => i.ImageId == imageOnFrontSideId).CardSide);
                Assert.AreEqual(ImageInCard.BackSide, card.Images.Single(i => i.ImageId == imageOnBackSide1Id).CardSide);
                Assert.AreEqual(ImageInCard.BackSide, card.Images.Single(i => i.ImageId == imageOnBackSide2Id).CardSide);
                Assert.AreEqual(ImageInCard.AdditionalInfo, card.Images.Single(i => i.ImageId == imageOnAdditionalInfoId).CardSide);
                Assert.IsTrue(card.TagsInCards.Any(t => t.TagId == tagId));
                Assert.AreEqual(0, card.RatingCount);
                Assert.AreEqual(0, card.AverageRating);
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
                    RandomHelper.String(),
                    languageId,
                    Array.Empty<Guid>(),
                    Array.Empty<Guid>(),
                    RandomHelper.String());
                cardGuid = (await new CreateCard(dbContext.AsCallContext()).RunAsync(request)).CardId;
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
                RandomHelper.String(),
                languageId,
                Array.Empty<Guid>(),
                new Guid[] { otherUser },
                RandomHelper.String());
            var ownerMustHaveVisibility = RandomHelper.String();
            var localizer = new TestLocalizer(new KeyValuePair<string, string>("OwnerMustHaveVisibility", ownerMustHaveVisibility).AsArray());
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new CreateCard(dbContext.AsCallContext(localizer)).RunAsync(request));
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
                RandomHelper.String(),
                language,
                Array.Empty<Guid>(),
                Array.Empty<Guid>(),
                RandomHelper.String());
            using (var dbContext = new MemCheckDbContext(db))
                cardGuid = (await new CreateCard(dbContext.AsCallContext()).RunAsync(createRequest)).CardId;

            var deck = await DeckHelper.CreateAsync(db, user);
            var addToDeckDate = RandomHelper.Date();
            await DeckHelper.AddCardAsync(db, deck, cardGuid, 1, addToDeckDate);

            using (var dbContext = new MemCheckDbContext(db))
            {
                var request = new GetCardsToRepeat.Request(user, deck, Array.Empty<Guid>(), Array.Empty<Guid>(), 10);
                var card = (await new GetCardsToRepeat(dbContext.AsCallContext(), addToDeckDate.AddDays(1)).RunAsync(request)).Cards.Single();

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
                image.AsArray(),
                RandomHelper.String(),
                image.AsArray(),
                RandomHelper.String(),
                language,
                Array.Empty<Guid>(),
                Array.Empty<Guid>(),
                RandomHelper.String());

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new CreateCard(dbContext.AsCallContext()).RunAsync(request));
        }
        [TestMethod()]
        public async Task ReferencesTooLong()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var creatorId = await UserHelper.CreateInDbAsync(testDB);
            var languageId = await CardLanguagHelper.CreateAsync(testDB);

            var request = new CreateCard.Request(
                creatorId,
                RandomHelper.String(),
                Array.Empty<Guid>(),
                RandomHelper.String(),
                Array.Empty<Guid>(),
                RandomHelper.String(),
                Array.Empty<Guid>(),
                RandomHelper.String(CardInputValidator.MaxReferencesLength + 1),
                languageId,
                Array.Empty<Guid>(),
                Array.Empty<Guid>(),
                RandomHelper.String());

            using var dbContext = new MemCheckDbContext(testDB);
            var exception = await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new CreateCard(dbContext.AsCallContext(new TestLocalizer())).RunAsync(request));
            StringAssert.Contains(exception.Message, CardInputValidator.MinReferencesLength.ToString(CultureInfo.InvariantCulture));
            StringAssert.Contains(exception.Message, CardInputValidator.MaxReferencesLength.ToString(CultureInfo.InvariantCulture));
            StringAssert.Contains(exception.Message, (CardInputValidator.MaxReferencesLength + 1).ToString(CultureInfo.InvariantCulture));
        }
    }
}
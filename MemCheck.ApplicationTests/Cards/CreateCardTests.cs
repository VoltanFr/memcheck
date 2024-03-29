﻿using MemCheck.Application.Helpers;
using MemCheck.Application.QueryValidation;
using MemCheck.Basics;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards;

[TestClass()]
public class CreateCardTests
{
    [TestMethod()]
    public async Task WithAllData()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var userWithViewId = await UserHelper.CreateInDbAsync(testDB);
        var owner = await UserHelper.CreateUserInDbAsync(testDB, subscribeToCardOnEdit: false);
        var frontSide = RandomHelper.String();
        var backSide = RandomHelper.String();
        var additionalInfo = RandomHelper.String();
        var references = RandomHelper.String();
        var versionDescription = RandomHelper.String();
        var languageId = await CardLanguageHelper.CreateAsync(testDB);
        var tagId = await TagHelper.CreateAsync(testDB, owner);

        var cardGuid = Guid.Empty;
        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var request = new CreateCard.Request(
                owner.Id,
                frontSide,
                backSide,
                additionalInfo,
                references,
                languageId,
                new Guid[] { tagId },
                new Guid[] { owner.Id, userWithViewId },
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
                .Include(c => c.TagsInCards)
                .Single(c => c.Id == cardGuid);
            Assert.AreEqual(owner.Id, card.VersionCreator.Id);
            Assert.AreEqual(frontSide, card.FrontSide);
            Assert.AreEqual(backSide, card.BackSide);
            Assert.AreEqual(additionalInfo, card.AdditionalInfo);
            Assert.AreEqual(references, card.References);
            Assert.AreEqual(versionDescription, card.VersionDescription);
            Assert.AreEqual(languageId, card.CardLanguage.Id);
            Assert.IsTrue(card.UsersWithView.Any(u => u.UserId == owner.Id));
            Assert.IsTrue(card.UsersWithView.Any(u => u.UserId == userWithViewId));
            Assert.IsTrue(card.TagsInCards.Any(t => t.TagId == tagId));
            Assert.AreEqual(0, card.RatingCount);
            Assert.AreEqual(0, card.AverageRating);
        }
        Assert.IsFalse(await CardSubscriptionHelper.UserIsSubscribedToCardAsync(testDB, owner.Id, cardGuid));
    }
    [TestMethod()]
    public async Task WithUserSubscribingToCardOnEdit()
    {
        var testDB = DbHelper.GetEmptyTestDB();

        var ownerId = await UserHelper.CreateInDbAsync(testDB, subscribeToCardOnEdit: true);
        var languageId = await CardLanguageHelper.CreateAsync(testDB);

        var cardGuid = Guid.Empty;
        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var request = new CreateCard.Request(
                ownerId,
                RandomHelper.String(),
                RandomHelper.String(),
                RandomHelper.String(),
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
        var languageId = await CardLanguageHelper.CreateAsync(testDB);

        var cardGuid = Guid.Empty;
        using var dbContext = new MemCheckDbContext(testDB);
        var request = new CreateCard.Request(
            creatorId,
            RandomHelper.String(),
            RandomHelper.String(),
            RandomHelper.String(),
            RandomHelper.String(),
            languageId,
            Array.Empty<Guid>(),
            new Guid[] { otherUser },
            RandomHelper.String());
        var ownerMustHaveVisibility = RandomHelper.String();
        var localizer = new TestLocalizer("OwnerMustHaveVisibility".PairedWith(ownerMustHaveVisibility));
        var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new CreateCard(dbContext.AsCallContext(localizer)).RunAsync(request));
        Assert.AreEqual(ownerMustHaveVisibility, exception.Message);
    }
    [TestMethod()]
    public async Task FrontSideTooLong()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var creatorId = await UserHelper.CreateInDbAsync(testDB);
        var languageId = await CardLanguageHelper.CreateAsync(testDB);

        var request = new CreateCard.Request(
            creatorId,
            RandomHelper.String(CardInputValidator.MaxFrontSideLength + 1),
            RandomHelper.String(),
            RandomHelper.String(),
            RandomHelper.String(),
            languageId,
            Array.Empty<Guid>(),
            Array.Empty<Guid>(),
            RandomHelper.String());

        using var dbContext = new MemCheckDbContext(testDB);
        var exception = await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new CreateCard(dbContext.AsCallContext(new TestLocalizer())).RunAsync(request));
        StringAssert.Contains(exception.Message, CardInputValidator.MinFrontSideLength.ToString(CultureInfo.InvariantCulture));
        StringAssert.Contains(exception.Message, CardInputValidator.MaxFrontSideLength.ToString(CultureInfo.InvariantCulture));
        StringAssert.Contains(exception.Message, (CardInputValidator.MaxFrontSideLength + 1).ToString(CultureInfo.InvariantCulture));
    }
    [TestMethod()]
    public async Task BackSideTooLong()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var creatorId = await UserHelper.CreateInDbAsync(testDB);
        var languageId = await CardLanguageHelper.CreateAsync(testDB);

        var request = new CreateCard.Request(
            creatorId,
            RandomHelper.String(),
            RandomHelper.String(CardInputValidator.MaxBackSideLength + 1),
            RandomHelper.String(),
            RandomHelper.String(),
            languageId,
            Array.Empty<Guid>(),
            Array.Empty<Guid>(),
            RandomHelper.String());

        using var dbContext = new MemCheckDbContext(testDB);
        var exception = await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new CreateCard(dbContext.AsCallContext(new TestLocalizer())).RunAsync(request));
        StringAssert.Contains(exception.Message, CardInputValidator.MinBackSideLength.ToString(CultureInfo.InvariantCulture));
        StringAssert.Contains(exception.Message, CardInputValidator.MaxBackSideLength.ToString(CultureInfo.InvariantCulture));
        StringAssert.Contains(exception.Message, (CardInputValidator.MaxBackSideLength + 1).ToString(CultureInfo.InvariantCulture));
    }
    [TestMethod()]
    public async Task AdditionalInfoTooLong()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var creatorId = await UserHelper.CreateInDbAsync(testDB);
        var languageId = await CardLanguageHelper.CreateAsync(testDB);

        var request = new CreateCard.Request(
            creatorId,
            RandomHelper.String(),
            RandomHelper.String(),
            RandomHelper.String(CardInputValidator.MaxAdditionalInfoLength + 1),
            RandomHelper.String(),
            languageId,
            Array.Empty<Guid>(),
            Array.Empty<Guid>(),
            RandomHelper.String());

        using var dbContext = new MemCheckDbContext(testDB);
        var exception = await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new CreateCard(dbContext.AsCallContext(new TestLocalizer())).RunAsync(request));
        StringAssert.Contains(exception.Message, CardInputValidator.MinAdditionalInfoLength.ToString(CultureInfo.InvariantCulture));
        StringAssert.Contains(exception.Message, CardInputValidator.MaxAdditionalInfoLength.ToString(CultureInfo.InvariantCulture));
        StringAssert.Contains(exception.Message, (CardInputValidator.MaxAdditionalInfoLength + 1).ToString(CultureInfo.InvariantCulture));
    }
    [TestMethod()]
    public async Task ReferencesTooLong()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var creatorId = await UserHelper.CreateInDbAsync(testDB);
        var languageId = await CardLanguageHelper.CreateAsync(testDB);

        var request = new CreateCard.Request(
            creatorId,
            RandomHelper.String(),
            RandomHelper.String(),
            RandomHelper.String(),
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
    [TestMethod()]
    public async Task PublicCardWithPersoTagMustFail()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var creator = await UserHelper.CreateUserInDbAsync(testDB);
        var languageId = await CardLanguageHelper.CreateAsync(testDB);
        var persoTagId = await TagHelper.CreateAsync(testDB, creator, name: Tag.Perso);

        var request = new CreateCard.Request(
            creator.Id,
            RandomHelper.String(),
            RandomHelper.String(),
            RandomHelper.String(),
            RandomHelper.String(),
            languageId,
            persoTagId.AsArray(),
            Array.Empty<Guid>(),
            RandomHelper.String());

        var errorMesg = RandomHelper.String();
        var localizer = new TestLocalizer("PersoTagAllowedOnlyOnPrivateCards".PairedWith(errorMesg));
        using var dbContext = new MemCheckDbContext(testDB);
        var exception = await Assert.ThrowsExceptionAsync<PersoTagAllowedOnlyOnPrivateCardsException>(async () => await new CreateCard(dbContext.AsCallContext(localizer)).RunAsync(request));
        Assert.AreEqual(errorMesg, exception.Message);
    }
    [TestMethod()]
    public async Task CardVisibleToOtherWithPersoTagMustFail()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var creator = await UserHelper.CreateUserInDbAsync(testDB);
        var otherUserId = await UserHelper.CreateInDbAsync(testDB);
        var languageId = await CardLanguageHelper.CreateAsync(testDB);
        var persoTagId = await TagHelper.CreateAsync(testDB, creator, name: Tag.Perso);

        var request = new CreateCard.Request(
            creator.Id,
            RandomHelper.String(),
            RandomHelper.String(),
            RandomHelper.String(),
            RandomHelper.String(),
            languageId,
            persoTagId.AsArray(),
            new[] { creator.Id, otherUserId },
            RandomHelper.String());

        var errorMesg = RandomHelper.String();
        var localizer = new TestLocalizer("PersoTagAllowedOnlyOnPrivateCards".PairedWith(errorMesg));
        using var dbContext = new MemCheckDbContext(testDB);
        var exception = await Assert.ThrowsExceptionAsync<PersoTagAllowedOnlyOnPrivateCardsException>(async () => await new CreateCard(dbContext.AsCallContext(localizer)).RunAsync(request));
        Assert.AreEqual(errorMesg, exception.Message);
    }
    [TestMethod()]
    public async Task PrivateCardWithPersoTagMustSucceed()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var creator = await UserHelper.CreateUserInDbAsync(testDB);
        var languageId = await CardLanguageHelper.CreateAsync(testDB);
        var persoTagId = await TagHelper.CreateAsync(testDB, creator, name: Tag.Perso);

        var request = new CreateCard.Request(
            creator.Id,
            RandomHelper.String(),
            RandomHelper.String(),
            RandomHelper.String(),
            RandomHelper.String(),
            languageId,
            persoTagId.AsArray(),
            creator.Id.AsArray(),
            RandomHelper.String());

        using (var dbContext = new MemCheckDbContext(testDB))
            await new CreateCard(dbContext.AsCallContext()).RunAsync(request);

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var card = dbContext.Cards.Include(card => card.VersionCreator).Single();
            Assert.AreEqual(creator.Id, card.VersionCreator.Id);
        }
    }
    [TestMethod()]
    public async Task CardWithImage()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var creatorId = await UserHelper.CreateInDbAsync(testDB);
        var languageId = await CardLanguageHelper.CreateAsync(testDB);
        var imageName = RandomHelper.String();

        var imageId = await ImageHelper.CreateAsync(testDB, creatorId, name: imageName);

        var request = new CreateCard.Request(
            creatorId,
            $"![Mnesios:{imageName}]",
            RandomHelper.String(),
            RandomHelper.String(),
            RandomHelper.String(),
            languageId,
            Array.Empty<Guid>(),
            creatorId.AsArray(),
            RandomHelper.String());

        using (var dbContext = new MemCheckDbContext(testDB))
            await new CreateCard(dbContext.AsCallContext()).RunAsync(request);

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var card = await dbContext.Cards.SingleAsync();
            Assert.IsTrue(await dbContext.ImagesInCards.AnyAsync());
            var imageInCard = await dbContext.ImagesInCards.SingleAsync();
            Assert.AreEqual(imageId, imageInCard.ImageId);
            Assert.AreEqual(card.Id, imageInCard.CardId);
        }
    }
    [TestMethod()]
    public async Task CardWithImageNotInDb()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var creatorId = await UserHelper.CreateInDbAsync(testDB);
        var languageId = await CardLanguageHelper.CreateAsync(testDB);

        var request = new CreateCard.Request(
            creatorId,
            $"![Mnesios:{RandomHelper.String()}]",
            RandomHelper.String(),
            RandomHelper.String(),
            RandomHelper.String(),
            languageId,
            Array.Empty<Guid>(),
            creatorId.AsArray(),
            RandomHelper.String());

        using (var dbContext = new MemCheckDbContext(testDB))
            await new CreateCard(dbContext.AsCallContext()).RunAsync(request);

        using (var dbContext = new MemCheckDbContext(testDB))
            Assert.IsFalse(await dbContext.ImagesInCards.AnyAsync());
    }
    [TestMethod()]
    public async Task CardsWithMultipleImages()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var creatorId = await UserHelper.CreateInDbAsync(testDB);
        var languageId = await CardLanguageHelper.CreateAsync(testDB);

        var image1Name = RandomHelper.String();
        var image1Id = await ImageHelper.CreateAsync(testDB, creatorId, name: image1Name);

        var image2Name = RandomHelper.String();
        var image2Id = await ImageHelper.CreateAsync(testDB, creatorId, name: image2Name);

        var image3Name = RandomHelper.String();
        var image3Id = await ImageHelper.CreateAsync(testDB, creatorId, name: image3Name);

        Guid card1Id;   //Card 1 contains image1, image2 and image3
        Guid card2Id;   //Card 2 contains image1 and image2
        Guid card3Id;   //Card 3 contains image2 and image3
        Guid card4Id;   //Card 4 contains no image

        using (var dbContext = new MemCheckDbContext(testDB))
            card1Id = (await new CreateCard(dbContext.AsCallContext()).RunAsync(new CreateCard.Request(
                creatorId,
                $"![Mnesios:{image1Name}]",
                RandomHelper.String(),
                $"some text ![Mnesios:{image1Name}] ![Mnesios:{image3Name}] ![Mnesios:{image2Name}]",
                RandomHelper.String(),
                languageId,
                Array.Empty<Guid>(),
                creatorId.AsArray(),
                RandomHelper.String()))).CardId;

        using (var dbContext = new MemCheckDbContext(testDB))
            card2Id = (await new CreateCard(dbContext.AsCallContext()).RunAsync(new CreateCard.Request(
                creatorId,
                "![Mnesios:DoesNotExist]",
                RandomHelper.String(),
                $"some text ![Mnesios:{image1Name}] ![Mnesios:{image2Name}] ![Mnesios:{image2Name}]",
                RandomHelper.String(),
                languageId,
                Array.Empty<Guid>(),
                creatorId.AsArray(),
                RandomHelper.String()))).CardId;

        using (var dbContext = new MemCheckDbContext(testDB))
            card3Id = (await new CreateCard(dbContext.AsCallContext()).RunAsync(new CreateCard.Request(
                creatorId,
                RandomHelper.String(),
                $"![Mnesios:{image2Name}] ![Mnesios:{image3Name}]",
                $"![Mnesios:{image2Name}] ![Mnesios:{image3Name}]",
                RandomHelper.String(),
                languageId,
                Array.Empty<Guid>(),
                creatorId.AsArray(),
                RandomHelper.String()))).CardId;

        using (var dbContext = new MemCheckDbContext(testDB))
            card4Id = (await new CreateCard(dbContext.AsCallContext()).RunAsync(new CreateCard.Request(
                creatorId,
                RandomHelper.String(),
                RandomHelper.String(),
                RandomHelper.String(),
                RandomHelper.String(),
                languageId,
                Array.Empty<Guid>(),
                creatorId.AsArray(),
                RandomHelper.String()))).CardId;

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var imagesInCard1 = dbContext.ImagesInCards.Where(imageInCard => imageInCard.CardId == card1Id).ToImmutableArray();
            Assert.AreEqual(3, imagesInCard1.Length);
            Assert.IsTrue(imagesInCard1.Any(imageInCard => imageInCard.ImageId == image1Id));
            Assert.IsTrue(imagesInCard1.Any(imageInCard => imageInCard.ImageId == image2Id));
            Assert.IsTrue(imagesInCard1.Any(imageInCard => imageInCard.ImageId == image3Id));

            var imagesInCard2 = dbContext.ImagesInCards.Where(imageInCard => imageInCard.CardId == card2Id).ToImmutableArray();
            Assert.AreEqual(2, imagesInCard2.Length);
            Assert.IsTrue(imagesInCard2.Any(imageInCard => imageInCard.ImageId == image1Id));
            Assert.IsTrue(imagesInCard2.Any(imageInCard => imageInCard.ImageId == image2Id));

            var imagesInCard3 = dbContext.ImagesInCards.Where(imageInCard => imageInCard.CardId == card3Id).ToImmutableArray();
            Assert.AreEqual(2, imagesInCard3.Length);
            Assert.IsTrue(imagesInCard3.Any(imageInCard => imageInCard.ImageId == image3Id));
            Assert.IsTrue(imagesInCard3.Any(imageInCard => imageInCard.ImageId == image2Id));

            var imagesInCard4 = dbContext.ImagesInCards.Where(imageInCard => imageInCard.CardId == card4Id).ToImmutableArray();
            Assert.IsFalse(imagesInCard4.Any());
        }
    }
}

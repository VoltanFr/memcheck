using MemCheck.Application.Helpers;
using MemCheck.Application.QueryValidation;
using MemCheck.Basics;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
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

        var ownerId = await UserHelper.CreateInDbAsync(testDB, subscribeToCardOnEdit: false);
        var frontSide = RandomHelper.String();
        var backSide = RandomHelper.String();
        var additionalInfo = RandomHelper.String();
        var references = RandomHelper.String();
        var versionDescription = RandomHelper.String();

        var languageId = await CardLanguageHelper.CreateAsync(testDB);
        var imageOnFrontSideId = await ImageHelper.CreateAsync(testDB, ownerId);
        var imageOnBackSide1Id = await ImageHelper.CreateAsync(testDB, ownerId);
        var imageOnBackSide2Id = await ImageHelper.CreateAsync(testDB, ownerId);
        var imageOnAdditionalInfoId = await ImageHelper.CreateAsync(testDB, ownerId);
        var tagId = await TagHelper.CreateAsync(testDB);

        var cardGuid = Guid.Empty;
        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var request = new CreateCard.Request(
                ownerId,
                frontSide,
                backSide,
                additionalInfo,
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
        var creatorId = await UserHelper.CreateInDbAsync(testDB);
        var languageId = await CardLanguageHelper.CreateAsync(testDB);
        var persoTagId = await TagHelper.CreateAsync(testDB, name: Tag.Perso);

        var request = new CreateCard.Request(
            creatorId,
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
        var creatorId = await UserHelper.CreateInDbAsync(testDB);
        var otherUserId = await UserHelper.CreateInDbAsync(testDB);
        var languageId = await CardLanguageHelper.CreateAsync(testDB);
        var persoTagId = await TagHelper.CreateAsync(testDB, name: Tag.Perso);

        var request = new CreateCard.Request(
            creatorId,
            RandomHelper.String(),
            RandomHelper.String(),
            RandomHelper.String(),
            RandomHelper.String(),
            languageId,
            persoTagId.AsArray(),
            new[] { creatorId, otherUserId },
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
        var creatorId = await UserHelper.CreateInDbAsync(testDB);
        var languageId = await CardLanguageHelper.CreateAsync(testDB);
        var persoTagId = await TagHelper.CreateAsync(testDB, name: Tag.Perso);

        var request = new CreateCard.Request(
            creatorId,
            RandomHelper.String(),
            RandomHelper.String(),
            RandomHelper.String(),
            RandomHelper.String(),
            languageId,
            persoTagId.AsArray(),
            creatorId.AsArray(),
            RandomHelper.String());

        using (var dbContext = new MemCheckDbContext(testDB))
            await new CreateCard(dbContext.AsCallContext()).RunAsync(request);

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var card = dbContext.Cards.Include(card => card.VersionCreator).Single();
            Assert.AreEqual(creatorId, card.VersionCreator.Id);
        }
    }
}

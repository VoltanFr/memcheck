using MemCheck.Application.Cards;
using MemCheck.Application.Helpers;
using MemCheck.Basics;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Images;

[TestClass()]
public class RefreshImagesInCardsTests
{
    [TestMethod()]
    public async Task EmptyDb()
    {
        var db = DbHelper.GetEmptyTestDB();

        using (var dbContext = new MemCheckDbContext(db))
        {
            var result = await new RefreshImagesInCards(dbContext.AsCallContext()).RunAsync(new RefreshImagesInCards.Request());
            Assert.AreEqual(0, result.TotalImageCount);
            Assert.AreEqual(0, result.TotalCardCount);
            Assert.AreEqual(0, result.ImagesInCardsCountOnStart);
            Assert.AreEqual(0, result.ImagesInCardsCountOnEnd);
        }

        using (var dbContext = new MemCheckDbContext(db))
            Assert.IsFalse(dbContext.ImagesInCards.Any());
    }
    [TestMethod()]
    public async Task CardHasNoImage_RefreshMustDoNothing()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var creatorId = await UserHelper.CreateInDbAsync(testDB);

        var imageName = RandomHelper.String();
        await ImageHelper.CreateAsync(testDB, creatorId, name: imageName);

        await CardHelper.CreateAsync(testDB, creatorId);

        using (var dbContext = new MemCheckDbContext(testDB))
            Assert.IsFalse(dbContext.ImagesInCards.Any());

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var result = await new RefreshImagesInCards(dbContext.AsCallContext()).RunAsync(new RefreshImagesInCards.Request());
            Assert.AreEqual(1, result.TotalImageCount);
            Assert.AreEqual(1, result.TotalCardCount);
            Assert.AreEqual(0, result.ImagesInCardsCountOnStart);
            Assert.AreEqual(0, result.ImagesInCardsCountOnEnd);
        }

        using (var dbContext = new MemCheckDbContext(testDB))
            Assert.IsFalse(dbContext.ImagesInCards.Any());
    }
    [TestMethod()]
    public async Task CardHasNoImage_RefreshMustRemove_BecauseTableErroneous()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var creatorId = await UserHelper.CreateInDbAsync(testDB);
        var imageName = RandomHelper.String();

        var imageId = await ImageHelper.CreateAsync(testDB, creatorId, name: imageName);

        var cardId = await CardHelper.CreateIdAsync(testDB, creatorId);

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            dbContext.ImagesInCards.Add(new ImageInCard() { CardId = cardId, ImageId = imageId });
            await dbContext.SaveChangesAsync();
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var result = await new RefreshImagesInCards(dbContext.AsCallContext()).RunAsync(new RefreshImagesInCards.Request());
            Assert.AreEqual(1, result.TotalImageCount);
            Assert.AreEqual(1, result.TotalCardCount);
            Assert.AreEqual(1, result.ImagesInCardsCountOnStart);
            Assert.AreEqual(0, result.ImagesInCardsCountOnEnd);
        }

        using (var dbContext = new MemCheckDbContext(testDB))
            Assert.IsFalse(dbContext.ImagesInCards.Any());
    }
    [TestMethod()]
    public async Task CardHasImage_RefreshMustDoNothing()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var creatorId = await UserHelper.CreateInDbAsync(testDB);

        var imageName = RandomHelper.String();
        var imageId = await ImageHelper.CreateAsync(testDB, creatorId, name: imageName);

        var cardId = await CardHelper.CreateIdAsync(testDB, creatorId, frontSide: $"![Mnesios:{imageName}]");

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var imageInCard = await dbContext.ImagesInCards.SingleAsync();
            Assert.AreEqual(imageId, imageInCard.ImageId);
            Assert.AreEqual(cardId, imageInCard.CardId);
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var result = await new RefreshImagesInCards(dbContext.AsCallContext()).RunAsync(new RefreshImagesInCards.Request());
            Assert.AreEqual(1, result.TotalImageCount);
            Assert.AreEqual(1, result.TotalCardCount);
            Assert.AreEqual(1, result.ImagesInCardsCountOnStart);
            Assert.AreEqual(1, result.ImagesInCardsCountOnEnd);
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var imageInCard = await dbContext.ImagesInCards.SingleAsync();
            Assert.AreEqual(imageId, imageInCard.ImageId);
            Assert.AreEqual(cardId, imageInCard.CardId);
        }
    }
    [TestMethod()]
    public async Task CardHasImage_RefreshMustAdd_BecauseTableErroneous()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var creatorId = await UserHelper.CreateInDbAsync(testDB);

        var imageName = RandomHelper.String();
        var imageId = await ImageHelper.CreateAsync(testDB, creatorId, name: imageName);

        var cardId = await CardHelper.CreateIdAsync(testDB, creatorId, frontSide: $"![Mnesios:{imageName}]");

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var imageInCard = await dbContext.ImagesInCards.SingleAsync();
            dbContext.ImagesInCards.Remove(imageInCard);
            await dbContext.SaveChangesAsync();
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var result = await new RefreshImagesInCards(dbContext.AsCallContext()).RunAsync(new RefreshImagesInCards.Request());
            Assert.AreEqual(1, result.TotalImageCount);
            Assert.AreEqual(1, result.TotalCardCount);
            Assert.AreEqual(0, result.ImagesInCardsCountOnStart);
            Assert.AreEqual(1, result.ImagesInCardsCountOnEnd);
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var imageInCard = await dbContext.ImagesInCards.SingleAsync();
            Assert.AreEqual(imageId, imageInCard.ImageId);
            Assert.AreEqual(cardId, imageInCard.CardId);
        }
    }
    [TestMethod()]
    public async Task CardHasImages_RefreshMustDoNothing()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var creatorId = await UserHelper.CreateInDbAsync(testDB);
        var languageId = await CardLanguageHelper.CreateAsync(testDB);

        var image1Name = RandomHelper.String();
        var image1Id = await ImageHelper.CreateAsync(testDB, creatorId, name: image1Name);

        var image2Name = RandomHelper.String();
        var image2Id = await ImageHelper.CreateAsync(testDB, creatorId, name: image2Name);

        var cardId = await CardHelper.CreateIdAsync(testDB, creatorId, backSide: $"Back: ![Mnesios:{image1Name}]", additionalInfo: $"Additional: ![Mnesios:{image2Name}] ![Mnesios:{image1Name}]");

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            Assert.AreEqual(2, dbContext.ImagesInCards.Count());
            var image1InCard = await dbContext.ImagesInCards.SingleAsync(imageInCard => imageInCard.ImageId == image1Id);
            Assert.AreEqual(cardId, image1InCard.CardId);
            var image2InCard = await dbContext.ImagesInCards.SingleAsync(imageInCard => imageInCard.ImageId == image2Id);
            Assert.AreEqual(cardId, image2InCard.CardId);
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var result = await new RefreshImagesInCards(dbContext.AsCallContext()).RunAsync(new RefreshImagesInCards.Request());
            Assert.AreEqual(2, result.TotalImageCount);
            Assert.AreEqual(1, result.TotalCardCount);
            Assert.AreEqual(2, result.ImagesInCardsCountOnStart);
            Assert.AreEqual(2, result.ImagesInCardsCountOnEnd);
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            Assert.AreEqual(2, dbContext.ImagesInCards.Count());
            var image1InCard = await dbContext.ImagesInCards.SingleAsync(imageInCard => imageInCard.ImageId == image1Id);
            Assert.AreEqual(cardId, image1InCard.CardId);
            var image2InCard = await dbContext.ImagesInCards.SingleAsync(imageInCard => imageInCard.ImageId == image2Id);
            Assert.AreEqual(cardId, image2InCard.CardId);
        }
    }
    [TestMethod()]
    public async Task CardHasImages_RefreshMustAdd_BecauseTableErroneous()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var creatorId = await UserHelper.CreateInDbAsync(testDB);

        var image1Name = RandomHelper.String();
        var image1Id = await ImageHelper.CreateAsync(testDB, creatorId, name: image1Name);

        var image2Name = RandomHelper.String();
        var image2Id = await ImageHelper.CreateAsync(testDB, creatorId, name: image2Name);

        var cardId = await CardHelper.CreateIdAsync(testDB, creatorId, backSide: $"Back: ![Mnesios:{image1Name}]", additionalInfo: $"Additional: ![Mnesios:{image2Name}] ![Mnesios:{image1Name}]");

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            Assert.AreEqual(2, dbContext.ImagesInCards.Count());
            var image1InCard = await dbContext.ImagesInCards.SingleAsync(imageInCard => imageInCard.ImageId == image1Id);
            dbContext.ImagesInCards.Remove(image1InCard);
            await dbContext.SaveChangesAsync();
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var result = await new RefreshImagesInCards(dbContext.AsCallContext()).RunAsync(new RefreshImagesInCards.Request());
            Assert.AreEqual(2, result.TotalImageCount);
            Assert.AreEqual(1, result.TotalCardCount);
            Assert.AreEqual(1, result.ImagesInCardsCountOnStart);
            Assert.AreEqual(2, result.ImagesInCardsCountOnEnd);
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            Assert.AreEqual(2, dbContext.ImagesInCards.Count());
            var image1InCard = await dbContext.ImagesInCards.SingleAsync(imageInCard => imageInCard.ImageId == image1Id);
            Assert.AreEqual(cardId, image1InCard.CardId);
            var image2InCard = await dbContext.ImagesInCards.SingleAsync(imageInCard => imageInCard.ImageId == image2Id);
            Assert.AreEqual(cardId, image2InCard.CardId);
        }
    }
    [TestMethod()]
    public async Task CardHasImages_RefreshMustRemove_BecauseTableErroneous()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var creatorId = await UserHelper.CreateInDbAsync(testDB);

        var image1Name = RandomHelper.String();
        var image1Id = await ImageHelper.CreateAsync(testDB, creatorId, name: image1Name);

        var image2Name = RandomHelper.String();
        var image2Id = await ImageHelper.CreateAsync(testDB, creatorId, name: image2Name);

        var image3Id = await ImageHelper.CreateAsync(testDB, creatorId);

        var cardId = await CardHelper.CreateIdAsync(testDB, creatorId, backSide: $"Back: ![Mnesios:{image1Name}]", additionalInfo: $"Additional: ![Mnesios:{image2Name}] ![Mnesios:{image1Name}]");

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            Assert.AreEqual(2, dbContext.ImagesInCards.Count());
            dbContext.ImagesInCards.Add(new ImageInCard() { CardId = cardId, ImageId = image3Id });
            await dbContext.SaveChangesAsync();
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var result = await new RefreshImagesInCards(dbContext.AsCallContext()).RunAsync(new RefreshImagesInCards.Request());
            Assert.AreEqual(3, result.TotalImageCount);
            Assert.AreEqual(1, result.TotalCardCount);
            Assert.AreEqual(3, result.ImagesInCardsCountOnStart);
            Assert.AreEqual(2, result.ImagesInCardsCountOnEnd);
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            Assert.AreEqual(2, dbContext.ImagesInCards.Count());
            var image1InCard = await dbContext.ImagesInCards.SingleAsync(imageInCard => imageInCard.ImageId == image1Id);
            Assert.AreEqual(cardId, image1InCard.CardId);
            var image2InCard = await dbContext.ImagesInCards.SingleAsync(imageInCard => imageInCard.ImageId == image2Id);
            Assert.AreEqual(cardId, image2InCard.CardId);
        }
    }
    [TestMethod()]
    public async Task CardHasImages_RefreshMustRemove_BecauseImageRenamed()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var creatorId = await UserHelper.CreateInDbAsync(testDB);
        var languageId = await CardLanguageHelper.CreateAsync(testDB);

        var image1Name = RandomHelper.String();
        var image1Id = await ImageHelper.CreateAsync(testDB, creatorId, name: image1Name);

        var image2Name = RandomHelper.String();
        var image2Id = await ImageHelper.CreateAsync(testDB, creatorId, name: image2Name);

        var request = new CreateCard.Request(
            creatorId,
            RandomHelper.String(),
            $"Back: ![Mnesios:{image1Name}]",
            $"Additional: ![Mnesios:{image2Name}] ![Mnesios:{image1Name}]",
            RandomHelper.String(),
            languageId,
            Array.Empty<Guid>(),
            creatorId.AsArray(),
            RandomHelper.String());

        using (var dbContext = new MemCheckDbContext(testDB))
            await new CreateCard(dbContext.AsCallContext()).RunAsync(request);

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var image2inDb = await dbContext.Images.SingleAsync(img => img.Id == image2Id);
            image2inDb.Name = RandomHelper.String();
            await dbContext.SaveChangesAsync();
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var result = await new RefreshImagesInCards(dbContext.AsCallContext()).RunAsync(new RefreshImagesInCards.Request());
            Assert.AreEqual(2, result.TotalImageCount);
            Assert.AreEqual(1, result.TotalCardCount);
            Assert.AreEqual(2, result.ImagesInCardsCountOnStart);
            Assert.AreEqual(1, result.ImagesInCardsCountOnEnd);
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            Assert.AreEqual(1, dbContext.ImagesInCards.Count());
            var card = await dbContext.Cards.SingleAsync();
            var imageInCard = await dbContext.ImagesInCards.SingleAsync();
            Assert.AreEqual(card.Id, imageInCard.CardId);
            Assert.AreEqual(image1Id, imageInCard.ImageId);
        }
    }
    [TestMethod()]
    public async Task CardHasImages_RefreshMustRemove_BecauseCardDeleted()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var creatorId = await UserHelper.CreateInDbAsync(testDB);

        var image1Name = RandomHelper.String();
        await ImageHelper.CreateAsync(testDB, creatorId, name: image1Name);

        var image2Name = RandomHelper.String();
        await ImageHelper.CreateAsync(testDB, creatorId, name: image2Name);

        await CardHelper.CreateAsync(testDB, creatorId, backSide: $"Back: ![Mnesios:{image1Name}]", additionalInfo: $"Additional: ![Mnesios:{image2Name}] ![Mnesios:{image1Name}]");

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var card = await dbContext.Cards.SingleAsync();
            dbContext.Cards.Remove(card);
            await dbContext.SaveChangesAsync();
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            Assert.IsFalse(dbContext.ImagesInCards.Any()); // Thanks to cascade delete

            var result = await new RefreshImagesInCards(dbContext.AsCallContext()).RunAsync(new RefreshImagesInCards.Request());
            Assert.AreEqual(2, result.TotalImageCount);
            Assert.AreEqual(0, result.TotalCardCount);
            Assert.AreEqual(0, result.ImagesInCardsCountOnStart);
            Assert.AreEqual(0, result.ImagesInCardsCountOnEnd);
        }

        using (var dbContext = new MemCheckDbContext(testDB))
            Assert.IsFalse(dbContext.ImagesInCards.Any());
    }
    [TestMethod()]
    public async Task CardHasImages_RefreshMustRemove_BecauseCardTextChanged()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var creatorId = await UserHelper.CreateInDbAsync(testDB);

        var image1Name = RandomHelper.String();
        var image1Id = await ImageHelper.CreateAsync(testDB, creatorId, name: image1Name);

        var image2Name = RandomHelper.String();
        await ImageHelper.CreateAsync(testDB, creatorId, name: image2Name);

        var cardId = await CardHelper.CreateIdAsync(testDB, creatorId, backSide: $"Back: ![Mnesios:{image1Name}]", additionalInfo: $"Additional: ![Mnesios:{image2Name}] ![Mnesios:{image1Name}]");

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            Assert.AreEqual(2, dbContext.ImagesInCards.Count());

            var card = await dbContext.Cards.SingleAsync();
            card.AdditionalInfo = "";
            await dbContext.SaveChangesAsync();
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var result = await new RefreshImagesInCards(dbContext.AsCallContext()).RunAsync(new RefreshImagesInCards.Request());
            Assert.AreEqual(2, result.TotalImageCount);
            Assert.AreEqual(1, result.TotalCardCount);
            Assert.AreEqual(2, result.ImagesInCardsCountOnStart);
            Assert.AreEqual(1, result.ImagesInCardsCountOnEnd);
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            Assert.AreEqual(1, dbContext.ImagesInCards.Count());
            var imageInCard = await dbContext.ImagesInCards.SingleAsync();
            Assert.AreEqual(cardId, imageInCard.CardId);
            Assert.AreEqual(image1Id, imageInCard.ImageId);
        }
    }
    [TestMethod()]
    public async Task ComplexCase()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var creatorId = await UserHelper.CreateInDbAsync(testDB);

        var image1Name = RandomHelper.String();
        var image1Id = await ImageHelper.CreateAsync(testDB, creatorId, name: image1Name);
        var image2Name = RandomHelper.String();
        var image2Id = await ImageHelper.CreateAsync(testDB, creatorId, name: image2Name);
        var image3Name = RandomHelper.String();
        var image3Id = await ImageHelper.CreateAsync(testDB, creatorId, name: image3Name);

        var card1Id = await CardHelper.CreateIdAsync(testDB, creatorId, frontSide: $"![Mnesios:{image1Name}]", backSide: $"![Mnesios:{image2Name}]", additionalInfo: $"![Mnesios:{image3Name}]");
        var card2Id = await CardHelper.CreateIdAsync(testDB, creatorId, backSide: $"![Mnesios:{image2Name}]", additionalInfo: $"![Mnesios:{image3Name}]");
        var card3Id = await CardHelper.CreateIdAsync(testDB, creatorId, additionalInfo: $"![Mnesios:{image3Name}]");
        var card4Id = await CardHelper.CreateIdAsync(testDB, creatorId);

        using (var dbContext = new MemCheckDbContext(testDB))
            Assert.AreEqual(6, dbContext.ImagesInCards.Count());

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var card1 = await dbContext.Cards.SingleAsync(card => card.Id == card1Id);
            card1.FrontSide = "";

            var image2 = await dbContext.Images.SingleAsync(img => img.Id == image2Id);
            var image2NewName = RandomHelper.String();
            image2.Name = image2NewName;

            var card4 = await dbContext.Cards.SingleAsync(card => card.Id == card4Id);
            card4.BackSide = $"![Mnesios:{image2NewName}] ![Mnesios:{image1Name}]";

            await dbContext.SaveChangesAsync();
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var result = await new RefreshImagesInCards(dbContext.AsCallContext()).RunAsync(new RefreshImagesInCards.Request());
            Assert.AreEqual(3, result.TotalImageCount);
            Assert.AreEqual(4, result.TotalCardCount);
            Assert.AreEqual(6, result.ImagesInCardsCountOnStart);
            Assert.AreEqual(5, result.ImagesInCardsCountOnEnd);
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            Assert.AreEqual(5, dbContext.ImagesInCards.Count());

            var imageInCard1 = await dbContext.ImagesInCards.SingleAsync(imageInCard => imageInCard.CardId == card1Id);
            Assert.AreEqual(image3Id, imageInCard1.ImageId);

            var imageInCard2 = await dbContext.ImagesInCards.SingleAsync(imageInCard => imageInCard.CardId == card2Id);
            Assert.AreEqual(image3Id, imageInCard2.ImageId);

            var imageInCard3 = await dbContext.ImagesInCards.SingleAsync(imageInCard => imageInCard.CardId == card3Id);
            Assert.AreEqual(image3Id, imageInCard3.ImageId);

            var imagesInCard4 = dbContext.ImagesInCards.Where(imageInCard => imageInCard.CardId == card4Id).ToImmutableArray();
            Assert.AreEqual(2, imagesInCard4.Length);
            Assert.AreEqual(1, imagesInCard4.Where(img => img.ImageId == image2Id).Count());
            Assert.AreEqual(1, imagesInCard4.Where(img => img.ImageId == image1Id).Count());
        }
    }
}

using MemCheck.Application.Heaping;
using MemCheck.Application.Helpers;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards;

[TestClass()]
public class GetCardsToRepeatTests
{
    [TestMethod()]
    public async Task UserNotLoggedIn()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        var request = new GetCardsToRepeat.Request(Guid.Empty, deck, Array.Empty<Guid>(), Array.Empty<Guid>(), 10);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetCardsToRepeat(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task UserDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        var request = new GetCardsToRepeat.Request(Guid.NewGuid(), deck, Array.Empty<Guid>(), Array.Empty<Guid>(), 10);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetCardsToRepeat(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task DeckDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        var request = new GetCardsToRepeat.Request(user, Guid.NewGuid(), Array.Empty<Guid>(), Array.Empty<Guid>(), 10);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetCardsToRepeat(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task UserNotOwner()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);
        var otherUser = await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        var request = new GetCardsToRepeat.Request(otherUser, deck, Array.Empty<Guid>(), Array.Empty<Guid>(), 10);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetCardsToRepeat(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task EmptyDeck()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user, algorithmId: DefaultHeapingAlgorithm.ID);

        using var dbContext = new MemCheckDbContext(db);
        var request = new GetCardsToRepeat.Request(user, deck, Array.Empty<Guid>(), Array.Empty<Guid>(), 10);
        var cards = await new GetCardsToRepeat(dbContext.AsCallContext(), new DateTime(2000, 1, 2)).RunAsync(request);
        Assert.IsFalse(cards.Cards.Any());
    }
    [TestMethod()]
    public async Task OneCardNonExpired()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user, algorithmId: DefaultHeapingAlgorithm.ID);
        var card = await CardHelper.CreateAsync(db, user);
        var addDate = RandomHelper.Date();
        await DeckHelper.AddCardAsync(db, deck, card.Id, 4, lastLearnUtcTime: addDate);

        using var dbContext = new MemCheckDbContext(db);
        var request = new GetCardsToRepeat.Request(user, deck, Array.Empty<Guid>(), Array.Empty<Guid>(), 10);
        var cards = await new GetCardsToRepeat(dbContext.AsCallContext(), addDate.AddDays(4)).RunAsync(request);
        Assert.IsFalse(cards.Cards.Any());
    }
    [TestMethod()]
    public async Task OneCardExpired()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);
        var references = RandomHelper.String();
        var card = await CardHelper.CreateAsync(db, user, references: references);
        var addDate = RandomHelper.Date();
        await DeckHelper.AddCardAsync(db, deck, card.Id, 1, addDate);

        using var dbContext = new MemCheckDbContext(db);
        var request = new GetCardsToRepeat.Request(user, deck, Array.Empty<Guid>(), Array.Empty<Guid>(), 10);
        var cards = await new GetCardsToRepeat(dbContext.AsCallContext(), addDate.AddDays(1)).RunAsync(request);
        var loadedCard = cards.Cards.Single();
        Assert.AreEqual(card.Id, loadedCard.CardId);
        Assert.AreEqual(references, loadedCard.References);
    }
    [TestMethod()]
    public async Task RequestedCount()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user, algorithmId: DeveloperHeapingAlgorithm.ID);
        var loadTime = RandomHelper.Date();
        const int cardCount = 50;
        for (var i = 0; i < cardCount; i++)
        {
            var card = await CardHelper.CreateAsync(db, user);
            await DeckHelper.AddCardAsync(db, deck, card.Id, RandomHelper.Heap(true), RandomHelper.DateBefore(loadTime));
        }
        using var dbContext = new MemCheckDbContext(db);
        var request = new GetCardsToRepeat.Request(user, deck, Array.Empty<Guid>(), Array.Empty<Guid>(), cardCount / 2);
        var cards = await new GetCardsToRepeat(dbContext.AsCallContext(), loadTime).RunAsync(request);
        Assert.AreEqual(request.CardsToDownload, cards.Cards.Count());

        request = new GetCardsToRepeat.Request(user, deck, Array.Empty<Guid>(), Array.Empty<Guid>(), cardCount);
        cards = await new GetCardsToRepeat(dbContext.AsCallContext(), loadTime).RunAsync(request);
        Assert.AreEqual(request.CardsToDownload, cards.Cards.Count());

        request = new GetCardsToRepeat.Request(user, deck, Array.Empty<Guid>(), Array.Empty<Guid>(), cardCount * 2);
        cards = await new GetCardsToRepeat(dbContext.AsCallContext(), loadTime).RunAsync(request);
        Assert.AreEqual(cardCount, cards.Cards.Count());
    }
    [TestMethod()]
    public async Task CheckOrder()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user, algorithmId: DeveloperHeapingAlgorithm.ID);
        var loadTime = RandomHelper.Date();
        const int cardCount = 100;
        for (var i = 0; i < cardCount; i++)
        {
            var card = await CardHelper.CreateAsync(db, user);
            await DeckHelper.AddCardAsync(db, deck, card.Id, RandomHelper.Heap(true), RandomHelper.DateBefore(loadTime));
        }
        using var dbContext = new MemCheckDbContext(db);
        var request = new GetCardsToRepeat.Request(user, deck, Array.Empty<Guid>(), Array.Empty<Guid>(), cardCount);
        var cards = (await new GetCardsToRepeat(dbContext.AsCallContext(), loadTime).RunAsync(request)).Cards.ToImmutableArray();
        Assert.AreEqual(cardCount, cards.Length);
        for (var i = 1; i < cards.Length; i++)
        {
            Assert.IsTrue(cards[i].Heap <= cards[i - 1].Heap);
            if (cards[i].Heap == cards[i - 1].Heap)
                Assert.IsTrue(cards[i].LastLearnUtcTime >= cards[i - 1].LastLearnUtcTime);
        }
    }
    [TestMethod()]
    public async Task OneCardWithImages()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);
        var image1 = await ImageHelper.CreateAsync(db, user);
        var image2 = await ImageHelper.CreateAsync(db, user);
        var createdCard = await CardHelper.CreateAsync(db, user, frontSideImages: new[] { image1, image2 });
        var addDate = RandomHelper.Date();
        await DeckHelper.AddCardAsync(db, deck, createdCard.Id, 1, addDate);

        using var dbContext = new MemCheckDbContext(db);
        var request = new GetCardsToRepeat.Request(user, deck, Array.Empty<Guid>(), Array.Empty<Guid>(), 10);
        var cards = await new GetCardsToRepeat(dbContext.AsCallContext(), addDate.AddDays(1)).RunAsync(request);
        var resultImages = cards.Cards.Single().Images;
        Assert.AreEqual(2, resultImages.Count());
        Assert.AreEqual(image1, resultImages.First().ImageId);
        Assert.AreEqual(image2, resultImages.Last().ImageId);
    }
    [TestMethod()]
    public async Task OneCardInNonFrench()
    {
        var db = DbHelper.GetEmptyTestDB();
        var otherLanguage = await CardLanguageHelper.CreateAsync(db, RandomHelper.String());
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);
        var card = await CardHelper.CreateAsync(db, user, language: otherLanguage);
        var addDate = RandomHelper.Date();
        await DeckHelper.AddCardAsync(db, deck, card.Id, 1, addDate);

        using var dbContext = new MemCheckDbContext(db);
        var request = new GetCardsToRepeat.Request(user, deck, Array.Empty<Guid>(), Array.Empty<Guid>(), 10);
        var cards = await new GetCardsToRepeat(dbContext.AsCallContext(), addDate.AddDays(1)).RunAsync(request);
        Assert.IsFalse(cards.Cards.Single().IsInFrench);
    }
    [TestMethod()]
    public async Task OneCardInFrench()
    {
        var db = DbHelper.GetEmptyTestDB();
        var french = await CardLanguageHelper.CreateAsync(db, "Français");
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);
        var card = await CardHelper.CreateAsync(db, user, language: french);
        var addDate = RandomHelper.Date();
        await DeckHelper.AddCardAsync(db, deck, card.Id, 1, addDate);

        using var dbContext = new MemCheckDbContext(db);
        var request = new GetCardsToRepeat.Request(user, deck, Array.Empty<Guid>(), Array.Empty<Guid>(), 10);
        var cards = await new GetCardsToRepeat(dbContext.AsCallContext(), addDate.AddDays(1)).RunAsync(request);
        Assert.IsTrue(cards.Cards.Single().IsInFrench);
    }
    [TestMethod()]
    public async Task TwoCardsWithLanguages()
    {
        var db = DbHelper.GetEmptyTestDB();
        var french = await CardLanguageHelper.CreateAsync(db, "Français");
        var otherLanguage = await CardLanguageHelper.CreateAsync(db, RandomHelper.String());
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);
        var frenchCreatedCard = await CardHelper.CreateAsync(db, user, language: french);
        var otherLanguageCard = await CardHelper.CreateAsync(db, user, language: otherLanguage);
        var addDate = RandomHelper.Date();
        await DeckHelper.AddCardAsync(db, deck, frenchCreatedCard.Id, 1, addDate);
        await DeckHelper.AddCardAsync(db, deck, otherLanguageCard.Id, 1, addDate);

        using var dbContext = new MemCheckDbContext(db);
        var request = new GetCardsToRepeat.Request(user, deck, Array.Empty<Guid>(), Array.Empty<Guid>(), 10);
        var cards = await new GetCardsToRepeat(dbContext.AsCallContext(), addDate.AddDays(1)).RunAsync(request);
        Assert.IsTrue(cards.Cards.Single(card => card.CardId == frenchCreatedCard.Id).IsInFrench);
        Assert.IsFalse(cards.Cards.Single(card => card.CardId == otherLanguageCard.Id).IsInFrench);
    }
}

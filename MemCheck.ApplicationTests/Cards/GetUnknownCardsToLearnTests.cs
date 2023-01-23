using MemCheck.Application.Heaping;
using MemCheck.Application.Helpers;
using MemCheck.Application.Notifiying;
using MemCheck.Application.QueryValidation;
using MemCheck.Basics;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards;

[TestClass()]
public class GetUnknownCardsToLearnTests
{
    [TestMethod()]
    public async Task UserNotLoggedIn()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        var request = new GetUnknownCardsToLearn.Request(Guid.Empty, deck, Array.Empty<Guid>(), 10);
        await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new GetUnknownCardsToLearn(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task UserDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        var request = new GetUnknownCardsToLearn.Request(Guid.NewGuid(), deck, Array.Empty<Guid>(), 10);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetUnknownCardsToLearn(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task DeckDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        var request = new GetUnknownCardsToLearn.Request(user, Guid.NewGuid(), Array.Empty<Guid>(), 10);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetUnknownCardsToLearn(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task UserNotOwner()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);
        var otherUser = await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        var request = new GetUnknownCardsToLearn.Request(otherUser, deck, Array.Empty<Guid>(), 10);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetUnknownCardsToLearn(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task OneCardToRepeat()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user, algorithmId: DefaultHeapingAlgorithm.ID);
        var card = await CardHelper.CreateAsync(db, user);
        await DeckHelper.AddCardAsync(db, deck, card.Id, 1, RandomHelper.Date());

        using var dbContext = new MemCheckDbContext(db);
        var request = new GetUnknownCardsToLearn.Request(user, deck, Array.Empty<Guid>(), 10);
        var cards = await new GetUnknownCardsToLearn(dbContext.AsCallContext()).RunAsync(request);
        Assert.IsFalse(cards.Cards.Any());
    }
    [TestMethod()]
    public async Task OneNeverLearnt()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user, algorithmId: DefaultHeapingAlgorithm.ID);
        var references = RandomHelper.String();
        var card = await CardHelper.CreateAsync(db, user, references: references);
        await DeckHelper.AddNeverLearntCardAsync(db, deck, card.Id);

        using var dbContext = new MemCheckDbContext(db);
        var request = new GetUnknownCardsToLearn.Request(user, deck, Array.Empty<Guid>(), 10);
        var runDate = RandomHelper.Date();
        var cards = await new GetUnknownCardsToLearn(dbContext.AsCallContext(), runDate).RunAsync(request);
        Assert.AreEqual(1, cards.Cards.Count());
        var loadedCard = cards.Cards.Single();
        Assert.AreEqual(references, loadedCard.References);
        Assert.AreEqual(CardInDeck.NeverLearntLastLearnTime, loadedCard.LastLearnUtcTime);
        Assert.AreEqual(CardInDeck.MaxHeapValue, loadedCard.MoveToHeapExpiryInfos.Length);
        for (var heapIndex = 0; heapIndex < CardInDeck.MaxHeapValue; heapIndex++)
        {
            var moveToHeapExpiryInfo = loadedCard.MoveToHeapExpiryInfos[heapIndex];
            Assert.AreEqual(heapIndex + 1, moveToHeapExpiryInfo.HeapId);
            Assert.AreEqual(runDate, moveToHeapExpiryInfo.UtcExpiryDate);
        }
    }
    [TestMethod()]
    public async Task OneLearnt()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user, algorithmId: UnitTestsHeapingAlgorithm.ID);
        var card = await CardHelper.CreateAsync(db, user);
        var lastLearnUtcTime = RandomHelper.Date();
        await DeckHelper.AddCardAsync(db, deck, card.Id, 0, lastLearnUtcTime: lastLearnUtcTime);

        using var dbContext = new MemCheckDbContext(db);
        var request = new GetUnknownCardsToLearn.Request(user, deck, Array.Empty<Guid>(), 10);
        var cards = (await new GetUnknownCardsToLearn(dbContext.AsCallContext()).RunAsync(request)).Cards;
        Assert.AreEqual(1, cards.Count());
        var cardFromResult = cards.Single();
        Assert.AreNotEqual(CardInDeck.NeverLearntLastLearnTime, cardFromResult.LastLearnUtcTime);
        Assert.AreEqual(CardInDeck.MaxHeapValue, cardFromResult.MoveToHeapExpiryInfos.Length);
        for (var heapIndex = 0; heapIndex < CardInDeck.MaxHeapValue; heapIndex++)
        {
            var moveToHeapExpiryInfo = cardFromResult.MoveToHeapExpiryInfos[heapIndex];
            Assert.AreEqual(heapIndex + 1, moveToHeapExpiryInfo.HeapId);
            Assert.AreEqual(lastLearnUtcTime.AddDays(heapIndex + 1), moveToHeapExpiryInfo.UtcExpiryDate);
        }
    }
    [TestMethod()]
    public async Task CardsNeverLearnt_NotTheSameCardsOnSuccessiveRuns()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user, algorithmId: DefaultHeapingAlgorithm.ID);
        for (var i = 0; i < 100; i++)
        {
            var card = await CardHelper.CreateAsync(db, user);
            await DeckHelper.AddNeverLearntCardAsync(db, deck, card.Id);
        }
        using var dbContext = new MemCheckDbContext(db);
        const int requestCardCount = 10;
        var request = new GetUnknownCardsToLearn.Request(user, deck, Array.Empty<Guid>(), requestCardCount);
        var firstRunCards = (await new GetUnknownCardsToLearn(dbContext.AsCallContext()).RunAsync(request)).Cards.Select(c => c.CardId).ToImmutableHashSet();
        Assert.AreEqual(requestCardCount, firstRunCards.Count);
        var secondRunCards = (await new GetUnknownCardsToLearn(dbContext.AsCallContext()).RunAsync(request)).Cards.Select(c => c.CardId).ToImmutableHashSet();
        Assert.AreEqual(requestCardCount, secondRunCards.Count);
        var thirdRunCards = (await new GetUnknownCardsToLearn(dbContext.AsCallContext()).RunAsync(request)).Cards.Select(c => c.CardId).ToImmutableHashSet();
        Assert.AreEqual(requestCardCount, thirdRunCards.Count);
        Assert.IsFalse(firstRunCards.SetEquals(secondRunCards));
        Assert.IsFalse(firstRunCards.SetEquals(thirdRunCards));
        Assert.IsFalse(secondRunCards.SetEquals(thirdRunCards));
    }
    [TestMethod()]
    public async Task CardsNeverLearnt_NotTheSameOrderOnSuccessiveRuns()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user, algorithmId: DefaultHeapingAlgorithm.ID);
        const int cardCount = 100;
        for (var i = 0; i < cardCount; i++)
        {
            var card = await CardHelper.CreateAsync(db, user);
            await DeckHelper.AddNeverLearntCardAsync(db, deck, card.Id);
        }
        using var dbContext = new MemCheckDbContext(db);
        var request = new GetUnknownCardsToLearn.Request(user, deck, Array.Empty<Guid>(), cardCount);
        var firstRunCards = (await new GetUnknownCardsToLearn(dbContext.AsCallContext()).RunAsync(request)).Cards.Select(c => c.CardId).ToImmutableArray();
        Assert.AreEqual(cardCount, firstRunCards.Length);
        var secondRunCards = (await new GetUnknownCardsToLearn(dbContext.AsCallContext()).RunAsync(request)).Cards.Select(c => c.CardId).ToImmutableArray();
        Assert.AreEqual(cardCount, secondRunCards.Length);
        var thirdRunCards = (await new GetUnknownCardsToLearn(dbContext.AsCallContext()).RunAsync(request)).Cards.Select(c => c.CardId).ToImmutableArray();
        Assert.AreEqual(cardCount, thirdRunCards.Length);
        Assert.IsFalse(firstRunCards.SequenceEqual(secondRunCards));
        Assert.IsFalse(firstRunCards.SequenceEqual(thirdRunCards));
        Assert.IsFalse(secondRunCards.SequenceEqual(thirdRunCards));
    }
    [TestMethod()]
    public async Task OneUnknownCardLearnt()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user, algorithmId: DefaultHeapingAlgorithm.ID);
        var card = await CardHelper.CreateAsync(db, user);
        await DeckHelper.AddCardAsync(db, deck, card.Id, 0);

        using var dbContext = new MemCheckDbContext(db);
        var request = new GetUnknownCardsToLearn.Request(user, deck, Array.Empty<Guid>(), 10);
        var cards = await new GetUnknownCardsToLearn(dbContext.AsCallContext()).RunAsync(request);
        Assert.AreEqual(1, cards.Cards.Count());
    }
    [TestMethod()]
    public async Task UnknownCardsLearnt_CheckOrder()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user, algorithmId: DeveloperHeapingAlgorithm.ID);
        const int cardCount = 100;
        for (var i = 0; i < cardCount; i++)
        {
            var card = await CardHelper.CreateAsync(db, user);
            await DeckHelper.AddCardAsync(db, deck, card.Id, 0);
        }
        using var dbContext = new MemCheckDbContext(db);
        var request = new GetUnknownCardsToLearn.Request(user, deck, Array.Empty<Guid>(), cardCount);
        var cards = (await new GetUnknownCardsToLearn(dbContext.AsCallContext()).RunAsync(request)).Cards.ToImmutableArray();
        Assert.AreEqual(cardCount, cards.Length);
        for (var i = 1; i < cards.Length; i++)
            Assert.IsTrue(cards[i].LastLearnUtcTime >= cards[i - 1].LastLearnUtcTime);
    }
    [TestMethod()]
    public async Task ComplexCase()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user, algorithmId: DeveloperHeapingAlgorithm.ID);
        const int cardCount = 100;
        for (var i = 0; i < cardCount; i++)
        {
            await DeckHelper.AddNeverLearntCardAsync(db, deck, await CardHelper.CreateIdAsync(db, user));
            await DeckHelper.AddCardAsync(db, deck, await CardHelper.CreateIdAsync(db, user), 0);
        }
        using var dbContext = new MemCheckDbContext(db);
        var request = new GetUnknownCardsToLearn.Request(user, deck, Array.Empty<Guid>(), cardCount);
        var cards = (await new GetUnknownCardsToLearn(dbContext.AsCallContext()).RunAsync(request)).Cards.ToImmutableArray();
        Assert.AreEqual(cardCount, cards.Length);
        for (var i = 1; i < cardCount / 2; i++)
            Assert.AreEqual(CardInDeck.NeverLearntLastLearnTime, cards[i].LastLearnUtcTime);
        for (var i = cardCount / 2; i < cards.Length; i++)
            Assert.IsTrue(cards[i].LastLearnUtcTime >= cards[i - 1].LastLearnUtcTime);
    }
    [TestMethod()]
    public async Task ComplexCaseWithLessCardsThanRequested()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user, algorithmId: DeveloperHeapingAlgorithm.ID);
        const int cardCount = 10;
        for (var i = 0; i < cardCount / 2; i++)
        {
            await DeckHelper.AddNeverLearntCardAsync(db, deck, await CardHelper.CreateIdAsync(db, user));
            await DeckHelper.AddCardAsync(db, deck, await CardHelper.CreateIdAsync(db, user), 0);
        }
        using var dbContext = new MemCheckDbContext(db);
        var request = new GetUnknownCardsToLearn.Request(user, deck, Array.Empty<Guid>(), cardCount * 2);
        var cards = (await new GetUnknownCardsToLearn(dbContext.AsCallContext()).RunAsync(request)).Cards.ToImmutableArray();
        Assert.AreEqual(cardCount, cards.Length);
        for (var i = 1; i < cardCount / 2; i++)
            Assert.AreEqual(CardInDeck.NeverLearntLastLearnTime, cards[i].LastLearnUtcTime);
        for (var i = cardCount / 2; i < cards.Length; i++)
            Assert.IsTrue(cards[i].LastLearnUtcTime >= cards[i - 1].LastLearnUtcTime);
    }
    [TestMethod()]
    public async Task OrderingOfNeverLearnt()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user, algorithmId: DefaultHeapingAlgorithm.ID);
        var randomDate = RandomHelper.Date();
        var cardAddedLater = await CardHelper.CreateAsync(db, user);
        await DeckHelper.AddNeverLearntCardAsync(db, deck, cardAddedLater.Id, randomDate.AddDays(1));
        for (var i = 0; i < 9; i++)
        {
            var card = await CardHelper.CreateAsync(db, user);
            await DeckHelper.AddNeverLearntCardAsync(db, deck, card.Id, randomDate);
        }

        using var dbContext = new MemCheckDbContext(db);
        var request = new GetUnknownCardsToLearn.Request(user, deck, Array.Empty<Guid>(), 3);
        var downloadedCards = (await new GetUnknownCardsToLearn(dbContext.AsCallContext()).RunAsync(request)).Cards.Select(c => c.CardId).ToImmutableHashSet();
        Assert.IsFalse(downloadedCards.Contains(cardAddedLater.Id));
    }
    [TestMethod()]
    public async Task OneCardInNonFrench()
    {
        var db = DbHelper.GetEmptyTestDB();
        var otherLanguage = await CardLanguageHelper.CreateAsync(db, RandomHelper.String());
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);
        var card = await CardHelper.CreateAsync(db, user, language: otherLanguage);
        await DeckHelper.AddNeverLearntCardAsync(db, deck, card.Id);

        using var dbContext = new MemCheckDbContext(db);
        var request = new GetUnknownCardsToLearn.Request(user, deck, Array.Empty<Guid>(), 10);
        var cards = await new GetUnknownCardsToLearn(dbContext.AsCallContext()).RunAsync(request);
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
        await DeckHelper.AddNeverLearntCardAsync(db, deck, card.Id);

        using var dbContext = new MemCheckDbContext(db);
        var request = new GetUnknownCardsToLearn.Request(user, deck, Array.Empty<Guid>(), 10);
        var cards = await new GetUnknownCardsToLearn(dbContext.AsCallContext()).RunAsync(request);
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
        await DeckHelper.AddNeverLearntCardAsync(db, deck, frenchCreatedCard.Id);
        await DeckHelper.AddNeverLearntCardAsync(db, deck, otherLanguageCard.Id);

        using var dbContext = new MemCheckDbContext(db);
        var request = new GetUnknownCardsToLearn.Request(user, deck, Array.Empty<Guid>(), 10);
        var cards = await new GetUnknownCardsToLearn(dbContext.AsCallContext()).RunAsync(request);
        Assert.IsTrue(cards.Cards.Single(card => card.CardId == frenchCreatedCard.Id).IsInFrench);
        Assert.IsFalse(cards.Cards.Single(card => card.CardId == otherLanguageCard.Id).IsInFrench);
    }
    [TestMethod()]
    public async Task TwoCardsWithCheckingOfAllFields()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user.Id, algorithmId: UnitTestsHeapingAlgorithm.ID);
        var french = await CardLanguageHelper.CreateAsync(db, "Français");
        var otherLanguage = await CardLanguageHelper.CreateAsync(db);
        var tagName = RandomHelper.String();
        var tag = await TagHelper.CreateAsync(db, tagName);

        var card1VersionDate = RandomHelper.Date();
        var card1 = await CardHelper.CreateAsync(db, user.Id, versionDate: card1VersionDate, language: french, tagIds: tag.AsArray(), userWithViewIds: user.Id.AsArray());
        var card1AddToDeckTime = RandomHelper.Date(card1VersionDate);
        var card1LastLearnTime = CardInDeck.NeverLearntLastLearnTime;
        var card1BiggestHeapReached = RandomHelper.Heap();
        var card1NbTimesInNotLearnedHeap = RandomHelper.Int(CardInDeck.MaxHeapValue);
        await DeckHelper.AddCardAsync(db, deck, card1.Id, lastLearnUtcTime: card1LastLearnTime, heap: 0, addToDeckUtcTime: card1AddToDeckTime, biggestHeapReached: card1BiggestHeapReached, nbTimesInNotLearnedHeap: card1NbTimesInNotLearnedHeap);
        var card1Rating = RandomHelper.Rating();
        await RatingHelper.RecordForUserAsync(db, user.Id, card1.Id, card1Rating);

        var card2VersionDate = RandomHelper.Date();
        var card2 = await CardHelper.CreateAsync(db, user.Id, versionDate: card2VersionDate, language: otherLanguage);
        var card2AddToDeckTime = RandomHelper.Date(card2VersionDate);
        var card2LastLearnTime = RandomHelper.Date(card2AddToDeckTime);
        var card2BiggestHeapReached = RandomHelper.Heap();
        var card2NbTimesInNotLearnedHeap = RandomHelper.Int(CardInDeck.MaxHeapValue);
        await DeckHelper.AddCardAsync(db, deck, card2.Id, lastLearnUtcTime: card2LastLearnTime, heap: 0, addToDeckUtcTime: card2AddToDeckTime, biggestHeapReached: card2BiggestHeapReached, nbTimesInNotLearnedHeap: card2NbTimesInNotLearnedHeap);
        using (var dbContext = new MemCheckDbContext(db))
            await new AddCardSubscriptions(dbContext.AsCallContext()).RunAsync(new AddCardSubscriptions.Request(user.Id, card2.Id.AsArray()));

        using (var dbContext = new MemCheckDbContext(db))
        {
            var request = new GetUnknownCardsToLearn.Request(user.Id, deck, Array.Empty<Guid>(), 10);
            var runDate = RandomHelper.Date();
            var result = (await new GetUnknownCardsToLearn(dbContext.AsCallContext(), runDate).RunAsync(request)).Cards;
            Assert.AreEqual(2, result.Count());

            {
                var card1FromResult = result.Single(card => card.CardId == card1.Id);
                Assert.AreEqual(card1LastLearnTime, card1FromResult.LastLearnUtcTime);
                Assert.AreEqual(card1VersionDate, card1FromResult.LastChangeUtcTime);
                Assert.AreEqual(card1AddToDeckTime, card1FromResult.AddToDeckUtcTime);
                Assert.AreEqual(card1BiggestHeapReached, card1FromResult.BiggestHeapReached);
                Assert.AreEqual(card1NbTimesInNotLearnedHeap, card1FromResult.NbTimesInNotLearnedHeap);
                Assert.AreEqual(card1.FrontSide, card1FromResult.FrontSide);
                Assert.AreEqual(card1.BackSide, card1FromResult.BackSide);
                Assert.AreEqual(card1.AdditionalInfo, card1FromResult.AdditionalInfo);
                Assert.AreEqual(card1.References, card1FromResult.References);
                Assert.AreEqual(user.UserName, card1FromResult.Owner);
                Assert.AreEqual(card1Rating, card1FromResult.UserRating);
                Assert.AreEqual(card1Rating, card1FromResult.AverageRating);
                Assert.AreEqual(1, card1FromResult.CountOfUserRatings);
                Assert.IsFalse(card1FromResult.RegisteredForNotifications);
                Assert.IsTrue(card1FromResult.IsInFrench);
                Assert.AreEqual(1, card1FromResult.Tags.Count());
                Assert.AreEqual(tagName, card1FromResult.Tags.Single());
                Assert.AreEqual(1, card1FromResult.VisibleTo.Count());
                Assert.AreEqual(user.UserName, card1FromResult.VisibleTo.Single());
                Assert.AreEqual(CardInDeck.MaxHeapValue, card1FromResult.MoveToHeapExpiryInfos.Length);
                for (var heapIndex = 0; heapIndex < CardInDeck.MaxHeapValue; heapIndex++)
                {
                    Assert.AreEqual(heapIndex + 1, card1FromResult.MoveToHeapExpiryInfos[heapIndex].HeapId);
                    Assert.AreEqual(runDate, card1FromResult.MoveToHeapExpiryInfos[heapIndex].UtcExpiryDate);
                }
            }
            {
                var card2FromResult = result.Single(card => card.CardId == card2.Id);
                Assert.AreEqual(card2LastLearnTime, card2FromResult.LastLearnUtcTime);
                Assert.AreEqual(card2VersionDate, card2FromResult.LastChangeUtcTime);
                Assert.AreEqual(card2AddToDeckTime, card2FromResult.AddToDeckUtcTime);
                Assert.AreEqual(card2BiggestHeapReached, card2FromResult.BiggestHeapReached);
                Assert.AreEqual(card2NbTimesInNotLearnedHeap, card2FromResult.NbTimesInNotLearnedHeap);
                Assert.AreEqual(card2.FrontSide, card2FromResult.FrontSide);
                Assert.AreEqual(card2.BackSide, card2FromResult.BackSide);
                Assert.AreEqual(card2.AdditionalInfo, card2FromResult.AdditionalInfo);
                Assert.AreEqual(card2.References, card2FromResult.References);
                Assert.AreEqual(user.UserName, card2FromResult.Owner);
                Assert.AreEqual(0, card2FromResult.UserRating);
                Assert.AreEqual(0, card2FromResult.AverageRating);
                Assert.AreEqual(0, card2FromResult.CountOfUserRatings);
                Assert.IsTrue(card2FromResult.RegisteredForNotifications);
                Assert.IsFalse(card2FromResult.IsInFrench);
                Assert.IsFalse(card2FromResult.Tags.Any());
                Assert.IsFalse(card2FromResult.VisibleTo.Any());
                Assert.AreEqual(CardInDeck.MaxHeapValue, card2FromResult.MoveToHeapExpiryInfos.Length);
                for (var heapIndex = 0; heapIndex < CardInDeck.MaxHeapValue; heapIndex++)
                {
                    Assert.AreEqual(heapIndex + 1, card2FromResult.MoveToHeapExpiryInfos[heapIndex].HeapId);
                    Assert.AreEqual(card2LastLearnTime.AddDays(heapIndex + 1), card2FromResult.MoveToHeapExpiryInfos[heapIndex].UtcExpiryDate);
                }
            }
        }
    }
}

﻿using MemCheck.Application.Heaping;
using MemCheck.Application.Helpers;
using MemCheck.Application.Notifiying;
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
public class GetCardsToRepeatTests
{
    [TestMethod()]
    public async Task UserNotLoggedIn()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        var request = new GetCardsToRepeat.Request(Guid.Empty, deck, Array.Empty<Guid>(), 10);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetCardsToRepeat(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task UserDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        var request = new GetCardsToRepeat.Request(Guid.NewGuid(), deck, Array.Empty<Guid>(), 10);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetCardsToRepeat(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task DeckDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);

        using var dbContext = new MemCheckDbContext(db);
        var request = new GetCardsToRepeat.Request(user, Guid.NewGuid(), Array.Empty<Guid>(), 10);
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
        var request = new GetCardsToRepeat.Request(otherUser, deck, Array.Empty<Guid>(), 10);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetCardsToRepeat(dbContext.AsCallContext()).RunAsync(request));
    }
    [TestMethod()]
    public async Task EmptyDeck()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user, algorithmId: DefaultHeapingAlgorithm.ID);

        using var dbContext = new MemCheckDbContext(db);
        var request = new GetCardsToRepeat.Request(user, deck, Array.Empty<Guid>(), 10);
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
        var request = new GetCardsToRepeat.Request(user, deck, Array.Empty<Guid>(), 10);
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
        var request = new GetCardsToRepeat.Request(user, deck, Array.Empty<Guid>(), 10);
        var cards = await new GetCardsToRepeat(dbContext.AsCallContext(), addDate.AddDays(1)).RunAsync(request);
        var loadedCard = cards.Cards.Single();
        Assert.AreEqual(card.Id, loadedCard.CardId);
        Assert.AreEqual(references, loadedCard.References);
        Assert.IsNull(loadedCard.LatestDiscussionEntryCreationUtcDate);
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
        var request = new GetCardsToRepeat.Request(user, deck, Array.Empty<Guid>(), cardCount / 2);
        var cards = await new GetCardsToRepeat(dbContext.AsCallContext(), loadTime).RunAsync(request);
        Assert.AreEqual(request.CardsToDownload, cards.Cards.Count());

        request = new GetCardsToRepeat.Request(user, deck, Array.Empty<Guid>(), cardCount);
        cards = await new GetCardsToRepeat(dbContext.AsCallContext(), loadTime).RunAsync(request);
        Assert.AreEqual(request.CardsToDownload, cards.Cards.Count());

        request = new GetCardsToRepeat.Request(user, deck, Array.Empty<Guid>(), cardCount * 2);
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
        var request = new GetCardsToRepeat.Request(user, deck, Array.Empty<Guid>(), cardCount);
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
        var request = new GetCardsToRepeat.Request(user, deck, Array.Empty<Guid>(), 10);
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
        var request = new GetCardsToRepeat.Request(user, deck, Array.Empty<Guid>(), 10);
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
        var request = new GetCardsToRepeat.Request(user, deck, Array.Empty<Guid>(), 10);
        var cards = await new GetCardsToRepeat(dbContext.AsCallContext(), addDate.AddDays(1)).RunAsync(request);
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
        var tag = await TagHelper.CreateAsync(db, user, tagName);

        var card1VersionDate = RandomHelper.Date();
        var card1 = await CardHelper.CreateAsync(db, user.Id, versionDate: card1VersionDate, language: french, tagIds: tag.AsArray(), userWithViewIds: user.Id.AsArray());
        var card1AddToDeckTime = RandomHelper.Date(card1VersionDate);
        var card1LastLearnTime = RandomHelper.Date(card1AddToDeckTime);
        var card1BiggestHeapReached = RandomHelper.Heap();
        var card1NbTimesInNotLearnedHeap = RandomHelper.Int(CardInDeck.MaxHeapValue);
        var card1Heap = RandomHelper.Heap(true);
        await DeckHelper.AddCardAsync(db, deck, card1.Id, lastLearnUtcTime: card1LastLearnTime, heap: card1Heap, addToDeckUtcTime: card1AddToDeckTime, biggestHeapReached: card1BiggestHeapReached, nbTimesInNotLearnedHeap: card1NbTimesInNotLearnedHeap);
        var card1Rating = RandomHelper.Rating();
        await RatingHelper.RecordForUserAsync(db, user.Id, card1.Id, card1Rating);
        var runDateFromCard1 = RandomHelper.Date(card1LastLearnTime.AddDays(card1Heap + 1));
        var card1LatestDiscussionEntryCreationDate = RandomHelper.Date(card1AddToDeckTime);
        using (var dbContext = new MemCheckDbContext(db))
            await new AddEntryToCardDiscussion(dbContext.AsCallContext(), card1LatestDiscussionEntryCreationDate).RunAsync(new AddEntryToCardDiscussion.Request(user.Id, card1.Id, RandomHelper.String()));

        var card2VersionDate = RandomHelper.Date();
        var card2 = await CardHelper.CreateAsync(db, user.Id, versionDate: card2VersionDate, language: otherLanguage);
        var card2AddToDeckTime = RandomHelper.Date(card2VersionDate);
        var card2LastLearnTime = RandomHelper.Date(card2AddToDeckTime);
        var card2BiggestHeapReached = RandomHelper.Heap();
        var card2NbTimesInNotLearnedHeap = RandomHelper.Int(CardInDeck.MaxHeapValue);
        var card2Heap = RandomHelper.Heap(true);
        await DeckHelper.AddCardAsync(db, deck, card2.Id, lastLearnUtcTime: card2LastLearnTime, heap: card2Heap, addToDeckUtcTime: card2AddToDeckTime, biggestHeapReached: card2BiggestHeapReached, nbTimesInNotLearnedHeap: card2NbTimesInNotLearnedHeap);
        using (var dbContext = new MemCheckDbContext(db))
            await new AddCardSubscriptions(dbContext.AsCallContext()).RunAsync(new AddCardSubscriptions.Request(user.Id, card2.Id.AsArray()));
        var runDateFromCard2 = RandomHelper.Date(card2LastLearnTime.AddDays(card2Heap + 1));
        var card2LatestDiscussionEntryCreationDate = RandomHelper.Date(card2AddToDeckTime);
        using (var dbContext = new MemCheckDbContext(db))
            await new AddEntryToCardDiscussion(dbContext.AsCallContext(), card2LatestDiscussionEntryCreationDate).RunAsync(new AddEntryToCardDiscussion.Request(user.Id, card2.Id, RandomHelper.String()));
        card2LatestDiscussionEntryCreationDate = RandomHelper.Date(card2LatestDiscussionEntryCreationDate);
        using (var dbContext = new MemCheckDbContext(db))
            await new AddEntryToCardDiscussion(dbContext.AsCallContext(), card2LatestDiscussionEntryCreationDate).RunAsync(new AddEntryToCardDiscussion.Request(user.Id, card2.Id, RandomHelper.String()));

        using (var dbContext = new MemCheckDbContext(db))
        {
            var request = new GetCardsToRepeat.Request(user.Id, deck, Array.Empty<Guid>(), 10);
            var runDate = runDateFromCard1 > runDateFromCard2 ? runDateFromCard1 : runDateFromCard2;
            var result = (await new GetCardsToRepeat(dbContext.AsCallContext(), runDate).RunAsync(request)).Cards;
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
                Assert.AreEqual(card1LatestDiscussionEntryCreationDate, card1FromResult.LatestDiscussionEntryCreationUtcDate);
                for (var heapIndex = 0; heapIndex < CardInDeck.MaxHeapValue; heapIndex++)
                {
                    Assert.AreEqual(heapIndex, card1FromResult.MoveToHeapExpiryInfos[heapIndex].HeapId);
                    Assert.AreEqual(heapIndex == 0 ? CardInDeck.NeverLearntLastLearnTime : card1LastLearnTime.AddDays(heapIndex), card1FromResult.MoveToHeapExpiryInfos[heapIndex].UtcExpiryDate);
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
                Assert.AreEqual(card2LatestDiscussionEntryCreationDate, card2FromResult.LatestDiscussionEntryCreationUtcDate);
                for (var heapIndex = 0; heapIndex < CardInDeck.MaxHeapValue; heapIndex++)
                {
                    Assert.AreEqual(heapIndex, card2FromResult.MoveToHeapExpiryInfos[heapIndex].HeapId);
                    Assert.AreEqual(heapIndex == 0 ? CardInDeck.NeverLearntLastLearnTime : card2LastLearnTime.AddDays(heapIndex), card2FromResult.MoveToHeapExpiryInfos[heapIndex].UtcExpiryDate);
                }
            }
        }
    }
}

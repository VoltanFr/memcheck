﻿using MemCheck.Application.Heaping;
using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Helpers;

public static class DeckHelper
{
    public static async Task<Guid> CreateAsync(DbContextOptions<MemCheckDbContext> testDB, Guid ownerId, string? description = null, int algorithmId = UnitTestsHeapingAlgorithm.ID)
    {
        using var dbContext = new MemCheckDbContext(testDB);
        var result = new Deck
        {
            Owner = await dbContext.Users.SingleAsync(u => u.Id == ownerId),
            Description = description ?? RandomHelper.String(),
            CardInDecks = Array.Empty<CardInDeck>(),
            HeapingAlgorithmId = algorithmId
        };
        dbContext.Decks.Add(result);
        await dbContext.SaveChangesAsync();
        return result.Id;
    }
    public static async Task<Guid> CreateAsync(DbContextOptions<MemCheckDbContext> testDB, MemCheckUser owner, string? description = null, int algorithmId = UnitTestsHeapingAlgorithm.ID)
    {
        return await CreateAsync(testDB, owner.Id, description, algorithmId);
    }
    public static async Task AddCardAsync(DbContextOptions<MemCheckDbContext> testDB, Guid deckId, Guid cardId, int? heap = null, DateTime? lastLearnUtcTime = null, DateTime? addToDeckUtcTime = null, int? biggestHeapReached = null, int nbTimesInNotLearnedHeap = 1)
    {
        heap ??= RandomHelper.Heap();
        lastLearnUtcTime ??= RandomHelper.Date();

        using var dbContext = new MemCheckDbContext(testDB);

        var deck = await dbContext.Decks.AsNoTracking().Include(deck => deck.Owner).SingleAsync(d => d.Id == deckId);
        CardVisibilityHelper.CheckUserIsAllowedToViewCard(dbContext, deck.Owner.Id, cardId);

        DateTime expiryTime;
        if (heap.Value != CardInDeck.UnknownHeap)
        {
            var heapingAlgo = await HeapingAlgorithm.OfDeckAsync(dbContext, deckId);
            expiryTime = heapingAlgo.ExpiryUtcDate(heap.Value, lastLearnUtcTime.Value);
        }
        else
            expiryTime = DateTime.MinValue;

        var cardForUser = new CardInDeck()
        {
            CardId = cardId,
            DeckId = deckId,
            CurrentHeap = heap.Value,
            LastLearnUtcTime = lastLearnUtcTime.Value,
            ExpiryUtcTime = expiryTime,
            AddToDeckUtcTime = addToDeckUtcTime ?? DateTime.UtcNow,
            NbTimesInNotLearnedHeap = nbTimesInNotLearnedHeap,
            BiggestHeapReached = biggestHeapReached != null ? biggestHeapReached.Value : heap.Value
        };
        dbContext.CardsInDecks.Add(cardForUser);
        await dbContext.SaveChangesAsync();
    }
    public static async Task AddNewCardAsync(DbContextOptions<MemCheckDbContext> testDB, Guid deck, int? heap = null, DateTime? lastLearnUtcTime = null, DateTime? addToDeckUtcTime = null, IEnumerable<Guid>? tagIds = null)
    {
        Guid owner;
        using (var dbContext = new MemCheckDbContext(testDB))
            owner = (await dbContext.Decks.Include(d => d.Owner).SingleAsync(d => d.Id == deck)).Owner.Id;

        var card = await CardHelper.CreateIdAsync(testDB, owner, tagIds: tagIds);

        await AddCardAsync(testDB, deck, card, heap, lastLearnUtcTime, addToDeckUtcTime);
    }
    public static async Task AddNeverLearntCardAsync(DbContextOptions<MemCheckDbContext> testDB, Guid deck, Guid card, DateTime? addToDeckUtcTime = null)
    {
        await AddCardAsync(testDB, deck, card, 0, CardInDeck.NeverLearntLastLearnTime, addToDeckUtcTime);
    }
    public static async Task CheckDeckContainsCards(DbContextOptions<MemCheckDbContext> testDB, Guid deck, params Guid[] cards)
    {
        using var dbContext = new MemCheckDbContext(testDB);
        var matchingCardsInDeckCount = await dbContext.CardsInDecks.AsNoTracking().CountAsync(cardInDeck => cardInDeck.DeckId == deck && cards.Contains(cardInDeck.CardId));
        Assert.AreEqual(cards.Length, matchingCardsInDeckCount);
    }
    public static async Task CheckDeckDoesNotContainCard(DbContextOptions<MemCheckDbContext> testDB, Guid deck, Guid card)
    {
        using var dbContext = new MemCheckDbContext(testDB);
        Assert.IsFalse(await dbContext.CardsInDecks.AsNoTracking().AnyAsync(cardInDeck => cardInDeck.DeckId == deck && card == cardInDeck.CardId));
    }
    public static async Task<Guid> GetUserSingleDeckAndSetTestHeapingAlgoAsync(DbContextOptions<MemCheckDbContext> testDB, Guid userId, int algorithmId = UnitTestsHeapingAlgorithm.ID)
    {
        using var dbContext = new MemCheckDbContext(testDB);
        var deck = dbContext.Decks.Single(deck => deck.Owner.Id == userId);
        deck.HeapingAlgorithmId = algorithmId;
        await dbContext.SaveChangesAsync();
        return deck.Id;
    }
}

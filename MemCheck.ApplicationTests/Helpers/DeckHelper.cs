﻿using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Tests.Helpers
{
    public static class DeckHelper
    {
        public static async Task<Guid> CreateAsync(DbContextOptions<MemCheckDbContext> testDB, Guid ownerId, string? description = null, int? algorithmId = null)
        {
            using var dbContext = new MemCheckDbContext(testDB);
            var result = new Deck
            {
                Owner = await dbContext.Users.SingleAsync(u => u.Id == ownerId),
                Description = description ?? RandomHelper.String(),
                CardInDecks = Array.Empty<CardInDeck>(),
                HeapingAlgorithmId = algorithmId == null ? RandomHelper.HeapingAlgorithm() : algorithmId.Value
            };
            dbContext.Decks.Add(result);
            await dbContext.SaveChangesAsync();
            return result.Id;
        }
        public static async Task AddCardAsync(DbContextOptions<MemCheckDbContext> testDB, Guid deck, Guid card, int? heap = null, DateTime? lastLearnUtcTime = null)
        {
            using var dbContext = new MemCheckDbContext(testDB);
            var cardForUser = new CardInDeck()
            {
                CardId = card,
                DeckId = deck,
                CurrentHeap = heap ?? RandomHelper.Heap(),
                LastLearnUtcTime = lastLearnUtcTime == null ? DateHelper.Random() : lastLearnUtcTime.Value,
                AddToDeckUtcTime = DateTime.UtcNow,
                NbTimesInNotLearnedHeap = 1,
                BiggestHeapReached = 0
            };
            dbContext.CardsInDecks.Add(cardForUser);
            await dbContext.SaveChangesAsync();
        }
        public static async Task AddNeverLearntCardAsync(DbContextOptions<MemCheckDbContext> testDB, Guid deck, Guid card)
        {
            await AddCardAsync(testDB, deck, card, 0, CardInDeck.NeverLearntLastLearnTime);
        }
        public static async Task CheckDeckContainsCards(DbContextOptions<MemCheckDbContext> testDB, Guid deck, params Guid[] cards)
        {
            using var dbContext = new MemCheckDbContext(testDB);
            var matchingCardsInDeckCount = await dbContext.CardsInDecks.AsNoTracking().CountAsync(cardInDeck => cardInDeck.DeckId == deck && cards.Contains(cardInDeck.CardId));
            Assert.AreEqual(cards.Length, matchingCardsInDeckCount);
        }
    }
}

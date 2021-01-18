﻿using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Tests.Helpers
{
    public static class DeckHelper
    {
        public static async Task<Guid> CreateAsync(DbContextOptions<MemCheckDbContext> testDB, Guid ownerId, string? description = null)
        {
            using var dbContext = new MemCheckDbContext(testDB);
            var result = new Deck
            {
                Owner = await dbContext.Users.SingleAsync(u => u.Id == ownerId),
                Description = description ?? StringHelper.RandomString(),
                CardInDecks = Array.Empty<CardInDeck>(),
                HeapingAlgorithmId = Deck.DefaultHeapingAlgorithmId
            };
            dbContext.Decks.Add(result);
            await dbContext.SaveChangesAsync();
            return result.Id;
        }
        public static async Task AddCardAsync(DbContextOptions<MemCheckDbContext> testDB, Guid user, Guid deck, Guid card, int heap, DateTime? lastLearnUtcTime = null)
        {
            using var dbContext = new MemCheckDbContext(testDB);
            await new AddCardInDeck(dbContext).RunAsync(deck, card);

            //We can't move a card up more than one heap at a time, by security in MoveCardToHeap
            for (int i = 1; i <= heap; i++)
                await new MoveCardToHeap(dbContext).RunAsync(new MoveCardToHeap.Request(user, deck, card, i, false), lastLearnUtcTime);
        }
    }
}

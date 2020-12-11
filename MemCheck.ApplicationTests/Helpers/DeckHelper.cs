using Microsoft.EntityFrameworkCore;
using MemCheck.Database;
using MemCheck.Domain;
using System.Threading.Tasks;
using System;

namespace MemCheck.Application.Tests.Helpers
{
    public static class DeckHelper
    {
        public static async Task<Guid> CreateAsync(DbContextOptions<MemCheckDbContext> testDB, Guid ownerId, string? description = null)
        {
            using var dbContext = new MemCheckDbContext(testDB);
            var result = new Deck();
            result.Owner = await dbContext.Users.SingleAsync(u => u.Id == ownerId);
            result.Description = description ?? StringServices.RandomString();
            result.CardInDecks = new CardInDeck[0];
            result.HeapingAlgorithmId = Deck.DefaultHeapingAlgorithmId;
            dbContext.Decks.Add(result);
            await dbContext.SaveChangesAsync();
            return result.Id;
        }
    }
}

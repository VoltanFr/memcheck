using MemCheck.Application.Cards;
using MemCheck.Basics;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Tests.Helpers
{
    public static class CardDeletionHelper
    {
        public static async Task DeleteCardAsync(DbContextOptions<MemCheckDbContext> db, Guid userId, Guid cardId, DateTime? deletionDate = null)
        {
            using var dbContext = new MemCheckDbContext(db);
            var deleter = new DeleteCards(dbContext, new TestLocalizer());
            var deletionRequest = new DeleteCards.Request(userId, cardId.AsArray());
            await deleter.RunAsync(deletionRequest, deletionDate);
        }
    }
}

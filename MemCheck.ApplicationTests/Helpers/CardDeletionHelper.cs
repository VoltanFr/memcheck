using Microsoft.EntityFrameworkCore;
using MemCheck.Database;
using System;
using System.Threading.Tasks;
using MemCheck.Application.CardChanging;

namespace MemCheck.Application.Tests.Helpers
{
    public static class CardDeletionHelper
    {
        public static async Task DeleteCardAsync(DbContextOptions<MemCheckDbContext> db, Guid userId, Guid cardId, DateTime deletionDate)
        {
            using var dbContext = new MemCheckDbContext(db);
            var deleter = new DeleteCards(dbContext, new TestLocalizer());
            var deletionRequest = new DeleteCards.Request(userId, new[] { cardId });
            await deleter.RunAsync(deletionRequest, deletionDate);
        }
    }
}

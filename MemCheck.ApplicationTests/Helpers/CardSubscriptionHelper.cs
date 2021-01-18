using Microsoft.EntityFrameworkCore;
using MemCheck.Database;
using System;
using MemCheck.Domain;
using System.Threading.Tasks;

namespace MemCheck.Application.Tests.Helpers
{
    public static class CardSubscriptionHelper
    {
        public static async Task CreateAsync(DbContextOptions<MemCheckDbContext> testDB, Guid subscriberId, Guid cardId, DateTime? lastNotificationDate = null)
        {
            using var dbContext = new MemCheckDbContext(testDB);
            var notif = new CardNotificationSubscription { CardId = cardId, UserId = subscriberId };
            if (lastNotificationDate != null)
                notif.LastNotificationUtcDate = lastNotificationDate.Value;
            dbContext.CardNotifications.Add(notif);
            await dbContext.SaveChangesAsync();
        }
        public static async Task<bool> UserIsSubscribedToCardAsync(DbContextOptions<MemCheckDbContext> db, Guid userId, Guid cardId)
        {
            using var dbContext = new MemCheckDbContext(db);
            return await dbContext.CardNotifications.AnyAsync(cardSubscription => cardSubscription.CardId == cardId && cardSubscription.UserId == userId);
        }
    }
}

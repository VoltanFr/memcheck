using Microsoft.EntityFrameworkCore;
using MemCheck.Database;
using System;
using MemCheck.Domain;
using System.Threading.Tasks;

namespace MemCheck.Application.Tests.Notifying
{
    public static class CardSubscriptionHelper
    {
        public static async Task CreateAsync(DbContextOptions<MemCheckDbContext> testDB, Guid subscriberId, Guid cardId, DateTime lastNotificationDate)
        {
            using var dbContext = new MemCheckDbContext(testDB);
            var notif = new CardNotificationSubscription();
            notif.CardId = cardId;
            notif.UserId = subscriberId;
            notif.LastNotificationUtcDate = lastNotificationDate;
            dbContext.CardNotifications.Add(notif);
            await dbContext.SaveChangesAsync();
        }
    }
}

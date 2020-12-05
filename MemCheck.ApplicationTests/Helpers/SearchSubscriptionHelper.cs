using Microsoft.EntityFrameworkCore;
using MemCheck.Database;
using System;
using MemCheck.Domain;
using System.Threading.Tasks;

namespace MemCheck.Application.Tests.Helpers
{
    public static class SearchSubscriptionHelper
    {
        public static async Task CreateAsync(DbContextOptions<MemCheckDbContext> testDB, Guid subscriberId, Guid? excludedDeckId = null, DateTime? lastNotificationDate = null)
        {
            using var dbContext = new MemCheckDbContext(testDB);
            var subscription = new SearchSubscription();
            subscription.UserId = subscriberId;
            subscription.ExcludedDeck = excludedDeckId == null ? Guid.Empty : excludedDeckId.Value;
            subscription.RequiredText = "";
            subscription.RequiredTags = new RequiredTagInSearchSubscription[0];
            subscription.excludeAllTags = false;
            subscription.ExcludedTags = new ExcludedTagInSearchSubscription[0];
            subscription.LastNotificationUtcDate = lastNotificationDate != null ? lastNotificationDate.Value : DateTime.MinValue;
            subscription.RegistrationUtcDate = lastNotificationDate != null ? lastNotificationDate.Value : DateTime.MinValue;
            dbContext.SearchSubscriptions.Add(subscription);
            await dbContext.SaveChangesAsync();
        }
    }
}
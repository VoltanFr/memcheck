using Microsoft.EntityFrameworkCore;
using MemCheck.Database;
using System;
using MemCheck.Domain;
using System.Threading.Tasks;

namespace MemCheck.Application.Tests.Helpers
{
    public static class SearchSubscriptionHelper
    {
        public static async Task<SearchSubscription> CreateAsync(DbContextOptions<MemCheckDbContext> testDB, Guid subscriberId, string requiredText = "", Guid? excludedDeckId = null, DateTime? lastNotificationDate = null)
        {
            using var dbContext = new MemCheckDbContext(testDB);
            var result = new SearchSubscription();
            result.UserId = subscriberId;
            result.ExcludedDeck = excludedDeckId == null ? Guid.Empty : excludedDeckId.Value;
            result.RequiredText = requiredText;
            result.RequiredTags = new RequiredTagInSearchSubscription[0];
            result.excludeAllTags = false;
            result.ExcludedTags = new ExcludedTagInSearchSubscription[0];
            result.LastRunUtcDate = lastNotificationDate != null ? lastNotificationDate.Value : DateTime.MinValue;
            result.RegistrationUtcDate = lastNotificationDate != null ? lastNotificationDate.Value : DateTime.MinValue;
            dbContext.SearchSubscriptions.Add(result);
            await dbContext.SaveChangesAsync();
            return result;
        }
    }
}
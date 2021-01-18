using Microsoft.EntityFrameworkCore;
using MemCheck.Database;
using System;
using MemCheck.Domain;
using System.Threading.Tasks;

namespace MemCheck.Application.Tests.Helpers
{
    public static class SearchSubscriptionHelper
    {
        public static async Task<SearchSubscription> CreateAsync(DbContextOptions<MemCheckDbContext> testDB, Guid subscriberId, string? name = null, string requiredText = "", Guid? excludedDeckId = null, DateTime? lastNotificationDate = null)
        {
            using var dbContext = new MemCheckDbContext(testDB);
            var result = new SearchSubscription();
            result.UserId = subscriberId;
            result.Name = name ?? StringHelper.RandomString();
            result.ExcludedDeck = excludedDeckId == null ? Guid.Empty : excludedDeckId.Value;
            result.RequiredText = requiredText;
            result.RequiredTags = Array.Empty<RequiredTagInSearchSubscription>();
            result.ExcludeAllTags = false;
            result.ExcludedTags = Array.Empty<ExcludedTagInSearchSubscription>();
            result.LastRunUtcDate = lastNotificationDate != null ? lastNotificationDate.Value : DateTime.MinValue;
            result.RegistrationUtcDate = lastNotificationDate != null ? lastNotificationDate.Value : DateTime.MinValue;
            dbContext.SearchSubscriptions.Add(result);
            await dbContext.SaveChangesAsync();
            return result;
        }
    }
}
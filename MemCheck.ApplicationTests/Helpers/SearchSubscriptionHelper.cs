using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Tests.Helpers
{
    public static class SearchSubscriptionHelper
    {
        public static async Task<SearchSubscription> CreateAsync(DbContextOptions<MemCheckDbContext> testDB, Guid subscriberId, string? name = null, string requiredText = "", Guid? excludedDeckId = null, DateTime? lastNotificationDate = null)
        {
            using var dbContext = new MemCheckDbContext(testDB);
            var result = new SearchSubscription
            {
                UserId = subscriberId,
                Name = name ?? RandomHelper.String(),
                ExcludedDeck = excludedDeckId == null ? Guid.Empty : excludedDeckId.Value,
                RequiredText = requiredText,
                RequiredTags = Array.Empty<RequiredTagInSearchSubscription>(),
                ExcludeAllTags = false,
                ExcludedTags = Array.Empty<ExcludedTagInSearchSubscription>(),
                LastRunUtcDate = lastNotificationDate != null ? lastNotificationDate.Value : DateTime.MinValue,
                RegistrationUtcDate = lastNotificationDate != null ? lastNotificationDate.Value : DateTime.MinValue
            };
            dbContext.SearchSubscriptions.Add(result);
            await dbContext.SaveChangesAsync();
            return result;
        }
    }
}
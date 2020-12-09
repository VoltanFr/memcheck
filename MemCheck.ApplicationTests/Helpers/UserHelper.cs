using Microsoft.EntityFrameworkCore;
using MemCheck.Database;
using System;
using MemCheck.Domain;
using System.Threading.Tasks;

namespace MemCheck.Application.Tests.Helpers
{
    public class UserHelper
    {
        public static async Task<Guid> CreateInDbAsync(DbContextOptions<MemCheckDbContext> db, int minimumCountOfDaysBetweenNotifs = 0, DateTime? lastNotificationUtcDate = null, bool subscribeToCardOnEdit = false, string? userName = null)
        {
            using var dbContext = new MemCheckDbContext(db);
            var result = Create(minimumCountOfDaysBetweenNotifs, lastNotificationUtcDate, subscribeToCardOnEdit, userName);
            dbContext.Users.Add(result);
            await dbContext.SaveChangesAsync();
            return result.Id;
        }
        public static MemCheckUser Create(int minimumCountOfDaysBetweenNotifs = 0, DateTime? lastNotificationUtcDate = null, bool subscribeToCardOnEdit = false, string? userName = null)
        {
            var result = new MemCheckUser();
            result.MinimumCountOfDaysBetweenNotifs = minimumCountOfDaysBetweenNotifs;
            result.LastNotificationUtcDate = lastNotificationUtcDate ?? DateTime.MinValue;
            result.SubscribeToCardOnEdit = subscribeToCardOnEdit;
            result.UserName = userName ?? StringServices.RandomString();
            return result;
        }
    }
}

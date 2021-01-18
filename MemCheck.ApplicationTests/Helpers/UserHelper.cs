using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
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
            return new MemCheckUser
            {
                MinimumCountOfDaysBetweenNotifs = minimumCountOfDaysBetweenNotifs,
                LastNotificationUtcDate = lastNotificationUtcDate ?? DateTime.MinValue,
                SubscribeToCardOnEdit = subscribeToCardOnEdit,
                UserName = userName ?? StringHelper.RandomString()
            };
        }
    }
}

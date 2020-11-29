using Microsoft.EntityFrameworkCore;
using MemCheck.Database;
using System;
using MemCheck.Domain;
using System.Threading.Tasks;

namespace MemCheck.Application.Tests.Notifying
{
    public class UserHelper
    {
        public static async Task<MemCheckUser> CreateAsync(DbContextOptions<MemCheckDbContext> db, int minimumCountOfDaysBetweenNotifs = 0, DateTime? lastNotificationUtcDate = null)
        {
            using var dbContext = new MemCheckDbContext(db);
            var result = new MemCheckUser();
            result.MinimumCountOfDaysBetweenNotifs = minimumCountOfDaysBetweenNotifs;
            result.LastNotificationUtcDate = lastNotificationUtcDate ?? DateTime.MinValue;
            dbContext.Users.Add(result);
            await dbContext.SaveChangesAsync();
            return result;
        }
    }
}

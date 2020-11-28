using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using MemCheck.Database;
using System.Linq;
using System;
using MemCheck.Application.Notifying;
using MemCheck.Domain;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MemCheck.Application.Tests.Notifying
{
    public class UserHelper
    {
        public static async Task<MemCheckUser> CreateUserAsync(DbContextOptions<MemCheckDbContext> db, int minimumCountOfDaysBetweenNotifs = 0, DateTime? lastNotificationUtcDate = null)
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
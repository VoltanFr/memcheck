using Microsoft.EntityFrameworkCore;
using MemCheck.Database;
using MemCheck.Domain;
using System.Threading.Tasks;
using System;

namespace MemCheck.Application.Tests.Helpers
{
    public static class CardLanguagHelper
    {
        public static async Task<Guid> CreateAsync(DbContextOptions<MemCheckDbContext> testDB)
        {
            using var dbContext = new MemCheckDbContext(testDB);
            var result = new CardLanguage();
            result.Name = StringServices.RandomString();
            dbContext.CardLanguages.Add(result);
            await dbContext.SaveChangesAsync();
            return result.Id;
        }
    }
}

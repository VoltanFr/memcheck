using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Tests.Helpers
{
    public static class CardLanguagHelper
    {
        public static async Task<Guid> CreateAsync(DbContextOptions<MemCheckDbContext> testDB, string? name = null)
        {
            using var dbContext = new MemCheckDbContext(testDB);
            var result = new CardLanguage { Name = name ?? RandomHelper.String() };
            dbContext.CardLanguages.Add(result);
            await dbContext.SaveChangesAsync();
            return result.Id;
        }
    }
}

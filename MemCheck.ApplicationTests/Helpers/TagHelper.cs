using Microsoft.EntityFrameworkCore;
using MemCheck.Database;
using MemCheck.Domain;
using System.Threading.Tasks;
using System;

namespace MemCheck.Application.Tests.Helpers
{
    public static class TagHelper
    {
        public static async Task<Guid> CreateAsync(DbContextOptions<MemCheckDbContext> testDB, string? name = null)
        {
            using var dbContext = new MemCheckDbContext(testDB);
            var result = new Tag();
            result.Name = name ?? StringHelper.RandomString();
            dbContext.Tags.Add(result);
            await dbContext.SaveChangesAsync();
            return result.Id;
        }
    }
}

using Microsoft.EntityFrameworkCore;
using MemCheck.Database;
using MemCheck.Domain;
using System.Threading.Tasks;
using System;

namespace MemCheck.Application.Tests.Helpers
{
    public static class ImageHelper
    {
        public static async Task<Guid> CreateAsync(DbContextOptions<MemCheckDbContext> testDB)
        {
            using var dbContext = new MemCheckDbContext(testDB);
            var result = new Image();
            result.Name = StringHelper.RandomString();
            result.Description = StringHelper.RandomString();
            result.OriginalContentType = StringHelper.RandomString();
            result.Source = StringHelper.RandomString();
            result.VersionDescription = StringHelper.RandomString();
            result.SmallBlob = new[] { (byte)1 };
            result.MediumBlob = new[] { (byte)1 };
            result.BigBlob = new[] { (byte)1 };
            result.OriginalBlob = new[] { (byte)1 };
            dbContext.Images.Add(result);
            await dbContext.SaveChangesAsync();
            return result.Id;
        }
    }
}

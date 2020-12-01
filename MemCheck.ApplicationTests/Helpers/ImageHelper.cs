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
            result.Name = StringServices.RandomString();
            result.Description = StringServices.RandomString();
            result.OriginalContentType = StringServices.RandomString();
            result.Source = StringServices.RandomString();
            result.VersionDescription = StringServices.RandomString();
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

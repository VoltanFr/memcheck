using Microsoft.EntityFrameworkCore;
using MemCheck.Database;
using MemCheck.Domain;
using System.Threading.Tasks;
using System;

namespace MemCheck.Application.Tests.Helpers
{
    public static class ImageHelper
    {
        public static async Task<Guid> CreateAsync(DbContextOptions<MemCheckDbContext> testDB, Guid creatorId, string? name = null)
        {
            using var dbContext = new MemCheckDbContext(testDB);
            var creator = await dbContext.Users.SingleAsync(u => u.Id == creatorId);
            var result = new Image();
            result.Owner = creator;
            result.Name = name ?? StringHelper.RandomString();
            result.Description = StringHelper.RandomString();
            result.Source = StringHelper.RandomString();
            result.InitialUploadUtcDate = DateTime.UtcNow;
            result.LastChangeUtcDate = DateTime.UtcNow;
            result.VersionDescription = StringHelper.RandomString();
            result.VersionType = ImageVersionType.Creation;
            result.OriginalContentType = "InvalidForUnitTests";
            result.OriginalSize = 1;
            result.OriginalBlob = new[] { (byte)0 };
            result.SmallBlobSize = 1;
            result.SmallBlob = new[] { (byte)0 };
            result.MediumBlobSize = 1;
            result.MediumBlob = new[] { (byte)0 };
            result.BigBlobSize = 1;
            result.BigBlob = new[] { (byte)0 };
            dbContext.Images.Add(result);
            await dbContext.SaveChangesAsync();
            return result.Id;
        }
    }
}

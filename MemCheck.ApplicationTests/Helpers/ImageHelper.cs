using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Tests.Helpers
{
    public static class ImageHelper
    {
        public static async Task<Guid> CreateAsync(DbContextOptions<MemCheckDbContext> testDB, Guid creatorId, string? name = null)
        {
            using var dbContext = new MemCheckDbContext(testDB);
            var creator = await dbContext.Users.SingleAsync(u => u.Id == creatorId);
            var result = new Image
            {
                Owner = creator,
                Name = name ?? RandomHelper.String(),
                Description = RandomHelper.String(),
                Source = RandomHelper.String(),
                InitialUploadUtcDate = DateTime.UtcNow,
                LastChangeUtcDate = DateTime.UtcNow,
                VersionDescription = RandomHelper.String(),
                VersionType = ImageVersionType.Creation,
                OriginalContentType = "InvalidForUnitTests",
                OriginalSize = 1,
                OriginalBlob = new[] { (byte)0 },
                SmallBlobSize = 1,
                SmallBlob = new[] { (byte)0 },
                MediumBlobSize = 1,
                MediumBlob = new[] { (byte)0 },
                BigBlobSize = 1,
                BigBlob = new[] { (byte)0 }
            };
            dbContext.Images.Add(result);
            await dbContext.SaveChangesAsync();
            return result.Id;
        }
    }
}

using MemCheck.Basics;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Helpers;

public static class ImageHelper
{
    public static async Task<Guid> CreateAsync(DbContextOptions<MemCheckDbContext> testDB, Guid creatorId, string? name = null, string? versionDescription = null, DateTime? lastChangeUtcDate = null, string? source = null, string? description = null)
    {
        using var dbContext = new MemCheckDbContext(testDB);
        var creator = await dbContext.Users.SingleAsync(u => u.Id == creatorId);
        var originalBlob = new[] { (byte)0, (byte)0, (byte)0, (byte)0 };
        var originalBlobSha1 = CryptoServices.GetSHA1(originalBlob);
        var smallBlob = new[] { (byte)0 };
        var mediumBlob = new[] { (byte)0, (byte)0 };
        var bigBlob = new[] { (byte)0, (byte)0, (byte)0 };
        var result = new Image
        {
            Owner = creator,
            Name = name ?? RandomHelper.String(),
            Description = description ?? RandomHelper.String(),
            Source = source ?? RandomHelper.String(),
            InitialUploadUtcDate = lastChangeUtcDate ?? DateTime.UtcNow,
            LastChangeUtcDate = lastChangeUtcDate ?? DateTime.UtcNow,
            VersionDescription = versionDescription ?? RandomHelper.String(),
            VersionType = ImageVersionType.Creation,
            OriginalContentType = "InvalidForUnitTests",
            OriginalSize = originalBlob.Length,
            OriginalBlob = originalBlob,
            OriginalBlobSha1 = originalBlobSha1,
            SmallBlobSize = smallBlob.Length,
            SmallBlob = smallBlob,
            MediumBlobSize = mediumBlob.Length,
            MediumBlob = mediumBlob,
            BigBlobSize = bigBlob.Length,
            BigBlob = bigBlob
        };
        dbContext.Images.Add(result);
        await dbContext.SaveChangesAsync();
        return result.Id;
    }
}

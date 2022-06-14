using MemCheck.Basics;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Helpers;

public static class ImageHelper
{
    public const string contentType = "InvalidForUnitTests";
    public const int originalBlobSize = 4;
    public const int smallBlobSize = 1;
    public const int mediumBlobSize = 2;
    public const int bigBlobSize = 3;
    public static async Task<Guid> CreateAsync(DbContextOptions<MemCheckDbContext> testDB, Guid creatorId, string? name = null, string? versionDescription = null, DateTime? lastChangeUtcDate = null, string? source = null, string? description = null)
    {
        using var dbContext = new MemCheckDbContext(testDB);
        var creator = await dbContext.Users.SingleAsync(u => u.Id == creatorId);
        var originalBlob = Enumerable.Range(1, originalBlobSize).Select(_ => (byte)0).ToArray();
        var originalBlobSha1 = CryptoServices.GetSHA1(originalBlob);
        var smallBlob = Enumerable.Range(1, smallBlobSize).Select(_ => (byte)0).ToArray();
        var mediumBlob = Enumerable.Range(1, mediumBlobSize).Select(_ => (byte)0).ToArray();
        var bigBlob = Enumerable.Range(1, bigBlobSize).Select(_ => (byte)0).ToArray();
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
            OriginalContentType = contentType,
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

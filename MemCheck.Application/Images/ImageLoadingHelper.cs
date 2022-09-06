using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace MemCheck.Application.Images;

internal static class ImageLoadingHelper
{
    public static ImmutableDictionary<Guid, ImageDetails> GetAllImageNames(MemCheckDbContext dbContext)
    {
        return dbContext.Images.AsNoTracking()
            .Select(i => new ImageDetails(i.Id, i.Name, i.Description, i.Owner.UserName, i.Source, i.InitialUploadUtcDate, i.LastChangeUtcDate, i.VersionDescription, i.OriginalContentType, i.OriginalSize, i.SmallBlobSize, i.MediumBlobSize, i.BigBlobSize))
            .ToImmutableDictionary(i => i.Id, i => i);
    }
}

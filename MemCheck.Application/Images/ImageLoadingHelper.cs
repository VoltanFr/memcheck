using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace MemCheck.Application.Images
{
    internal static class ImageLoadingHelper
    {
        public static ImmutableDictionary<Guid, string> GetAllImageNames(MemCheckDbContext dbContext)
        {
            return dbContext.Images.AsNoTracking().Select(i => new { i.Id, i.Name }).ToImmutableDictionary(i => i.Id, i => i.Name);
        }
    }
}
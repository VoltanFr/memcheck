using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace MemCheck.Application.Tags
{
    internal static class TagLoadingHelper
    {
        public static ImmutableDictionary<Guid, string> Run(MemCheckDbContext dbContext)
        {
            return dbContext.Tags.AsNoTracking().Select(t => new { t.Id, t.Name }).ToImmutableDictionary(t => t.Id, t => t.Name);
        }
    }
}

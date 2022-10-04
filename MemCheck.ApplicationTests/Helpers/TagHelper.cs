using MemCheck.Application.Tags;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Helpers;

public static class TagHelper
{
    public static async Task<Guid> CreateAsync(DbContextOptions<MemCheckDbContext> testDB, string? name = null, string? description = null)
    {
        using var dbContext = new MemCheckDbContext(testDB);
        var result = new Tag { Name = name ?? RandomHelper.String(), Description = description ?? RandomHelper.String() };
        dbContext.Tags.Add(result);
        await dbContext.SaveChangesAsync();
        return result.Id;
    }
    public static async Task RefreshAllAsync(DbContextOptions<MemCheckDbContext> db)
    {
        using var tmpDbContext = new MemCheckDbContext(db);
        await new RefreshTagStats(tmpDbContext.AsCallContext()).RunAsync(new RefreshTagStats.Request());
    }
}

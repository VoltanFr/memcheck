using MemCheck.Application.Tags;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Helpers;

public static class TagHelper
{
    public static async Task<Guid> CreateAsync(DbContextOptions<MemCheckDbContext> testDB, MemCheckUser creatingUser, string? name = null, string? description = null)
    {
        return await CreateAsync(testDB, creatingUser.Id, name, description);
    }
    public static async Task<Guid> CreateAsync(DbContextOptions<MemCheckDbContext> testDB, Guid creatingUserId, string? name = null, string? description = null, DateTime? versionUtcDate = null)
    {
        return (await CreateTagAsync(testDB, creatingUserId, name, description, versionUtcDate)).Id;
    }
    public static async Task<Tag> CreateTagAsync(DbContextOptions<MemCheckDbContext> testDB, Guid creatingUserId, string? name = null, string? description = null, DateTime? versionUtcDate = null)
    {
        using var dbContext = new MemCheckDbContext(testDB);
        var creatingUser = await dbContext.Users.SingleAsync(u => u.Id == creatingUserId);
        var result = new Tag
        {
            Name = name ?? RandomHelper.String(),
            Description = description ?? RandomHelper.String(),
            CreatingUser = creatingUser,
            VersionDescription = RandomHelper.String(),
            VersionUtcDate = versionUtcDate ?? RandomHelper.Date(),
        };
        dbContext.Tags.Add(result);
        await dbContext.SaveChangesAsync();
        return result;
    }
    public static async Task RefreshAllAsync(DbContextOptions<MemCheckDbContext> db)
    {
        using var tmpDbContext = new MemCheckDbContext(db);
        await new RefreshTagStats(tmpDbContext.AsCallContext()).RunAsync(new RefreshTagStats.Request());
    }
}

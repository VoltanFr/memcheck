using MemCheck.Application.Ratings;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Helpers;

public static class RatingHelper
{
    public static async Task RecordForUserAsync(DbContextOptions<MemCheckDbContext> testDB, Guid userId, Guid cardId, int rating)
    {
        var request = new SetCardRating.Request(userId, cardId, rating);
        using var dbContext = new MemCheckDbContext(testDB);
        await new SetCardRating(dbContext.AsCallContext()).RunAsync(request);
    }
    public static async Task RecordForUserAsync(DbContextOptions<MemCheckDbContext> testDB, MemCheckUser user, Guid cardId, int rating)
    {
        await RecordForUserAsync(testDB, user.Id, cardId, rating);
    }
}

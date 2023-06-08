using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Helpers;

public static class CardPreviousVersionHelper
{
    public static async Task<CardPreviousVersion?> GetPreviousVersionAsync(DbContextOptions<MemCheckDbContext> db, Guid cardId)
    {
        using var dbContext = new MemCheckDbContext(db);
        var card = await dbContext.Cards
            .AsNoTracking()
            .Include(card => card.PreviousVersion)
            .ThenInclude(previousVersion => previousVersion!.UsersWithView)
            .SingleOrDefaultAsync(card => card.Id == cardId);
        return card?.PreviousVersion;
    }
}

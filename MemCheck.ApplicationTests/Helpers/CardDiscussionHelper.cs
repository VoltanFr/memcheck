using MemCheck.Application.Cards;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Helpers;

public static class CardDiscussionHelper
{
    public static async Task<CardDiscussionEntry> CreateEntryAsync(DbContextOptions<MemCheckDbContext> testDB, Guid userId, Guid cardId, DateTime? entryDate = null, string? text = null)
    {
        var request = new AddEntryToCardDiscussion.Request(userId, cardId, text ?? RandomHelper.String());

        Guid entryId;
        using (var dbContext = new MemCheckDbContext(testDB))
            entryId = (await new AddEntryToCardDiscussion(dbContext.AsCallContext(), entryDate).RunAsync(request)).EntryId;

        using (var dbContext = new MemCheckDbContext(testDB))
            return dbContext.CardDiscussionEntries.Single(entry => entry.Id == entryId);
    }
}

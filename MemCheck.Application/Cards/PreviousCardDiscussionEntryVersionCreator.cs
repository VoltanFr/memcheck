using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards;

internal sealed class PreviousCardDiscussionEntryVersionCreator
{
    #region Fields
    private readonly MemCheckDbContext dbContext;
    #endregion
    #region Private methods
    private async Task<CardDiscussionEntryPreviousVersion> CreatePreviousVersionAsync(CardDiscussionEntry entry, DateTime? versionUtcDate = null)
    {
        var previousVersion = new CardDiscussionEntryPreviousVersion()
        {
            Card = entry.Card,
            Creator = entry.Creator,
            Text = entry.Text,
            CreationUtcDate = versionUtcDate ?? entry.CreationUtcDate,
            PreviousVersion = entry.PreviousVersion,
        };
        await dbContext.CardDiscussionEntryPreviousVersions.AddAsync(previousVersion);
        return previousVersion;
    }
    #endregion
    public PreviousCardDiscussionEntryVersionCreator(MemCheckDbContext dbContext)
    {
        this.dbContext = dbContext;
    }
    public async Task<CardDiscussionEntry> RunAsync(Guid entryId, DateTime? newVersionUtcDate = null)
    {
        var entry = await dbContext.CardDiscussionEntries
            .Include(entry => entry.Creator)
            .Include(entry => entry.PreviousVersion)
            .SingleAsync(entry => entry.Id == entryId);

        var previousVersion = await CreatePreviousVersionAsync(entry);
        entry.PreviousVersion = previousVersion;
        entry.CreationUtcDate = newVersionUtcDate ?? DateTime.UtcNow;
        return entry;
    }
}

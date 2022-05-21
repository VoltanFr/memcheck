using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.CommandLineDbClient.Deletion;

internal sealed class CountDeletedCards : ICmdLinePlugin
{
    #region Fields
    private readonly ILogger<BrutalDeletion> logger;
    private readonly MemCheckDbContext dbContext;
    #endregion
    public CountDeletedCards(MemCheckDbContext dbContext, ILogger<BrutalDeletion> logger)
    {
        this.dbContext = dbContext;
        this.logger = logger;
    }
    public CountDeletedCards(IServiceProvider serviceProvider) : this(serviceProvider.GetRequiredService<MemCheckDbContext>(), serviceProvider.GetRequiredService<ILogger<BrutalDeletion>>())
    {
    }
    public void DescribeForOpportunityToCancel()
    {
        logger.LogInformation($"Will count the deleted cards in the database");
    }
    public async Task RunAsync()
    {
        var deletedCount = await dbContext.CardPreviousVersions.Where(pv => pv.VersionType == CardPreviousVersionType.Deletion).CountAsync();
        var totalCount = await dbContext.CardPreviousVersions.CountAsync();

        logger.LogInformation($"There are {deletedCount} deleted cards in the DB (this does not include previous versions of non-deleted cards)");
        logger.LogInformation($"For information the total count of rows in the CardPreviousVersions table is {totalCount}");
    }
}

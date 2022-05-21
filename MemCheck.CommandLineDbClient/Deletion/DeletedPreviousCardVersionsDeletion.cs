using MemCheck.Application.Cards;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.CommandLineDbClient.Deletion;

internal sealed class DeletedPreviousCardVersionsDeletion : ICmdLinePlugin
{
    #region Fields
    private readonly ILogger<BrutalDeletion> logger;
    private readonly MemCheckDbContext dbContext;
    #endregion
    public DeletedPreviousCardVersionsDeletion(IServiceProvider serviceProvider)
    {
        dbContext = serviceProvider.GetRequiredService<MemCheckDbContext>();
        logger = serviceProvider.GetRequiredService<ILogger<BrutalDeletion>>();
    }
    public void DescribeForOpportunityToCancel()
    {
        logger.LogCritical($"Will completely delete cards currently soft-deleted");
    }
    public async Task RunAsync()
    {
        await new CountDeletedCards(dbContext, logger).RunAsync();

        var limitDate = DateTime.Now;

        logger.LogCritical($"Will completely delete all the soft deleted cards which were deleted before {limitDate}");
        logger.LogWarning("Please confirm again");
        Engine.GetConfirmationOrCancel(logger);

        var adminUserId = await dbContext.Users.Where(u => u.UserName == "Voltan").Select(u => u.Id).SingleAsync();
        var deleter = new DeleteCardPreviousVersionsOfDeletedCards(dbContext.AsCallContext());
        await deleter.RunAsync(new DeleteCardPreviousVersionsOfDeletedCards.Request(adminUserId, limitDate));

        logger.LogInformation($"Deletion finished");
    }
}

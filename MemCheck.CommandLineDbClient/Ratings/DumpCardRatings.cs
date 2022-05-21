using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.CommandLineDbClient.Ratings;

internal sealed class DumpCardRatings : ICmdLinePlugin
{
    #region Fields
    private readonly ILogger<RateAllBotCards> logger;
    private readonly MemCheckDbContext dbContext;
    private readonly Guid cardId = new("{f005a3e4-c358-4af8-32f6-08d9a7c792b3}");
    #endregion
    public DumpCardRatings(IServiceProvider serviceProvider)
    {
        dbContext = serviceProvider.GetRequiredService<MemCheckDbContext>();
        logger = serviceProvider.GetRequiredService<ILogger<RateAllBotCards>>();
    }
    public void DescribeForOpportunityToCancel()
    {
        logger.LogInformation($"Will dump all ratings of card '{cardId}'");
    }
    public async Task RunAsync()
    {
        var ratings = await dbContext.UserCardRatings.Include(rating => rating.User).Where(rating => rating.CardId == cardId).ToListAsync();
        logger.LogInformation($"Dumping {ratings.Count} ratings of card '{cardId}'...");
        foreach (var rating in ratings)
            logger.LogInformation($"By user '{rating.User.UserName}': {rating.Rating} stars");
        logger.LogInformation($"Rating finished");
    }
}

using MemCheck.Application.Ratings;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.CommandLineDbClient.Ratings
{
    internal sealed class RateAllBotCards : IMemCheckTest
    {
        #region Fields
        private readonly ILogger<RateAllBotCards> logger;
        private readonly MemCheckDbContext dbContext;
        #endregion
        public RateAllBotCards(IServiceProvider serviceProvider)
        {
            dbContext = serviceProvider.GetRequiredService<MemCheckDbContext>();
            logger = serviceProvider.GetRequiredService<ILogger<RateAllBotCards>>();
        }
        public void DescribeForOpportunityToCancel()
        {
            logger.LogInformation($"Will rate all bot cards");
        }
        async public Task RunAsync()
        {
            var author = await dbContext.Users.Where(u => u.UserName == "VoltanBot").SingleAsync();
            var ratingUser = await dbContext.Users.Where(u => u.UserName == "VoltanBot").SingleAsync();
            var cardIds = await dbContext.Cards.Where(card => card.VersionCreator.Id == author.Id).Select(card => card.Id).ToListAsync();

            var rater = new SetCardRating(dbContext);

            foreach (var cardId in cardIds)
            {
                var request = new SetCardRating.Request(ratingUser.Id, cardId, 5);
                await rater.RunAsync(request);
            }

            await dbContext.SaveChangesAsync();

            logger.LogInformation($"Rating finished");
        }
    }
}

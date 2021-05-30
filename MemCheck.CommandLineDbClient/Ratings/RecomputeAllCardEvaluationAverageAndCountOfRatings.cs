using MemCheck.Basics;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.CommandLineDbClient.Ratings
{
    internal sealed class RecomputeAllCardEvaluationAverageAndCountOfRatings : ICmdLinePlugin
    {
        #region Fields
        private readonly ILogger<RecomputeAllCardEvaluationAverageAndCountOfRatings> logger;
        private readonly MemCheckDbContext dbContext;
        #endregion
        public RecomputeAllCardEvaluationAverageAndCountOfRatings(IServiceProvider serviceProvider)
        {
            dbContext = serviceProvider.GetRequiredService<MemCheckDbContext>();
            logger = serviceProvider.GetRequiredService<ILogger<RecomputeAllCardEvaluationAverageAndCountOfRatings>>();
        }
        public void DescribeForOpportunityToCancel()
        {
            logger.LogInformation($"Will recompute average rating and rating count for all cards");
        }
        async public Task RunAsync()
        {
            var allCardWithRatings = (await dbContext.UserCardRatings.Select(c => c.CardId).ToListAsync()).ToImmutableArray();
            logger.LogInformation($"Will recompute ratings of {allCardWithRatings.Length} cards");

            foreach (var cardId in allCardWithRatings)
            {
                var count = await dbContext.UserCardRatings.AsNoTracking().Where(c => c.CardId == cardId).CountAsync();
                var average = await dbContext.UserCardRatings.AsNoTracking().Where(c => c.CardId == cardId).Select(c => c.Rating).AverageAsync();

                var card = await dbContext.Cards.Where(c => c.Id == cardId).SingleAsync();
                logger.LogInformation($"Will update to average eval {average} with a count of {count} evals: { card.FrontSide.Truncate(100)}");

                card.RatingCount = count;
                card.AverageRating = average;
            }

            await dbContext.SaveChangesAsync();

            logger.LogInformation($"Rating updates finished");
        }
    }
}

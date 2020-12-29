using MemCheck.Application;
using MemCheck.Application.CardChanging;
using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
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
        async public Task RunAsync(MemCheckDbContext dbContext)
        {
            var user = await dbContext.Users.Where(u => u.UserName == "VoltanBot").SingleAsync();
            var cardIds = await dbContext.Cards.Where(card => card.VersionCreator.Id == user.Id).Select(card => card.Id).ToListAsync();

            var rater = new SetCardRating(dbContext);

            foreach (var cardId in cardIds)
            {
                var request = new SetCardRating.Request(user, cardId, 5);
                await rater.RunAsync(request);
            }

            await dbContext.SaveChangesAsync();

            logger.LogInformation($"Rating finished");
        }
    }
}

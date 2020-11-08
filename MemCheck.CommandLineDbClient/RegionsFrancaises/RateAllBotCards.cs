using MemCheck.CommandLineDbClient.Geography;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.CommandLineDbClient.Pauker
{
    internal sealed class RateAllBotCards : IMemCheckTest
    {
        #region Fields
        private readonly ILogger<PaukerImportTest> logger;
        private readonly MemCheckDbContext dbContext;
        #endregion
        public RateAllBotCards(IServiceProvider serviceProvider)
        {
            dbContext = serviceProvider.GetService<MemCheckDbContext>();
            logger = serviceProvider.GetService<ILogger<PaukerImportTest>>();
        }
        public void DescribeForOpportunityToCancel()
        {
            logger.LogInformation($"Will rate all bot cards");
        }
        async public Task RunAsync(MemCheckDbContext dbContext)
        {
            var user = await dbContext.Users.Where(u => u.UserName == "VoltanBot").SingleAsync();
            var cardIds = await dbContext.Cards.Where(card => card.VersionCreator.Id == user.Id).Select(card => card.Id).ToListAsync();

            foreach (var cardId in cardIds)
                if (!dbContext.UserCardRatings.Where(ranking => ranking.UserId == user.Id && ranking.CardId == cardId).Any())
                    dbContext.UserCardRatings.Add(new UserCardRating() { UserId = user.Id, CardId = cardId, Rating = 5 });

            await dbContext.SaveChangesAsync();

            logger.LogInformation($"Rating finished");
        }
    }
}

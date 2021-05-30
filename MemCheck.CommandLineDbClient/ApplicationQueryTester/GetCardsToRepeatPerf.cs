using MemCheck.Application.Cards;
using MemCheck.Application.Heaping;
using MemCheck.Basics;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.CommandLineDbClient.ApplicationQueryTester
{
    internal sealed class GetCardsToRepeatPerf : ICmdLinePlugin
    {
        #region Fields
        private readonly ILogger<GetCardsToRepeatPerf> logger;
        private readonly MemCheckDbContext dbContext;
        #endregion
        public GetCardsToRepeatPerf(IServiceProvider serviceProvider)
        {
            dbContext = serviceProvider.GetRequiredService<MemCheckDbContext>();
            logger = serviceProvider.GetRequiredService<ILogger<GetCardsToRepeatPerf>>();
        }
        public void DescribeForOpportunityToCancel()
        {
            logger.LogInformation($"Will request cards to repeat");
        }
        async public Task RunAsync()
        {
            var user = dbContext.Users.Where(user => user.UserName == "Toto1").Single().Id;
            var deck = dbContext.Decks.Where(deck => deck.Owner.Id == user).First().Id;

            var chronos = new List<double>();

            for (int i = 0; i < 10; i++)
            {
                var chrono = await RunOneGet(user, deck);
                chronos.Add(chrono);
            }

            logger.LogInformation($"Average time: {chronos.Average()} seconds");
        }
        private async Task<double> RunOneGet(Guid userId, Guid deckId)
        {
            var request = new GetCardsToRepeat.Request(userId, deckId, Array.Empty<Guid>(), Array.Empty<Guid>(), 100);
            var runner = new GetCardsToRepeat(dbContext);
            var chrono = Stopwatch.StartNew();
            var cards = await runner.RunAsync(request);
            chrono.Stop();
            logger.LogInformation($"Got {cards.Count()} in {chrono.Elapsed}");
            return chrono.Elapsed.TotalSeconds;
        }
    }
}

//[10:28:04 INF] Got 14 in 00:00:00.9131297
//[10:28:04 INF] Average time: 1,1713525000000002 seconds
using MemCheck.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.CommandLineDbClient.ApplicationQueryTester
{
    internal sealed class GetUserDecksWithHeapsAndTags : ICmdLinePlugin
    {
        #region Fields
        private readonly ILogger<GetUserDecksWithHeapsAndTags> logger;
        private readonly MemCheckDbContext dbContext;
        #endregion
        public GetUserDecksWithHeapsAndTags(IServiceProvider serviceProvider)
        {
            dbContext = serviceProvider.GetRequiredService<MemCheckDbContext>();
            logger = serviceProvider.GetRequiredService<ILogger<GetUserDecksWithHeapsAndTags>>();
        }
        async public Task RunAsync()
        {
            var userId = dbContext.Users.Where(user => user.UserName == "Voltan").Single().Id;

            var chronos = new List<double>();

            for (int i = 0; i < 5; i++)
            {
                var realCodeChrono = Stopwatch.StartNew();
                var runner = new Application.Decks.GetUserDecksWithHeapsAndTags(dbContext.AsCallContext());
                var result = await runner.RunAsync(new Application.Decks.GetUserDecksWithHeapsAndTags.Request(userId));
                var deck = result.First();
                logger.LogInformation($"Deck: {deck.Description}");
                logger.LogInformation($"Heaps: {string.Join(',', deck.Heaps.Select(heap => heap.ToString()))}");
                logger.LogInformation($"Tags: {string.Join(',', deck.Tags.Select(tag => tag.TagName))}");
                logger.LogInformation($"Got {result.Count()} decks in {realCodeChrono.Elapsed}");
                chronos.Add(realCodeChrono.Elapsed.TotalSeconds);
            }

            logger.LogInformation($"Average time: {chronos.Average()} seconds");
        }
        public void DescribeForOpportunityToCancel()
        {
            logger.LogInformation($"Will request cards to learn");
        }
    }
}

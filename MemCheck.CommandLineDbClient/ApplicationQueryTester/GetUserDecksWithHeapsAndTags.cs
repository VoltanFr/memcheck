using MemCheck.Application;
using MemCheck.Application.Heaping;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace MemCheck.CommandLineDbClient.ApplicationQueryTester
{
    internal sealed class GetUserDecksWithHeapsAndTags : IMemCheckTest
    {
        #region Fields
        private readonly ILogger<GetCards> logger;
        private readonly MemCheckDbContext dbContext;
        #endregion
        public GetUserDecksWithHeapsAndTags(IServiceProvider serviceProvider)
        {
            dbContext = serviceProvider.GetService<MemCheckDbContext>();
            logger = serviceProvider.GetService<ILogger<GetCards>>();
        }
        async public Task RunAsync(MemCheckDbContext dbContext)
        {
            var userId = dbContext.Users.Where(user => user.UserName == "Voltan").Single().Id;

            var chronos = new List<double>();

            for (int i = 0; i < 5; i++)
            {
                var realCodeChrono = Stopwatch.StartNew();
                var runner = new Application.GetUserDecksWithHeapsAndTags(dbContext);
                var result = await runner.RunAsync(userId);
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

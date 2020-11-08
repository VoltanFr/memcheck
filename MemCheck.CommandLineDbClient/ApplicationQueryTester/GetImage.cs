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
    internal sealed class GetImage : IMemCheckTest
    {
        #region Fields
        private readonly ILogger<GetCards> logger;
        private readonly MemCheckDbContext dbContext;
        #endregion
        public GetImage(IServiceProvider serviceProvider)
        {
            dbContext = serviceProvider.GetService<MemCheckDbContext>();
            logger = serviceProvider.GetService<ILogger<GetCards>>();
        }
        async public Task RunAsync(MemCheckDbContext dbContext)
        {
            var user = dbContext.Users.Where(user => user.UserName == "Voltan").Single();
            var deck = dbContext.Decks.Where(deck => deck.Owner == user).First();

            var chronos = new List<double>();

            for (int i = 0; i < 5; i++)
            {
                var realCodeChrono = Stopwatch.StartNew();
                var request = new Application.GetImage.Request(new Guid("980ce406-0417-4963-c9b4-08d8206a4d4c"), 2);
                var runner = new Application.GetImage(dbContext);
                var bytes = runner.Run(request);
                logger.LogInformation($"Got {bytes.Length} bytes in {realCodeChrono.Elapsed}");
                chronos.Add(realCodeChrono.Elapsed.TotalSeconds);
            }

            logger.LogInformation($"Average time: {chronos.Average()} seconds");

            await Task.CompletedTask;
        }
        public void DescribeForOpportunityToCancel()
        {
            logger.LogInformation($"Will request cards to learn");
        }
    }
}

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
    internal sealed class GetImage : ICmdLinePlugin
    {
        #region Fields
        private readonly ILogger<GetImage> logger;
        private readonly MemCheckDbContext dbContext;
        #endregion
        public GetImage(IServiceProvider serviceProvider)
        {
            dbContext = serviceProvider.GetRequiredService<MemCheckDbContext>();
            logger = serviceProvider.GetRequiredService<ILogger<GetImage>>();
        }
        public async Task RunAsync()
        {
            var user = dbContext.Users.Where(user => user.UserName == "Voltan").Single();
            var deck = dbContext.Decks.Where(deck => deck.Owner == user).First();

            var chronos = new List<double>();

            for (int i = 0; i < 5; i++)
            {
                var realCodeChrono = Stopwatch.StartNew();
                var request = new Application.Images.GetImage.Request(new Guid("980ce406-0417-4963-c9b4-08d8206a4d4c"), Application.Images.GetImage.Request.ImageSize.Medium);
                var runner = new Application.Images.GetImage(dbContext.AsCallContext());
                var bytes = await runner.RunAsync(request);
                logger.LogInformation($"Got {bytes.ImageBytes.Length} bytes in {realCodeChrono.Elapsed}");
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

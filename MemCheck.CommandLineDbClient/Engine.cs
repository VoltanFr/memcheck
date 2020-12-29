using MemCheck.CommandLineDbClient.ApplicationQueryTester;
using MemCheck.CommandLineDbClient.Pauker;
using MemCheck.CommandLineDbClient.Ratings;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MemCheck.CommandLineDbClient
{
    //public class TheApp : IHostedService
    //{
    //    ClassThatLogs _classThatLogs;

    //    public TheApp(ClassThatLogs classThatLogs)
    //    {
    //        _classThatLogs = classThatLogs ?? throw new ArgumentNullException(nameof(classThatLogs));
    //    }

    //    public Task StartAsync(CancellationToken cancellationToken)
    //    {
    //        _classThatLogs.WriteLogs();

    //        return Task.CompletedTask;
    //    }

    //    public Task StopAsync(CancellationToken cancellationToken)
    //    {
    //        return Task.CompletedTask;
    //    }
    //}

    internal sealed class Engine : IHostedService
    {
        #region Fields
        private readonly ILogger<Engine> logger;
        private readonly MemCheckDbContext dbContext;
        private readonly IServiceProvider serviceProvider;
        #endregion
        public Engine(ILogger<Engine> logger, MemCheckDbContext dbContext, IServiceProvider serviceProvider)
        {
            this.logger = logger;
            this.dbContext = dbContext;
            this.serviceProvider = serviceProvider;
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            IMemCheckTest test = new RateAllBotCards(serviceProvider);
            test.DescribeForOpportunityToCancel();
            logger.LogWarning("Opportunity to cancel. Please confirm with Y");
            var input = Console.ReadLine();
            if (input == null || !input.Trim().Equals("y", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogError("User cancellation");
                return;
            }
            try
            {
                await test.RunAsync(dbContext);
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
            }
            logger.LogInformation($"Program terminating");
        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

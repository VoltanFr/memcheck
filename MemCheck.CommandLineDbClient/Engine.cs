using MemCheck.CommandLineDbClient.ManageDB;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MemCheck.CommandLineDbClient
{
    internal sealed class Engine : IHostedService
    {
        #region Fields
        private readonly ILogger<Engine> logger;
        private readonly IServiceProvider serviceProvider;
        #endregion
        #region Private method
        private ICmdLinePlugin GetPlugin()
        {
            return new MakeUserAdmin(serviceProvider);
        }
        #endregion
        public Engine(ILogger<Engine> logger, IServiceProvider serviceProvider)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var test = GetPlugin();
            test.DescribeForOpportunityToCancel();
            GetConfirmationOrCancel(logger);
            var chrono = Stopwatch.StartNew();
            try
            {
                await test.RunAsync();
                chrono.Stop();
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
            }
            logger.LogInformation($"Program terminating, took {chrono.Elapsed}");
            Debugger.Break();
            throw new InvalidProgramException("Test done");
        }
        public static void GetConfirmationOrCancel(ILogger logger)
        {
            logger.LogWarning("Opportunity to cancel. Please confirm with Y");
            var input = Console.ReadLine();
            if (input == null || !input.Trim().Equals("y", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogError("User cancellation");
                throw new Exception("User cancellation");
            }
        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

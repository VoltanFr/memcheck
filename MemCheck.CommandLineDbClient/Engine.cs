using MemCheck.CommandLineDbClient.HandleBadCards;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
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
        private readonly IServiceProvider serviceProvider;
        #endregion
        public Engine(ILogger<Engine> logger, IServiceProvider serviceProvider)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            IMemCheckTest test = new ListCardsWithVisibilityConsistencyProblem(serviceProvider);
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
                await test.RunAsync();
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
            }
            logger.LogInformation($"Program terminating");
            Debugger.Break();
            throw new InvalidProgramException("Test done");
        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

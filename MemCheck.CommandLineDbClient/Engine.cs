using MemCheck.CommandLineDbClient.PerfMeasurements.Search;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MemCheck.CommandLineDbClient;

internal sealed class Engine : IHostedService
{
    #region Fields
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<Engine> logger;
    #endregion
    #region Private method
    private ICmdLinePlugin GetPlugin()
    {
        return new TextSearchPerfMeasurements(serviceProvider);
    }
    #endregion
    public Engine(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        logger = serviceProvider.GetRequiredService<ILogger<Engine>>();
    }
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var dbContext = serviceProvider.GetRequiredService<MemCheckDbContext>();
        var connectionString = dbContext.Database.GetConnectionString()!;
        var logLevel = connectionString.Contains("memcheck-db-server-fr.database.windows.net", StringComparison.OrdinalIgnoreCase) ? LogLevel.Warning : LogLevel.Information;
        logger.Log(logLevel, $"DB: {connectionString}");

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
            throw new InvalidProgramException("User cancellation");
        }
    }
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

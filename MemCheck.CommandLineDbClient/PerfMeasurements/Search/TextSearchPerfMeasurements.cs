using MemCheck.Application;
using MemCheck.Application.Searching;
using MemCheck.Basics;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.CommandLineDbClient.PerfMeasurements.Search;

internal sealed class TextSearchPerfMeasurements : ICmdLinePlugin
{
    #region Fields
    private readonly ILogger logger;
    private readonly MemCheckDbContext dbContext;
    private readonly CallContext callContext;
    #endregion
    private sealed record TestDefinition(string Description, SearchCards.Request Request)
    {
        public List<double> RunSpentSeconds = new();
        public int TotalNbCards { get; set; } = -1;
        public int CardCount { get; set; } = -1;
        public int AnomalyCount { get; set; } = -1;
    }
    public TextSearchPerfMeasurements(IServiceProvider serviceProvider)
    {
        dbContext = serviceProvider.GetRequiredService<MemCheckDbContext>();
        logger = serviceProvider.GetRequiredService<ILogger<TextSearchPerfMeasurements>>();
        callContext = serviceProvider.GetRequiredService<MemCheckDbContext>().AsCallContext();
    }
    public void DescribeForOpportunityToCancel()
    {
        logger.LogInformation("Will measure perf of searching card containing text");
    }
    private async Task RunTestAsync(TestDefinition test)
    {
        var search = new SearchCards(callContext);
        var chrono = Stopwatch.StartNew();
        var result = await search.RunAsync(test.Request);
        chrono.Stop();

        if (test.TotalNbCards == -1) // On first run, we keep the counts, and we don't save the chrono, since we consider this run as a pre-heat
        {
            test.TotalNbCards = result.TotalNbCards;
            test.CardCount = result.Cards.Length;
            test.AnomalyCount = 0;
        }
        else
        {
            test.RunSpentSeconds.Add(chrono.Elapsed.TotalSeconds);
            if (result.TotalNbCards != test.TotalNbCards)
            {
                logger.LogError($"Unexpected result.TotalNbCards (not equal to first run)");
                test.AnomalyCount++;
            }
            if (result.Cards.Length != test.CardCount)
            {
                logger.LogError($"Unexpected result.CardCount (not equal to first run)");
                test.AnomalyCount++;
            }
        }
    }
    public async Task RunAsync()
    {
        var cardLanguage = await dbContext.CardLanguages.SingleAsync();
        var userVoltan = callContext.DbContext.Users.Single(u => u.UserName == "Voltan").Id;

        var testDefinitions = new[] {
            new TestDefinition("Without text with user",new SearchCards.Request() { UserId = userVoltan }),
            new TestDefinition("Without text without user",new SearchCards.Request() { UserId = userVoltan }),
            new TestDefinition("With not existing text with user",new SearchCards.Request() { UserId = userVoltan,RequiredText = new Guid().ToString() }),
            new TestDefinition("With not existing text without user",new SearchCards.Request() { RequiredText = new Guid().ToString() }),
            new TestDefinition("Tri with user",new SearchCards.Request() { UserId = userVoltan,RequiredText = "Tri", PageSize = 500 }),
            new TestDefinition("Tri without user",new SearchCards.Request() { RequiredText = "Tri", PageSize = 500 }),
            new TestDefinition("e with user",new SearchCards.Request() { UserId = userVoltan,RequiredText = "e", PageSize = 500 }),
            new TestDefinition("e without user",new SearchCards.Request() { RequiredText = "e", PageSize = 500 }),
        };

        await 10.TimesAsync(async () =>
            {
                foreach (var testDefinition in testDefinitions)
                    await RunTestAsync(testDefinition);
            }
        );

        foreach (var testDefinition in testDefinitions)
        {
            logger.LogInformation($"Average time for test '{testDefinition.Description}': {Enumerable.Average(testDefinition.RunSpentSeconds):F2}");
            logger.LogInformation($"\tTotalNbCards: {testDefinition.TotalNbCards}");
            logger.LogInformation($"\tCardCount: {testDefinition.CardCount}");
            if (testDefinition.AnomalyCount > 0)
                logger.LogError($"\tAnomaly count: {testDefinition.AnomalyCount}");
        }
    }
}

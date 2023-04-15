using MemCheck.Application.Searching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CallContext = MemCheck.Application.CallContext;

namespace MemCheck.CommandLineDbClient.PerfMeasurements.Search;

internal sealed class TextSearchPerfMeasurements : AbstractPerfMeasurements<TextSearchPerfMeasurements.TestDefinition>
{
    internal sealed record TestDefinition : PerfTestDefinition
    {
        public TestDefinition(string Description, SearchCards.Request request) : base(Description)
        {
            Request = request;
            TotalNbCards = -1;
            CardCount = -1;
        }

        public int TotalNbCards { get; set; }
        public int CardCount { get; set; }
        public SearchCards.Request Request { get; }

        public override void LogDetailsOnEnd(ILogger logger)
        {
            logger.LogInformation($"\tTotalNbCards: {TotalNbCards}");
            logger.LogInformation($"\tCardCount: {CardCount}");
        }
    }
    public TextSearchPerfMeasurements(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
    protected override async Task<IEnumerable<TestDefinition>> CreateTestDefinitionsAsync()
    {
        var cardLanguage = await DbContext.CardLanguages.SingleAsync();
        var userVoltan = CallContext.DbContext.Users.Single(u => u.UserName == "Voltan").Id;
        //var tagQuartiersM = callContext.DbContext.Tags.Single(tag => tag.Name == "Quartiers maritimes").Id;

        return new[] {
            new TestDefinition("Without text with user",new SearchCards.Request() { UserId = userVoltan }),
            new TestDefinition("Without text without user",new SearchCards.Request() ),
            new TestDefinition("With not existing text with user",new SearchCards.Request() { UserId = userVoltan,RequiredText = new Guid().ToString() }),
            new TestDefinition("With not existing text without user",new SearchCards.Request() { RequiredText = new Guid().ToString() }),
            new TestDefinition("Tri with user",new SearchCards.Request() { UserId = userVoltan,RequiredText = "Tri", PageSize = 500 }),
            new TestDefinition("Tri without user",new SearchCards.Request() { RequiredText = "Tri", PageSize = 500 }),
            new TestDefinition("e with user",new SearchCards.Request() { UserId = userVoltan,RequiredText = "e", PageSize = 500 }),
            new TestDefinition("e without user",new SearchCards.Request() { RequiredText = "e", PageSize = 500 }),
            //new TestDefinition("Rochelle with tag", new SearchCards.Request() { UserId = userVoltan,RequiredText = "Rochelle", PageSize = 50, RequiredTags=tagQuartiersM.AsArray() })
        };
    }
    protected override int IterationCount => 10;
    public override void DescribeForOpportunityToCancel()
    {
        Logger.LogInformation("Will measure perf of searching card containing text");
    }
    protected override async Task RunTestAsync(TestDefinition test)
    {
        var search = new SearchCards(CallContext);
        var chrono = Stopwatch.StartNew();
        var result = await search.RunAsync(test.Request);
        chrono.Stop();

        if (test.AnomalyCount == -1) // On first run, we keep the counts, and we don't save the chrono, since we consider this run as a pre-heat
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
                Logger.LogError($"Unexpected result.TotalNbCards (not equal to first run)");
                test.AnomalyCount++;
            }
            if (result.Cards.Length != test.CardCount)
            {
                Logger.LogError($"Unexpected result.CardCount (not equal to first run)");
                test.AnomalyCount++;
            }
        }
    }
}

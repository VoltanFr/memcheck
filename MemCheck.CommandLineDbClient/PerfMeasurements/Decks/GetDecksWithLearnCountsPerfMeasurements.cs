using MemCheck.Application.Decks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CallContext = MemCheck.Application.CallContext;

namespace MemCheck.CommandLineDbClient.PerfMeasurements.Decks;

internal sealed class GetDecksWithLearnCountsPerfMeasurements : AbstractPerfMeasurements<GetDecksWithLearnCountsPerfMeasurements.TestDefinition>
{
    internal sealed record TestDefinition : PerfTestDefinition
    {
        public TestDefinition(string Description, GetDecksWithLearnCounts.Request request) : base(Description)
        {
            Request = request;
            DeckDescription = "NotSet";
        }

        public Guid Id { get; set; }
        public string DeckDescription { get; set; }
        public int CardCount { get; set; }

        public GetDecksWithLearnCounts.Request Request { get; }

        public override void LogDetailsOnEnd(ILogger logger)
        {
            logger.LogInformation($"\tId: {Id}");
            logger.LogInformation($"\tDescription: {DeckDescription}");
            logger.LogInformation($"\tCardCount: {CardCount}");
        }
    }
    public GetDecksWithLearnCountsPerfMeasurements(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
    protected override async Task<IEnumerable<TestDefinition>> CreateTestDefinitionsAsync()
    {
        var cardLanguage = await DbContext.CardLanguages.SingleAsync();
        var userVoltan = CallContext.DbContext.Users.Single(u => u.UserName == "Voltan").Id;

        return new[] {
            new TestDefinition("With user",new GetDecksWithLearnCounts.Request(userVoltan))
        };
    }
    protected override int IterationCount => 100;
    public override void DescribeForOpportunityToCancel()
    {
        Logger.LogInformation("Will measure perf of getting decks with learn counts");
    }
    protected override async Task RunTestAsync(TestDefinition test)
    {
        var search = new GetDecksWithLearnCounts(CallContext);
        var chrono = Stopwatch.StartNew();
        var result = await search.RunAsync(test.Request);
        chrono.Stop();

        if (result.Length != 1)
            throw new InvalidProgramException("Only implemented for single deck user");

        var deck = result.First();

        if (test.AnomalyCount == -1) // On first run, we keep the values, and we don't save the chrono, since we consider this run as a pre-heat
        {
            test.AnomalyCount = 0;
            test.Id = deck.Id;
            test.DeckDescription = deck.Description;
            test.CardCount = deck.CardCount;
        }
        else
        {
            test.RunSpentSeconds.Add(chrono.Elapsed.TotalSeconds);
            if (deck.Id != test.Id)
            {
                Logger.LogError($"Unexpected Id (not equal to first run)");
                test.AnomalyCount++;
            }
            if (deck.Description != test.DeckDescription)
            {
                Logger.LogError($"Unexpected Description (not equal to first run)");
                test.AnomalyCount++;
            }
            if (deck.CardCount != test.CardCount)
            {
                Logger.LogError($"Unexpected CardCount (not equal to first run)");
                test.AnomalyCount++;
            }
        }
    }
}

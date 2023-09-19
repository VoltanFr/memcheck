using MemCheck.Application.Cards;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CallContext = MemCheck.Application.CallContext;

namespace MemCheck.CommandLineDbClient.PerfMeasurements.Cards;

internal sealed class GetCardForEditPerfMeasurements : AbstractPerfMeasurements<GetCardForEditPerfMeasurements.TestDefinition>
{
    internal sealed record TestDefinition : PerfTestDefinition
    {
        public TestDefinition(string Description, GetCardForEdit.Request request) : base(Description)
        {
            Request = request;
            FrontSide = "NotSet";
        }

        public string FrontSide { get; set; }

        public GetCardForEdit.Request Request { get; }

        public override void LogDetailsOnEnd(ILogger logger)
        {
            logger.LogInformation($"\tFrontSide: {FrontSide}");
        }
    }
    public GetCardForEditPerfMeasurements(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
    protected override async Task<IEnumerable<TestDefinition>> CreateTestDefinitionsAsync()
    {
        var cardLanguage = await DbContext.CardLanguages.SingleAsync();
        var userVoltan = CallContext.DbContext.Users.Single(u => u.UserName == "Voltan").Id;

        return new[] {
            new TestDefinition("In deck", new GetCardForEdit.Request(userVoltan, new Guid("c6cbd449-1155-42cd-e7b9-08d7eba1e1a5"))),
            new TestDefinition("Not in deck", new GetCardForEdit.Request(userVoltan, new Guid("9a6966b1-8aea-4d87-715f-08d7e644ee91")))
        };
    }
    protected override int IterationCount => 100;
    public override void DescribeForOpportunityToCancel()
    {
        Logger.LogInformation("Will measure perf of get cards for learning");
    }
    protected override async Task RunTestAsync(TestDefinition test)
    {
        var getApp = new GetCardForEdit(CallContext);
        var chrono = Stopwatch.StartNew();
        var result = await getApp.RunAsync(test.Request);
        chrono.Stop();

        if (test.AnomalyCount == -1) // On first run, we keep the values, and we don't save the chrono, since we consider this run as a pre-heat
        {
            test.AnomalyCount = 0;
            test.FrontSide = result.FrontSide;
        }
        else
        {
            test.RunSpentSeconds.Add(chrono.Elapsed.TotalSeconds);
            if (result.FrontSide != test.FrontSide)
            {
                Logger.LogError($"Unexpected front side (not equal to first run)");
                test.AnomalyCount++;
            }
        }
    }
}

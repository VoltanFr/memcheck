using MemCheck.Application.QueryValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using CallContext = MemCheck.Application.CallContext;

namespace MemCheck.CommandLineDbClient.PerfMeasurements.QueryValidation;

internal sealed class CheckUserExistsPerfMeasurements : AbstractPerfMeasurements<CheckUserExistsPerfMeasurements.TestDefinition>
{
    internal sealed record TestDefinition : PerfTestDefinition
    {
        public TestDefinition(string Description, Guid UserId) : base(Description)
        {
            this.UserId = UserId;
        }

        public Guid UserId { get; }
        public bool Exists { get; set; }

        public override void LogDetailsOnEnd(ILogger logger)
        {
        }
    }
    public CheckUserExistsPerfMeasurements(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
    public override void DescribeForOpportunityToCancel()
    {
        Logger.LogInformation("Will measure perf of QueryValidationHelper.CheckUserExists");
    }
    protected override async Task<IEnumerable<TestDefinition>> CreateTestDefinitionsAsync()
    {
        var userVoltan = (await CallContext.DbContext.Users.SingleAsync(u => u.UserName == "Voltan")).Id;

        return new[] {
            new TestDefinition("With user",userVoltan ),
            new TestDefinition("Without user",Guid.Empty ),
        };
    }
    protected override int IterationCount => 100;
    protected override async Task RunTestAsync(TestDefinition test)
    {
        bool exists;
        var chrono = Stopwatch.StartNew();
        try
        {
            await QueryValidationHelper.CheckUserExistsAsync(DbContext, test.UserId);
            exists = true;
        }
        catch (NonexistentUserException)
        {
            exists = false;
        }
        catch (Exception e)
        {
            Logger.LogError($"Unexpected exception: {e.GetType().Name} ({e.Message})");
            throw;
        }
        chrono.Stop();

        if (test.AnomalyCount == -1) // On first run, we keep the counts, and we don't save the chrono, since we consider this run as a pre-heat
        {
            test.AnomalyCount = 0;
            test.Exists = exists;
        }
        else
        {
            test.RunSpentSeconds.Add(chrono.Elapsed.TotalSeconds);
            if (exists != test.Exists)
            {
                Logger.LogError($"Unexpected result: {exists} (was {test.Exists} on first run)");
                test.AnomalyCount++;
            }
        }
    }
}

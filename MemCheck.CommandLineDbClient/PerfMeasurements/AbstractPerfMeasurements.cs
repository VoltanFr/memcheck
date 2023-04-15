using MemCheck.Basics;
using MemCheck.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using CallContext = MemCheck.Application.CallContext;

namespace MemCheck.CommandLineDbClient.PerfMeasurements;

internal abstract class AbstractPerfMeasurements<TestDefinitionClass> : ICmdLinePlugin where TestDefinitionClass : PerfTestDefinition
{
    protected AbstractPerfMeasurements(IServiceProvider serviceProvider)
    {
        DbContext = serviceProvider.GetRequiredService<MemCheckDbContext>();
        Logger = serviceProvider.GetRequiredService<ILogger<AbstractPerfMeasurements<TestDefinitionClass>>>();
        CallContext = serviceProvider.GetRequiredService<MemCheckDbContext>().AsCallContext();
    }

    protected abstract Task<IEnumerable<TestDefinitionClass>> CreateTestDefinitionsAsync();
    protected abstract int IterationCount { get; }
    public abstract void DescribeForOpportunityToCancel();
    protected abstract Task RunTestAsync(TestDefinitionClass test);

    protected MemCheckDbContext DbContext { get; }
    protected ILogger<AbstractPerfMeasurements<TestDefinitionClass>> Logger { get; }
    protected CallContext CallContext { get; }

    public async Task RunAsync()
    {
        var testDefinitions = (await CreateTestDefinitionsAsync()).ToImmutableArray();

        await IterationCount.TimesAsync(async () =>
        {
            foreach (var testDefinition in testDefinitions)
                await RunTestAsync(testDefinition);
        }
        );

        foreach (var testDefinition in testDefinitions)
            testDefinition.LogOnEnd(Logger);
    }
}

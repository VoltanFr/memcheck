using MemCheck.Application.Images;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.CommandLineDbClient.PerfMeasurements.Images;

internal sealed class GetImageFromNamePerfMeasurements : AbstractPerfMeasurements<GetImageFromNamePerfMeasurements.TestDefinition>
{
    internal sealed record TestDefinition : PerfTestDefinition
    {
        public TestDefinition(string description, int byteCount, GetImageFromName.Request request) : base(description)
        {
            Request = request;
            ByteCount = byteCount;
        }

        public int ByteCount { get; set; }

        public GetImageFromName.Request Request { get; }

        public override void LogDetailsOnEnd(ILogger logger)
        {
            logger.LogInformation($"\tImage bytes: {ByteCount}");
        }
    }
    public GetImageFromNamePerfMeasurements(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
    protected override async Task<IEnumerable<TestDefinition>> CreateTestDefinitionsAsync()
    {
        var imageNames = CallContext.DbContext.Images.Select(img => img.Name);
        foreach (var imageName in imageNames)
            Console.WriteLine(imageName);

        await Task.CompletedTask;
        return new[] {
            new TestDefinition("Small image", int.MinValue, new GetImageFromName.Request("Vocabulaire anglais bateau", GetImageFromName.Request.ImageSize.Small)),
            new TestDefinition("Big image", int.MinValue,new GetImageFromName.Request("Voiliers au port", GetImageFromName.Request.ImageSize.Big))
        };
    }
    protected override int IterationCount => 1000;
    public override void DescribeForOpportunityToCancel()
    {
        Logger.LogInformation("Will measure perf of getting an image from its name");
    }
    protected override async Task RunTestAsync(TestDefinition test)
    {
        var search = new GetImageFromName(CallContext);
        var chrono = Stopwatch.StartNew();
        var result = await search.RunAsync(test.Request);
        chrono.Stop();

        if (test.AnomalyCount == -1) // On first run, we keep the values, and we don't save the chrono, since we consider this run as a pre-heat
        {
            test.AnomalyCount = 0;
            test.ByteCount = result.ImageBytes.Length;
        }
        else
        {
            test.RunSpentSeconds.Add(chrono.Elapsed.TotalSeconds);
            if (result.ImageBytes.Length != test.ByteCount)
            {
                Logger.LogError($"Unexpected byte count (not equal to first run)");
                test.AnomalyCount++;
            }
        }
    }
}

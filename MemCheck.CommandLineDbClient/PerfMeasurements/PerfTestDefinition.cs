using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace MemCheck.CommandLineDbClient.PerfMeasurements;

internal abstract record PerfTestDefinition(string Description)
{
    public List<double> RunSpentSeconds = new();
    public int AnomalyCount { get; set; } = -1;
    public void LogOnEnd(ILogger logger)
    {
        logger.LogInformation($"Average time for test '{Description}': {Enumerable.Average(RunSpentSeconds):F2}");
        LogDetailsOnEnd(logger);
        if (AnomalyCount > 0)
            logger.LogError($"\tAnomaly count: {AnomalyCount}");
    }
    public abstract void LogDetailsOnEnd(ILogger logger);
}

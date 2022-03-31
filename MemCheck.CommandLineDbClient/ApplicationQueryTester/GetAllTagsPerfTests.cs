using MemCheck.Application;
using MemCheck.Application.Tags;
using MemCheck.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.CommandLineDbClient.ApplicationQueryTester
{
    internal sealed class GetAllTagsPerfTests : ICmdLinePlugin
    {
        #region Fields
        private readonly ILogger<GetCardForEdit> logger;
        private readonly CallContext callContext;
        #endregion
        private record TestDefinition(string Description, GetAllTags.Request Request)
        {
            public List<double> RunSpentSeconds = new List<double>();
            public int TagCount { get; set; }
            public int TotalCardCount { get; set; }
            public int AnomalyCount { get; set; } = 0;
        }
        public GetAllTagsPerfTests(IServiceProvider serviceProvider)
        {
            logger = serviceProvider.GetRequiredService<ILogger<GetCardForEdit>>();
            callContext = serviceProvider.GetRequiredService<MemCheckDbContext>().AsCallContext();
        }
        private async Task RunTestAsync(TestDefinition test, bool isFirstRun)
        {
            var chrono = Stopwatch.StartNew();
            var result = (await new GetAllTags(callContext).RunAsync(test.Request)).Tags.ToImmutableArray();
            chrono.Stop();
            var totalCardCount = Enumerable.Sum(result, tag => tag.CardCount);
            if (isFirstRun)
            {
                test.TagCount = result.Length;
                test.TotalCardCount = totalCardCount;
            }
            else
            {
                test.RunSpentSeconds.Add(chrono.Elapsed.TotalSeconds);
                if (result.Length != test.TagCount)
                {
                    logger.LogError($"Unexpected tag count (not equal to first run)");
                    test.AnomalyCount++;
                }
                if (totalCardCount != test.TotalCardCount)
                {
                    logger.LogError($"Unexpected total card count (not equal to first run)");
                    test.AnomalyCount++;
                }
            }
        }
        async public Task RunAsync()
        {
            //Tests show that single measures are not reliable. I run each case ten times, not taking the first time into account.

            var userVoltan = callContext.DbContext.Users.Single(u => u.UserName == "Voltan").Id;

            var testDefinitions = new[] {
                new TestDefinition("Voltan without filtering",new GetAllTags.Request(200, 1, "")),
                new TestDefinition("Voltan filtering on Marine",new GetAllTags.Request( 200, 1, "Marine")),
                new TestDefinition("No user without filtering",new GetAllTags.Request( 200, 1, "")),
                new TestDefinition("No user with filtering",new GetAllTags.Request(  200, 1, "Marine")),
            };

            for (int i = 0; i < 10; i++)
                foreach (var testDefinition in testDefinitions)
                    await RunTestAsync(testDefinition, i == 0);

            foreach (var testDefinition in testDefinitions)
            {
                logger.LogInformation($"Average time for test '{testDefinition.Description}': {Enumerable.Average(testDefinition.RunSpentSeconds)}");
                logger.LogInformation($"\tCount of tags for test '{testDefinition.Description}': {testDefinition.TagCount}");
                logger.LogInformation($"\tSumm of card count for test '{testDefinition.Description}': {testDefinition.TotalCardCount}");
                if (testDefinition.AnomalyCount > 0)
                    logger.LogError($"\tAnomaly count for test '{testDefinition.Description}': {testDefinition.AnomalyCount}");
            }
        }
        public void DescribeForOpportunityToCancel()
        {
            logger.LogInformation($"Will time GetAllTags");
        }
    }
}

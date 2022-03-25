using MemCheck.Application;
using MemCheck.Application.Tags;
using MemCheck.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MemCheck.CommandLineDbClient.ApplicationQueryTester
{
    internal sealed class GetAllTagsPerfTests : ICmdLinePlugin
    {
        #region Fields
        private readonly ILogger<GetCardForEdit> logger;
        private readonly CallContext callContext;
        #endregion
        public GetAllTagsPerfTests(IServiceProvider serviceProvider)
        {
            logger = serviceProvider.GetRequiredService<ILogger<GetCardForEdit>>();
            callContext = serviceProvider.GetRequiredService<MemCheckDbContext>().AsCallContext();
        }
        async public Task RunAsync()
        {
            var chrono = Stopwatch.StartNew();

            var getAllTags = new GetAllTags(callContext);
            var request = new GetAllTags.Request(Guid.Empty, 200, 1, "");
            var result = (await getAllTags.RunAsync(request)).Tags.ToImmutableArray();

            logger.LogInformation($"Time: {chrono.Elapsed}");
            logger.LogInformation($"Obtained {result.Length} tags");
            foreach (var tag in result)
            {
                logger.LogInformation($"{tag.TagName}: {tag.CardCount} cards");
            }
        }
        public void DescribeForOpportunityToCancel()
        {
            logger.LogInformation($"Will time GetAllTags");
        }
    }
}

using MemCheck.Application;
using MemCheck.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.CommandLineDbClient.ApplicationQueryTester;

internal sealed class GetCardForEdit : ICmdLinePlugin
{
    #region Fields
    private readonly ILogger<GetCardForEdit> logger;
    private readonly CallContext callContext;
    #endregion
    public GetCardForEdit(IServiceProvider serviceProvider)
    {
        logger = serviceProvider.GetRequiredService<ILogger<GetCardForEdit>>();
        callContext = serviceProvider.GetRequiredService<MemCheckDbContext>().AsCallContext();
    }
    public async Task RunAsync()
    {
        var userId = callContext.DbContext.Users.Where(user => user.UserName == "Voltan").Single().Id;
        var cardId = callContext.DbContext.Cards.Where(card => !card.UsersWithView.Any() && card.Images.Any()).OrderBy(card => card.VersionUtcDate).First().Id;

        const int runCount = 20;

        var chronos = new List<double>();
        for (var i = 0; i < runCount; i++)
        {
            var request = new Application.Cards.GetCardForEdit.Request(userId, cardId);
            var runner = new Application.Cards.GetCardForEdit(callContext);
            var oneRunChrono = Stopwatch.StartNew();
            var card = await runner.RunAsync(request);
            logger.LogInformation($"Got a card with {card.CountOfUserRatings} ratings, {card.Tags.Count()} tags, {card.UsersOwningDeckIncluding.Count()} users, {card.UsersWithVisibility} users with access in {oneRunChrono.Elapsed}");
            chronos.Add(oneRunChrono.Elapsed.TotalSeconds);
        }


        logger.LogInformation($"Average time: {chronos.Average()} seconds");
    }
    public void DescribeForOpportunityToCancel()
    {
        logger.LogInformation($"Will request a card for edit mode");
    }
}

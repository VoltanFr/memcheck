using MemCheck.Application;
using MemCheck.Application.Heaping;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace MemCheck.CommandLineDbClient.ApplicationQueryTester
{
    internal sealed class SearchCards : IMemCheckTest
    {
        #region Fields
        private readonly ILogger<GetCards> logger;
        private readonly MemCheckDbContext dbContext;
        #endregion
        public SearchCards(IServiceProvider serviceProvider)
        {
            dbContext = serviceProvider.GetRequiredService<MemCheckDbContext>();
            logger = serviceProvider.GetRequiredService<ILogger<GetCards>>();
        }
        async public Task RunAsync(MemCheckDbContext dbContext)
        {
            var user = dbContext.Users.Where(user => user.UserName == "Voltan").Single().Id;
            var deckId = dbContext.Decks.Where(deck => deck.Owner.Id == user).First().Id;

            var chronos = new List<double>();

            for (int i = 0; i < 5; i++)
            {
                var request = new Application.Searching.SearchCards.Request(user, Guid.Empty, true, null, 1, 3000, "", new Guid[0].ToImmutableArray(), new Guid[0].ToImmutableArray(), Application.Searching.SearchCards.Request.VibilityFiltering.CardsVisibleByMoreThanOwner, Application.Searching.SearchCards.Request.RatingFilteringMode.Ignore, 0, Application.Searching.SearchCards.Request.NotificationFiltering.Ignore);
                var runner = new Application.Searching.SearchCards(dbContext);
                var realCodeChrono = Stopwatch.StartNew();
                var result = runner.Run(request);
                chronos.Add(realCodeChrono.Elapsed.TotalSeconds);
                logger.LogInformation($"Got {result.TotalNbCards} cards in {realCodeChrono.Elapsed}");
                logger.LogInformation($"First card has {result.Cards.First().Tags.Count()} tags, the first tag is {result.Cards.First().Tags.First()}");
                logger.LogInformation($"Last card has {result.Cards.Last().VisibleTo.Count()} users with view, the first user is {result.Cards.Last().VisibleTo.First()}");
            }

            logger.LogInformation($"Average time: {chronos.Average()} seconds");

            await Task.CompletedTask;
        }
        public void DescribeForOpportunityToCancel()
        {
            logger.LogInformation($"Will search cards, as in the search page");
        }
    }
}

using MemCheck.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.CommandLineDbClient.ApplicationQueryTester
{
    internal sealed class SearchCards : ICmdLinePlugin
    {
        #region Fields
        private readonly ILogger<SearchCards> logger;
        private readonly MemCheckDbContext dbContext;
        #endregion
        public SearchCards(IServiceProvider serviceProvider)
        {
            dbContext = serviceProvider.GetRequiredService<MemCheckDbContext>();
            logger = serviceProvider.GetRequiredService<ILogger<SearchCards>>();
        }
        private async Task<(int cardCount, double secondsElapsed)> RunTestAsync(Guid userId)
        {
            var request = new Application.Searching.SearchCards.Request
            {
                UserId = userId,
                PageSize = Application.Searching.SearchCards.Request.MaxPageSize,
                RatingFiltering = Application.Searching.SearchCards.Request.RatingFilteringMode.AtLeast,
                RatingFilteringValue = 5,
                Visibility = Application.Searching.SearchCards.Request.VibilityFiltering.CardsVisibleByMoreThanOwner
            };
            var runner = new Application.Searching.SearchCards(dbContext.AsCallContext());
            var chrono = Stopwatch.StartNew();
            var result = await runner.RunAsync(request);
            return new(result.TotalNbCards, chrono.Elapsed.TotalSeconds);

        }
        public async Task RunAsync()
        {
            var user = dbContext.Users.Where(user => user.UserName == "Toto1").Single().Id;
            var deckId = dbContext.Decks.Where(deck => deck.Owner.Id == user).First().Id;

            var expectedCardCount = (await RunTestAsync(user)).cardCount;

            var chronos = new List<double>();

            for (int i = 0; i < 5; i++)
            {
                (int cardCount, double secondsElapsed) = await RunTestAsync(user);

                if (cardCount != expectedCardCount)
                    throw new InvalidProgramException($"Expected {expectedCardCount} cards, got {cardCount}");

                chronos.Add(secondsElapsed);
                logger.LogInformation($"Got {cardCount} cards in {secondsElapsed} seconds");
            }

            logger.LogInformation($"Average time: {chronos.Average()} seconds");
        }
        public void DescribeForOpportunityToCancel()
        {
            logger.LogInformation($"Will search cards, as in the search page");
        }
    }
}

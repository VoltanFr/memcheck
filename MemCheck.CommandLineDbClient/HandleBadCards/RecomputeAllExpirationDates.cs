using MemCheck.Application.Heaping;
using MemCheck.Basics;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.CommandLineDbClient.HandleBadCards
{
    internal sealed class RecomputeAllExpirationDates : IMemCheckTest
    {
        #region Fields
        private readonly ILogger<RecomputeAllExpirationDates> logger;
        private readonly MemCheckDbContext dbContext;
        #endregion
        public RecomputeAllExpirationDates(IServiceProvider serviceProvider)
        {
            dbContext = serviceProvider.GetRequiredService<MemCheckDbContext>();
            logger = serviceProvider.GetRequiredService<ILogger<RecomputeAllExpirationDates>>();
        }
        public void DescribeForOpportunityToCancel()
        {
            logger.LogInformation($"Will recompute all expiration dates");
        }
        async public Task RunAsync(MemCheckDbContext dbContext)
        {
            var user = dbContext.Users.Where(user => user.UserName == "Voltan").Single().Id;
            var deck = dbContext.Decks.Where(deck => deck.Owner.Id == user).First().Id;
            var algo = await HeapingAlgorithm.OfDeckAsync(dbContext, deck);

            var allCardsWithNoExpirationDate = dbContext.CardsInDecks.Where(c => c.DeckId == deck && c.ExpiryUtcTime == DateTime.MinValue && c.CurrentHeap != CardInDeck.UnknownHeap).OrderBy(c => c.LastLearnUtcTime).Take(10000);
            int count = allCardsWithNoExpirationDate.Count();
            logger.LogInformation($"Will recompute expiration dates of {count} cards");

            foreach (var card in allCardsWithNoExpirationDate)
            {
                var expiryDate = algo.ExpiryUtcDate(card.CurrentHeap, card.LastLearnUtcTime);
                card.ExpiryUtcTime = expiryDate;
            }

            await dbContext.SaveChangesAsync();

            logger.LogInformation($"Expiration dates updating of {count} cards finished");
        }
    }
}

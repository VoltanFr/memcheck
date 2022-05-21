using MemCheck.Application.Heaping;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.CommandLineDbClient.HandleBadCards;

internal sealed class RecomputeAllExpirationDates : ICmdLinePlugin
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
    public async Task RunAsync()
    {
        var user = dbContext.Users.Where(user => user.UserName == "Voltan").Single().Id;
        var deck = dbContext.Decks.Where(deck => deck.Owner.Id == user).First().Id;
        var algo = await HeapingAlgorithm.OfDeckAsync(dbContext, deck);
        logger.LogInformation($"Algo: {algo.GetType().Name}, id: {algo.Id}");

        var allCardsNotUnkown = dbContext.CardsInDecks.Where(c => c.DeckId == deck && c.CurrentHeap != CardInDeck.UnknownHeap).OrderBy(c => c.LastLearnUtcTime);
        int count = allCardsNotUnkown.Count();
        logger.LogInformation($"Will recompute expiration dates of {count} cards");

        var doneCount = 0;

        foreach (var card in allCardsNotUnkown)
        {
            var initialExpiryDate = card.ExpiryUtcTime;
            var expiryDate = algo.ExpiryUtcDate(card.CurrentHeap, card.LastLearnUtcTime);
            doneCount++;
            logger.LogInformation($"Will update card {doneCount} of {count} in heap {card.CurrentHeap} expiry from {initialExpiryDate} to {expiryDate}");
            card.ExpiryUtcTime = expiryDate;
        }

        var dbUpdateCount = await dbContext.SaveChangesAsync();

        logger.LogInformation($"Expiration dates updating of {count} cards finished - dbUpdateCount: {dbUpdateCount}");
    }
}

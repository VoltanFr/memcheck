using MemCheck.CommandLineDbClient.Pauker;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.CommandLineDbClient.HandleBadCards;

internal sealed class ListCardsWithVisibilityConsistencyProblem : ICmdLinePlugin
{
    #region Fields
    private readonly ILogger<PaukerImportTest> logger;
    private readonly MemCheckDbContext dbContext;
    #endregion
    public ListCardsWithVisibilityConsistencyProblem(IServiceProvider serviceProvider)
    {
        dbContext = serviceProvider.GetRequiredService<MemCheckDbContext>();
        logger = serviceProvider.GetRequiredService<ILogger<PaukerImportTest>>();
    }
    public void DescribeForOpportunityToCancel()
    {
        logger.LogInformation("Will change visibility of cards currently private but must be public because used by other users");
    }
    public async Task RunAsync()
    {
        var user = dbContext.Users.Where(user => user.UserName == "Voltan").Single();

        var cardsToFix = await dbContext.CardsInDecks
            .Where(cardInDeck => cardInDeck.Card.UsersWithView.Any() && !cardInDeck.Card.UsersWithView.Any(userWithView => userWithView.UserId == cardInDeck.Deck.Owner.Id))
            .Select(cardInDeck => new { cardInDeck.CardId, UserId = cardInDeck.Deck.Owner.Id })
            .ToListAsync();

        logger.LogInformation($"Found {cardsToFix.Count} cards to fix");
        foreach (var cardId in cardsToFix.Select(c => c.CardId))
            logger.LogInformation($"\t {cardId}");

        logger.LogInformation($"Finished");
    }
}

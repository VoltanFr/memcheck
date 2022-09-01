using MemCheck.Application.Cards;
using MemCheck.CommandLineDbClient.Pauker;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.CommandLineDbClient.Referencing;

internal sealed class AddReferences : ICmdLinePlugin
{
    #region Fields
    private readonly ILogger<PaukerImportTest> logger;
    private readonly MemCheckDbContext dbContext;
    #endregion
    private static UpdateCard.Request RequestForReferencesChange(Card card, string references, Guid versionCreator, string versionDescription)
    {
        return new UpdateCard.Request(
            card.Id,
            versionCreator,
            card.FrontSide,
            card.BackSide,
            card.AdditionalInfo,
            references,
            card.CardLanguage.Id,
            card.TagsInCards.Select(t => t.TagId).ToImmutableArray(),
            card.UsersWithView.Select(uwv => uwv.UserId).ToImmutableArray(),
            versionDescription);
    }
    public AddReferences(IServiceProvider serviceProvider)
    {
        dbContext = serviceProvider.GetRequiredService<MemCheckDbContext>();
        logger = serviceProvider.GetRequiredService<ILogger<PaukerImportTest>>();
    }
    public void DescribeForOpportunityToCancel()
    {
        logger.LogInformation($"Will add references");
    }
    public async Task RunAsync()
    {
        var userId = await dbContext.Users.Where(u => u.UserName == "VoltanBot").Select(user => user.Id).SingleAsync();
        var tagId = await dbContext.Tags.Where(tag => tag.Name == "Échelle de Beaufort").Select(tag => tag.Id).SingleAsync();

        var cardsToUpdate = dbContext.Cards
            .Include(card => card.Images)
            .Include(card => card.UsersWithView)
            .Include(card => card.TagsInCards)
            .Include(card => card.CardLanguage)
            .Where(card => card.TagsInCards.Any(tag => tag.TagId == tagId))
            .Where(card => card.FrontSide.StartsWith("Comment s'appelle la force juste au-dessus de"))
            .Where(card => card.References.Length == 0)
            .Where(card => !card.UsersWithView.Any())
            .ToImmutableArray();

        logger.LogInformation($"There are {cardsToUpdate.Length} cards to update");

        foreach (var card in cardsToUpdate)
        {
            var updater = new UpdateCard(dbContext.AsCallContext());
            var request = RequestForReferencesChange(card, "[Wikipédia : Échelle de Beaufort](https://fr.wikipedia.org/wiki/%C3%89chelle_de_Beaufort).", userId, "Ajout de la référence Wikipédia");
            await updater.RunAsync(request);
            logger.LogInformation($"Card {card.Id} updated");
        }
    }
}

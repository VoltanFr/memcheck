using MemCheck.Application.Ratings;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.CommandLineDbClient.Ratings;

internal sealed class RateAllPublicCards : ICmdLinePlugin
{
    #region Fields
    private readonly ILogger<RateAllBotCards> logger;
    private readonly MemCheckDbContext dbContext;
    private readonly Guid userId;
    private readonly Guid englishVocabularyTagId;
    private readonly ImmutableDictionary<Guid, int> userRatings;
    #endregion
    #region Private record CardFromDb
    private sealed record CardFromDb(Guid Id, string FrontSide, string AdditionalInfo, string References, ImmutableHashSet<Guid> TagIds);
    #endregion
    #region Private methods
    private async Task RateEnglishCardsAsync()
    {
        var cards = dbContext.Cards
            .AsNoTracking()
            .Where(card => !card.UsersWithView.Any() && card.TagsInCards.Any(tag => tag.TagId == englishVocabularyTagId))
            .Select(card => new { card.Id, card.FrontSide, card.AdditionalInfo })
            .ToImmutableArray();

        logger.LogInformation($"{cards.Length} English cards");
        var rater = new SetCardRating(dbContext.AsCallContext());
        var i = 1;

        foreach (var card in cards)
        {
            var ratingToSetForCard = card.AdditionalInfo.Length > 0 ? 5 : 3;
            if (!userRatings.TryGetValue(card.Id, out var existingRating) || ratingToSetForCard != existingRating)
            {
                logger.LogInformation($"[English {i} of {cards.Length}]: setting rating {ratingToSetForCard} on {card.Id} ({card.FrontSide})");
                var request = new SetCardRating.Request(userId, card.Id, ratingToSetForCard);
                await rater.RunAsync(request);
            }
            i++;
        }

        logger.LogInformation($"English rating finished");
    }
    private async Task RateNonEnglishCardsAsync()
    {
        var cards = dbContext.Cards
            .AsNoTracking()
            .Where(card => !card.UsersWithView.Any() && !card.TagsInCards.Any(tag => tag.TagId == englishVocabularyTagId))
            .Select(card => new { card.Id, card.FrontSide, card.AdditionalInfo, card.References })
            .ToImmutableArray();

        logger.LogInformation($"{cards.Length} non-English cards");
        var rater = new SetCardRating(dbContext.AsCallContext());
        var i = 1;

        foreach (var card in cards)
        {
            var ratingToSetForCard = card.AdditionalInfo.Length == 0 ? 3 : (card.References.Length == 0 ? 4 : 5);
            if (!userRatings.TryGetValue(card.Id, out var existingRating) || ratingToSetForCard != existingRating)
            {
                logger.LogInformation($"[Non-English {i} of {cards.Length}]: setting rating {ratingToSetForCard} on {card.Id} ({card.FrontSide})");
                var request = new SetCardRating.Request(userId, card.Id, ratingToSetForCard);
                await rater.RunAsync(request);
            }
            i++;
        }

        logger.LogInformation($"Other than English rating finished");
    }
    #endregion
    public RateAllPublicCards(IServiceProvider serviceProvider)
    {
        dbContext = serviceProvider.GetRequiredService<MemCheckDbContext>();
        logger = serviceProvider.GetRequiredService<ILogger<RateAllBotCards>>();
        userId = dbContext.Users.Single(u => u.UserName == "VoltanBot").Id;
        englishVocabularyTagId = dbContext.Tags.Single(tag => tag.Name == "Vocabulaire anglais").Id;
        userRatings = dbContext.UserCardRatings.AsNoTracking().Where(rating => rating.UserId == userId).ToImmutableDictionary(rating => rating.CardId, rating => rating.Rating);
    }
    public void DescribeForOpportunityToCancel()
    {
        logger.LogInformation($"Will rate all public cards");
    }
    public async Task RunAsync()
    {
        await RateEnglishCardsAsync();
        await RateNonEnglishCardsAsync();
    }
}

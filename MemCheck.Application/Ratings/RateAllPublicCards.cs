using MemCheck.Application.QueryValidation;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Ratings;

//Current implementation does not take care of concurrency: if the same user account was used in parallel to set a rating just at the same time on a card, the final rating would be unpredictable

public sealed class RateAllPublicCards : RequestRunner<RateAllPublicCards.Request, RateAllPublicCards.Result>
{
    public RateAllPublicCards(CallContext callContext) : base(callContext)
    {
    }
    protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
    {
        var userRatings = DbContext.UserCardRatings.AsNoTracking().Where(rating => rating.UserId == request.UserId).ToImmutableDictionary(rating => rating.CardId, rating => rating.Rating);

        var cards = DbContext.Cards
         .AsNoTracking()
         .Where(card => !card.UsersWithView.Any())
         .Select(card => new { card.Id, card.AdditionalInfo, card.References })
         .ToImmutableArray();

        var changedRatingsCount = 0;

        foreach (var card in cards)
        {
            var ratingToSetForCard = card.AdditionalInfo.Length == 0 ? 3 : (card.References.Length == 0 ? 4 : 5);

            var ratingExists = userRatings.TryGetValue(card.Id, out var existingRating);

            if (!ratingExists)
            {
                changedRatingsCount++;
                DbContext.UserCardRatings.Add(new UserCardRating() { UserId = request.UserId, CardId = card.Id, Rating = ratingToSetForCard });
            }
            else
            if (ratingToSetForCard != existingRating)
            {
                var existingRatingByUser = await DbContext.UserCardRatings.Where(rating => rating.UserId == request.UserId && rating.CardId == card.Id).SingleAsync();
                existingRatingByUser.Rating = ratingToSetForCard;
                changedRatingsCount++;
            }
        }

        await DbContext.SaveChangesAsync();
        var result = new Result(cards.Length, changedRatingsCount);

        return new ResultWithMetrologyProperties<Result>(result, IntMetric("PublicCardCount", result.PublicCardCount), IntMetric("ChangedRatingCount", result.ChangedRatingCount));
    }
    #region Request & Result
    public sealed record Request(Guid UserId) : IRequest
    {
        public async Task CheckValidityAsync(CallContext callContext)
        {
            await QueryValidationHelper.CheckUserExistsAsync(callContext.DbContext, UserId);
        }
    }
    public sealed record Result(int PublicCardCount, int ChangedRatingCount);
    #endregion
}

using Microsoft.EntityFrameworkCore;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Ratings;

public sealed class RefreshAverageRatings : RequestRunner<RefreshAverageRatings.Request, RefreshAverageRatings.Result>
{
    public RefreshAverageRatings(CallContext callContext) : base(callContext)
    {
    }
    protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
    {
        var cardsFromDb = (await DbContext.Cards.ToDictionaryAsync(card => card.Id, card => card)).ToImmutableDictionary();

        var averageRatingsFromRealValues = (await DbContext.UserCardRatings.AsNoTracking()
            .GroupBy(userCardRating => userCardRating.CardId)
            .Select(group => new { CardId = group.Key, AverageRating = group.Average(g => g.Rating) })
            .ToDictionaryAsync(averageCardRating => averageCardRating.CardId, averageCardRating => averageCardRating.AverageRating))
            .ToImmutableDictionary();

        var changedAverageRatingCount = 0;
        foreach (var cardId in cardsFromDb.Keys)
        {
            var newAverage = averageRatingsFromRealValues.ContainsKey(cardId) ? averageRatingsFromRealValues[cardId] : 0;

            if (cardsFromDb[cardId].AverageRating != newAverage)
            {
                cardsFromDb[cardId].AverageRating = newAverage;
                changedAverageRatingCount++;
            }
        }

        await DbContext.SaveChangesAsync();

        var result = new Result(cardsFromDb.Count, changedAverageRatingCount);

        return new ResultWithMetrologyProperties<Result>(result,
            IntMetric("TotalCardCountInDb", result.TotalCardCountInDb),
            IntMetric("ChangedAverageRatingCount", result.ChangedAverageRatingCount));
    }
    #region Request & Result
    public sealed record Request : IRequest
    {
        public async Task CheckValidityAsync(CallContext callContext)
        {
            await Task.CompletedTask;
        }
    }
    public sealed record Result(int TotalCardCountInDb, int ChangedAverageRatingCount);
    #endregion
}

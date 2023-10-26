using MemCheck.Basics;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards;

public sealed class GetAllPublicCards : RequestRunner<GetAllPublicCards.Request, GetAllPublicCards.Result>
{
    public GetAllPublicCards(CallContext callContext) : base(callContext)
    {
    }
    protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
    {
        var resultCards = await DbContext.Cards
            .AsNoTracking()
            .Where(card => !card.UsersWithView.Any())
            .Select(card => new ResultCard(card.Id, card.FrontSide, card.BackSide, card.VersionUtcDate, card.AverageRating))
            .ToImmutableArrayAsync();

        var result = new Result(resultCards);

        return new ResultWithMetrologyProperties<Result>(result, IntMetric("ResultCardCount", result.Cards.Length));
    }
    #region Request and result classes
    public sealed record Request : IRequest
    {
        public async Task CheckValidityAsync(CallContext callContext)
        {
            await Task.CompletedTask;
        }
    }
    public sealed record Result(ImmutableArray<ResultCard> Cards);
    public sealed record ResultCard(Guid CardId, string FrontSide, string BackSide, DateTime VersionUtcDate, double AverageRating); // AverageRating is 0 if no rating
    #endregion
}

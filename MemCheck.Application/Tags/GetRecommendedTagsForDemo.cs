using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Tags;

public sealed class GetRecommendedTagsForDemo : RequestRunner<GetRecommendedTagsForDemo.Request, GetRecommendedTagsForDemo.Result>
{
    public GetRecommendedTagsForDemo(CallContext callContext) : base(callContext)
    {
    }
    protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
    {
        var resultTags = DbContext.Tags
            .AsNoTracking()
            .Where(tag => tag.CountOfPublicCards >= request.RequiredCountOfPublicCards && tag.AverageRatingOfPublicCards >= request.RequiredAverageRatingOfPublicCards)
            .Select(tag => new ResultTag(tag.Id, tag.Name))
            .ToImmutableArray();

        await Task.CompletedTask;

        return new ResultWithMetrologyProperties<Result>(new Result(resultTags), IntMetric("Tagcount", resultTags.Length));
    }
    #region Request & Result
    public sealed record Request(int RequiredCountOfPublicCards, double RequiredAverageRatingOfPublicCards) : IRequest
    {
        public async Task CheckValidityAsync(CallContext callContext)
        {
            if (RequiredCountOfPublicCards is < 10 or > 300)
                throw new InvalidProgramException($"Inconsistent RequiredCountOfPublicCards: {RequiredCountOfPublicCards}");
            if (RequiredAverageRatingOfPublicCards is < 3 or > 5)
                throw new InvalidProgramException($"Inconsistent RequiredAverageRatingOfPublicCards: {RequiredAverageRatingOfPublicCards}");
            await Task.CompletedTask;
        }
    }
    public sealed record Result(ImmutableArray<ResultTag> Tags);
    public sealed record ResultTag(Guid TagId, string TagName);
    #endregion
}

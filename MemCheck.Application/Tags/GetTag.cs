using MemCheck.Application.QueryValidation;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Tags;

public sealed class GetTag : RequestRunner<GetTag.Request, GetTag.Result>
{
    public GetTag(CallContext callContext) : base(callContext)
    {
    }
    protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
    {
        var tag = await DbContext.Tags.AsNoTracking().Include(tag => tag.CreatingUser).Include(tag => tag.TagsInCards).SingleAsync(tag => tag.Id == request.TagId);
        var result = new Result(tag.Id, tag.Name, tag.Description, tag.CreatingUser.GetUserName(), tag.TagsInCards == null ? 0 : tag.TagsInCards.Count, tag.VersionUtcDate);
        return new ResultWithMetrologyProperties<Result>(result, ("TagName", result.TagName), IntMetric("CardCount", result.CardCount));
    }
    #region Request & Result
    public sealed record Request(Guid TagId) : IRequest
    {
        public async Task CheckValidityAsync(CallContext callContext)
        {
            QueryValidationHelper.CheckNotReservedGuid(TagId);
            await Task.CompletedTask;
        }
    }

    public sealed record Result(Guid TagId, string TagName, string Description, string CreatingUserName, int CardCount, DateTime VersionUtcDate);
    #endregion
}

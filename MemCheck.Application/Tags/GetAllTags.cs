using MemCheck.Application.QueryValidation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Tags
{
    public sealed class GetAllTags : RequestRunner<GetAllTags.Request, GetAllTags.Result>
    {
        public GetAllTags(CallContext callContext) : base(callContext)
        {
        }
        protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
        {
            //Since there are not a lot of tags, we just load them all into RAM

            var allTags = DbContext.Tags
                .AsNoTracking()
                .Where(tag => EF.Functions.Like(tag.Name, $"%{request.Filter}%"))
                .Select(tag => new ResultTag(tag.Id, tag.Name, tag.Description, tag.CountOfPublicCards, tag.AverageRatingOfPublicCards)).ToImmutableArray();

            var totalCount = allTags.Length;
            var pageCount = (int)Math.Ceiling((double)totalCount / request.PageSize);
            var pageTags = allTags
                .OrderBy(tag => tag.TagName)
                .Skip((request.PageNo - 1) * request.PageSize)
                .Take(request.PageSize);

            var result = new Result(totalCount, pageCount, pageTags);

            await Task.CompletedTask;

            return new ResultWithMetrologyProperties<Result>(result,
                ("PageSize", request.PageSize.ToString()),
                ("PageNo", request.PageNo.ToString()),
                ("FilterLength", request.Filter.Length.ToString()),
                ("TotalCount", result.TotalCount.ToString()),
                ("PageCount", result.PageCount.ToString()),
                ("TagCount", result.Tags.Count().ToString()));
        }
        #region Request & Result
        public sealed record Request(int PageSize, int PageNo, string Filter) : IRequest
        {
            public const int MaxPageSize = 500;
            public async Task CheckValidityAsync(CallContext callContext)
            {
                if (PageNo < 1)
                    throw new RequestInputException($"First page is numbered 1, received a request for page {PageNo}");
                if (PageSize < 1)
                    throw new RequestInputException($"PageSize too small: {PageSize} (max size: {MaxPageSize})");
                if (PageSize > MaxPageSize)
                    throw new RequestInputException($"PageSize too big: {PageSize} (max size: {MaxPageSize})");
                await Task.CompletedTask;
            }
        }
        public sealed class Result
        {
            public Result(int totalCount, int pageCount, IEnumerable<ResultTag> tags)
            {
                TotalCount = totalCount;
                PageCount = pageCount;
                Tags = tags;
            }
            public int TotalCount { get; }
            public int PageCount { get; }
            public IEnumerable<ResultTag> Tags { get; }
        }
        public sealed record ResultTag(Guid TagId, string TagName, string TagDescription, int CardCount, double AverageRating);
        #endregion
    }
}


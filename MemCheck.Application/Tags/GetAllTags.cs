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
            //Code tuned for perf. Understand and use GetAllTagsPerfTests before making changes

            var allTagsForDetails = DbContext.Tags.AsNoTracking();
            var allTagsForDetailsFiltered = request.Filter.Length > 0
                ? allTagsForDetails.Where(tag => EF.Functions.Like(tag.Name, $"%{request.Filter}%"))
                : allTagsForDetails;
            var allTagDetails = allTagsForDetailsFiltered
                .Select(tag => new { tag.Id, tag.Name, tag.Description })
                .ToImmutableArray();

            var allTags = DbContext.TagsInCards.Include(tagInCard => tagInCard.Card).AsNoTracking();
            var forUser = request.UserId == Guid.Empty
                ? allTags.Where(tagInCard => !tagInCard.Card.UsersWithView.Any())
                : allTags.Where(tagInCard => !tagInCard.Card.UsersWithView.Any() || tagInCard.Card.UsersWithView.Any(userWithView => userWithView.UserId == request.UserId));
            var filtered = request.Filter.Length > 0
                ? forUser.Where(tagInCard => EF.Functions.Like(tagInCard.Tag.Name, $"%{request.Filter}%"))
                : forUser;
            var statsOfTagCards = filtered
                .GroupBy(tagInCard => tagInCard.TagId)
                .Select(group => new { TagId = group.Key, Count = group.Count(), AverageRating = group.Average(tagInCard => tagInCard.Card.AverageRating) })
                .ToImmutableDictionary(tagStats => tagStats.TagId, keyAndCount => new { keyAndCount.Count, keyAndCount.AverageRating });

            var resultTags = new List<ResultTag>();
            foreach (var tag in allTagDetails)
            {
                statsOfTagCards.TryGetValue(tag.Id, out var tagStats);
                resultTags.Add(new ResultTag(tag.Id, tag.Name, tag.Description, tagStats == null ? 0 : tagStats.Count, tagStats == null ? 0 : tagStats.AverageRating));
            }

            var totalCount = allTagDetails.Length;
            var pageCount = (int)Math.Ceiling((double)totalCount / request.PageSize);
            var pageTags = resultTags
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
        public sealed record Request(Guid UserId, int PageSize, int PageNo, string Filter) : IRequest
        {
            public const int MaxPageSize = 500;
            public async Task CheckValidityAsync(CallContext callContext)
            {
                if (UserId != Guid.Empty)
                    await QueryValidationHelper.CheckUserExistsAsync(callContext.DbContext, UserId);
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


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
            var tags = DbContext.Tags
                .AsNoTracking()
                .Include(tag => tag.TagsInCards)
                .ThenInclude(tagInCard => tagInCard.Card)
                .ThenInclude(card => card.UsersWithView)
                .Where(tag => EF.Functions.Like(tag.Name, $"%{request.Filter}%"))
                .OrderBy(tag => tag.Name);
            var totalCount = await tags.CountAsync();
            var pageCount = (int)Math.Ceiling((double)totalCount / request.PageSize);
            var pageTags = tags.Skip((request.PageNo - 1) * request.PageSize).Take(request.PageSize);
            var resultTagsWithoutCardCount = pageTags.Select(tag => new { tag.Id, tag.Name, tag.Description, tag.TagsInCards }).ToImmutableArray();

            var resultTags = new List<ResultTag>();
            foreach (var tagWithoutCardCount in resultTagsWithoutCardCount)
            {
                var cardCount = 0;
                foreach (var tagInCard in tagWithoutCardCount.TagsInCards)
                    if (CardVisibilityHelper.CardIsVisibleToUser(request.UserId, tagInCard.Card))
                        cardCount++;
                resultTags.Add(new ResultTag(tagWithoutCardCount.Id, tagWithoutCardCount.Name, tagWithoutCardCount.Description, cardCount));
            }

            var result = new Result(totalCount, pageCount, resultTags);
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
        public sealed class ResultTag
        {
            public ResultTag(Guid tagId, string tagName, string tagDescription, int cardCount)
            {
                TagId = tagId;
                TagName = tagName;
                TagDescription = tagDescription;
                CardCount = cardCount;
            }
            public Guid TagId { get; }
            public string TagName { get; }
            public string TagDescription { get; }
            public int CardCount { get; }
        }
        #endregion
    }
}


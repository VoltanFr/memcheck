using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Tags
{
    public sealed class GetAllTags
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public GetAllTags(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task<Result> RunAsync(Request request)
        {
            request.CheckValidity();
            var tags = dbContext.Tags.Where(tag => EF.Functions.Like(tag.Name, $"%{request.Filter}%")).OrderBy(tag => tag.Name);
            var totalCount = await tags.CountAsync();
            var pageCount = (int)Math.Ceiling((double)totalCount / request.PageSize);
            var pageTags = tags.Skip((request.PageNo - 1) * request.PageSize).Take(request.PageSize);
            return new Result(totalCount, pageCount, pageTags.Select(tag => new ResultTag(tag.Id, tag.Name, tag.TagsInCards.Count)));
        }

        #region Request type
        public sealed record Request(int PageSize, int PageNo, string Filter)
        {
            public const int MaxPageSize = 500;
            public void CheckValidity()
            {
                if (PageNo < 1)
                    throw new RequestInputException($"First page is numbered 1, received a request for page {PageNo}");
                if (PageSize < 1)
                    throw new RequestInputException($"PageSize too small: {PageSize} (max size: {MaxPageSize})");
                if (PageSize > MaxPageSize)
                    throw new RequestInputException($"PageSize too big: {PageSize} (max size: {MaxPageSize})");
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
            public ResultTag(Guid tagId, string tagName, int cardCount)
            {
                TagId = tagId;
                TagName = tagName;
                CardCount = cardCount;
            }
            public Guid TagId { get; }
            public string TagName { get; } = null!;
            public int CardCount { get; }
        }
        #endregion
    }
}


using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MemCheck.Application
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
        public ResultModel Run(int pageSize, int pageNo, string filter)
        {
            var tags = dbContext.Tags.Where(tag => EF.Functions.Like(tag.Name, $"%{filter}%")).OrderBy(tag => tag.Name);
            var totalCount = tags.Count();
            var pageCount = (int)Math.Ceiling(((double)totalCount) / pageSize);
            var pageTags = tags.Skip((pageNo - 1) * pageSize).Take(pageSize);

            return new ResultModel(totalCount, pageCount, pageTags.Select(tag => new ResultTagModel(tag.Id, tag.Name, tag.TagsInCards.Count)));
        }
        public sealed class ResultModel
        {
            public ResultModel(int totalCount, int pageCount, IEnumerable<ResultTagModel> tags)
            {
                TotalCount = totalCount;
                PageCount = pageCount;
                Tags = tags;
            }
            public int TotalCount { get; }
            public int PageCount { get; }
            public IEnumerable<ResultTagModel> Tags { get; }
        }
        public sealed class ResultTagModel
        {
            public ResultTagModel(Guid tagId, string tagName, int cardCount)
            {
                TagId = tagId;
                TagName = tagName;
                CardCount = cardCount;
            }
            public Guid TagId { get; }
            public string TagName { get; } = null!;
            public int CardCount { get; }
        }
    }
}


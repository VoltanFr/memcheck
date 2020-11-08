using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MemCheck.Application
{
    public sealed class GetTag
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public GetTag(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public ResultModel Run(Guid tagId)
        {
            var tag = dbContext.Tags.Include(tag => tag.TagsInCards).Single(tag => tag.Id == tagId);
            return new ResultModel(tag.Id, tag.Name, tag.TagsInCards == null ? 0 : tag.TagsInCards.Count);
        }
        public sealed class ResultModel
        {
            public ResultModel(Guid tagId, string tagName, int cardCount)
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


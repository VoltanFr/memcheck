using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Tags
{
    public sealed class RefreshTagStats : RequestRunner<RefreshTagStats.Request, RefreshTagStats.Result>
    {
        public RefreshTagStats(CallContext callContext) : base(callContext)
        {
        }
        protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
        {
            var tagsBeforeRefresh = DbContext.Tags
                .AsNoTracking()
                .Select(tag => new ResultTag(tag.Id, tag.Name, tag.CountOfPublicCards, tag.AverageRatingOfPublicCards)).ToImmutableArray();

            var computedAverageRatings = DbContext.TagsInCards
                .AsNoTracking()
                .Include(tagInCard => tagInCard.Card)
                .Where(tagInCard => !tagInCard.Card.UsersWithView.Any())    //public cards
                .Where(tagInCard => tagInCard.Card.AverageRating != 0)    //with rating
                .GroupBy(tagInCard => tagInCard.TagId)
                .Select(group => new { TagId = group.Key, Count = group.Count(), AverageRating = group.Average(tagInCard => tagInCard.Card.AverageRating) })
                .ToImmutableDictionary(tagStats => tagStats.TagId, tagStats => tagStats.AverageRating);

            var computedCardCounts = DbContext.TagsInCards
                .AsNoTracking()
                .Include(tagInCard => tagInCard.Card)
                .Where(tagInCard => !tagInCard.Card.UsersWithView.Any())    //public cards
                .GroupBy(tagInCard => tagInCard.TagId)
                .Select(group => new { TagId = group.Key, Count = group.Count(), AverageRating = group.Average(tagInCard => tagInCard.Card.AverageRating) })
                .ToImmutableDictionary(tagStats => tagStats.TagId, tagStats => tagStats.Count);

            var result = new List<ResultTag>();
            foreach (var resultTag in tagsBeforeRefresh)
            {
                var averageRatingExists = computedAverageRatings.TryGetValue(resultTag.TagId, out var averageRating);
                var cardCountExists = computedCardCounts.TryGetValue(resultTag.TagId, out var cardCount);
                result.Add(resultTag with { CardCountAfterRun = cardCountExists ? cardCount : 0, AverageRatingAfterRun = averageRatingExists ? averageRating : 0 });

                var dbTag = DbContext.Tags.Single(tag => tag.Id == resultTag.TagId);
                dbTag.CountOfPublicCards = cardCountExists ? cardCount : 0;
                dbTag.AverageRatingOfPublicCards = averageRatingExists ? averageRating : 0;
            }

            await DbContext.SaveChangesAsync();

            return new ResultWithMetrologyProperties<Result>(new Result(result.ToImmutableArray()), IntMetric("TagCount", result.Count));
        }
        #region Request & Result
        public sealed record Request() : IRequest
        {
            public async Task CheckValidityAsync(CallContext callContext)
            {
                await Task.CompletedTask;
            }
        }
        public sealed class Result
        {
            public Result(ImmutableArray<ResultTag> tags)
            {
                Tags = tags;
            }
            public ImmutableArray<ResultTag> Tags { get; }
        }
        public sealed record ResultTag(Guid TagId, string TagName, int CardCountBeforeRun, double AverageRatingBeforeRun)
        {
            public int CardCountAfterRun { get; set; }
            public double AverageRatingAfterRun { get; set; }
        }
        #endregion
    }
}


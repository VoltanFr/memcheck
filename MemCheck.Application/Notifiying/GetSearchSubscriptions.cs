using MemCheck.Application.QueryValidation;
using MemCheck.Application.Tags;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Notifying
{
    public sealed class GetSearchSubscriptions : RequestRunner<GetSearchSubscriptions.Request, IEnumerable<GetSearchSubscriptions.Result>>
    {
        public GetSearchSubscriptions(CallContext callContext) : base(callContext)
        {
        }
        protected override async Task<ResultWithMetrologyProperties<IEnumerable<Result>>> DoRunAsync(Request request)
        {
            var tagDictionary = TagLoadingHelper.Run(DbContext);
            var deckDictionary = DbContext.Decks.AsNoTracking().Where(d => d.Owner.Id == request.UserId).Select(t => new { t.Id, t.Description }).ToImmutableDictionary(t => t.Id, t => t.Description);

            var queryResult = await DbContext.SearchSubscriptions.AsNoTracking()
                .Include(s => s.RequiredTags)
                .Include(s => s.ExcludedTags)
                .Where(search => search.UserId == request.UserId)
                .ToListAsync();

            var result = queryResult.Select(subscription => new Result(subscription, tagDictionary, deckDictionary, DbContext));
            return new ResultWithMetrologyProperties<IEnumerable<Result>>(result, IntMetric("ResultCount", result.Count()));
        }
        #region Request & Result
        public sealed record Request : IRequest
        {
            public Request(Guid userId)
            {
                UserId = userId;
            }
            public Guid UserId { get; }
            public async Task CheckValidityAsync(CallContext callContext)
            {
                QueryValidationHelper.CheckNotReservedGuid(UserId);
                await Task.CompletedTask;
            }
        }
        public sealed record Result
        {
            internal Result(SearchSubscription subscription, ImmutableDictionary<Guid, string> tagDictionary, ImmutableDictionary<Guid, string> deckDictionary, MemCheckDbContext dbContext)
            {
                Id = subscription.Id;
                Name = subscription.Name;
                ExcludedDeck = subscription.ExcludedDeck == Guid.Empty ? null : deckDictionary[subscription.ExcludedDeck];
                RequiredText = subscription.RequiredText;
                RequiredTags = subscription.RequiredTags.Select(t => tagDictionary[t.TagId]);
                ExcludeAllTags = subscription.ExcludeAllTags;
                ExcludedTags = subscription.ExcludedTags.Select(t => tagDictionary[t.TagId]);
                RegistrationUtcDate = subscription.RegistrationUtcDate;
                LastRunUtcDate = subscription.LastRunUtcDate;
                CardCountOnLastRun = dbContext.CardsInSearchResults.Where(card => card.SearchSubscriptionId == subscription.Id).Count();
            }
            public Guid Id { get; }
            public string Name { get; }
            public string? ExcludedDeck { get; } //null means ignore
            public string RequiredText { get; }
            public IEnumerable<string> RequiredTags { get; }
            public bool ExcludeAllTags { get; }
            public IEnumerable<string> ExcludedTags { get; }
            public DateTime RegistrationUtcDate { get; }
            public DateTime LastRunUtcDate { get; }
            public int CardCountOnLastRun { get; }
        }
        #endregion
    }
}

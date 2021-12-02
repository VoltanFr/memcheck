using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Notifying
{
    public sealed class SubscribeToSearch : RequestRunner<SubscribeToSearch.Request, SubscribeToSearch.Result>
    {
        public SubscribeToSearch(CallContext callContext) : base(callContext)
        {
        }
        protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
        {
            var now = DateTime.UtcNow;

            var search = new SearchSubscription
            {
                UserId = request.UserId,
                Name = request.Name,
                ExcludedDeck = request.ExcludedDeck,
                RequiredText = request.RequiredText,
                RegistrationUtcDate = now,
                LastRunUtcDate = DateTime.MinValue
            };

            DbContext.SearchSubscriptions.Add(search);

            var requiredTags = new List<RequiredTagInSearchSubscription>();
            foreach (var requiredTag in request.RequiredTags)
            {
                var required = new RequiredTagInSearchSubscription
                {
                    SearchSubscriptionId = search.Id,
                    TagId = requiredTag
                };
                DbContext.RequiredTagInSearchSubscriptions.Add(required);
                requiredTags.Add(required);
            }
            search.RequiredTags = requiredTags;

            if (request.ExcludedTags != null)
            {
                var excludedTags = new List<ExcludedTagInSearchSubscription>();
                foreach (var excludedTag in request.ExcludedTags)
                {
                    var excluded = new ExcludedTagInSearchSubscription
                    {
                        SearchSubscriptionId = search.Id,
                        TagId = excludedTag
                    };
                    excludedTags.Add(excluded);
                    DbContext.ExcludedTagInSearchSubscriptions.Add(excluded);
                }

                search.ExcludedTags = excludedTags;
                search.ExcludeAllTags = false;
            }
            else
                search.ExcludeAllTags = true;

            await DbContext.SaveChangesAsync();

            return new ResultWithMetrologyProperties<Result>(new Result(search.Id),
                ("HasAnExcludedDeck", (request.ExcludedDeck != Guid.Empty).ToString()),
                ("Name", request.Name),
                ("RequiredText", request.RequiredText),
                ("RequiredTagCount", request.RequiredTags.Count().ToString()),
                ("ExcludedTagCount", request.ExcludedTags == null ? "-1" : request.ExcludedTags.Count().ToString()),
                ("NameLength", request.Name.Length.ToString()));
        }
        #region Request & Result
        public sealed class Request : IRequest
        {
            public const int MaxSubscriptionCount = 5;
            public Request(Guid userId, Guid excludedDeck, string name, string requiredText, IEnumerable<Guid> requiredTags, IEnumerable<Guid>? excludedTags)
            {
                UserId = userId;
                Name = name;
                ExcludedDeck = excludedDeck;
                RequiredText = requiredText;
                RequiredTags = requiredTags;
                ExcludedTags = excludedTags;
            }
            public Guid UserId { get; }
            public string Name { get; }
            public Guid ExcludedDeck { get; } //Guid.Empty means ignore
            public string RequiredText { get; }
            public IEnumerable<Guid> RequiredTags { get; }
            public IEnumerable<Guid>? ExcludedTags { get; } //null means that we return only cards which have no tag (we exclude all tags)
            public async Task CheckValidityAsync(CallContext callContext)
            {
                if (QueryValidationHelper.IsReservedGuid(UserId))
                    throw new RequestInputException("Invalid user id");

                if (ExcludedDeck != Guid.Empty)
                {
                    var deck = callContext.DbContext.Decks.Include(d => d.Owner).SingleOrDefault(d => d.Id == ExcludedDeck);
                    if (deck == null)
                        throw new RequestInputException($"Invalid deck id '{ExcludedDeck}'");
                    if (deck.Owner.Id != UserId)
                        throw new RequestInputException($"Deck not allowed for user '{UserId}': '{ExcludedDeck}'");
                }

                if (ExcludedTags != null)
                {
                    foreach (var excludedTag in ExcludedTags)
                        if (!await callContext.DbContext.Tags.AnyAsync(t => t.Id == excludedTag))
                            throw new RequestInputException($"Invalid excluded tag id '{excludedTag}'");
                    if (ExcludedTags.GroupBy(guid => guid).Where(guid => guid.Count() > 1).Any())
                        throw new RequestInputException("Excluded tag list contains duplicate");
                }

                foreach (var requiredTag in RequiredTags)
                    if (!callContext.DbContext.Tags.Any(t => t.Id == requiredTag))
                        throw new RequestInputException($"Invalid required tag id '{requiredTag}'");

                if (RequiredTags.GroupBy(guid => guid).Where(guid => guid.Count() > 1).Any())
                    throw new RequestInputException("Required tag list contains duplicate");

                int userSubscriptionsCount = callContext.DbContext.SearchSubscriptions.Count(s => s.UserId == UserId);
                if (userSubscriptionsCount >= MaxSubscriptionCount)
                    throw new RequestInputException($"User already has {userSubscriptionsCount} subscriptions, can not have more than {MaxSubscriptionCount}");
            }
        }
        public sealed record Result(Guid SearchId);
        #endregion
    }
}

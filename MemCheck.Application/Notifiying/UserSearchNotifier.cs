using MemCheck.Database;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using MemCheck.Application.Searching;
using System.Linq;
using MemCheck.Domain;
using MemCheck.Application.QueryValidation;

namespace MemCheck.Application.Notifying
{
    /// <summary> This notifier reports cards which appear or disappear in the given search, compared to the previous run.
    /// Here is the complete list of possibilities of events which bring this situation if they have occurred since last execution:
    /// - A card has been created
    /// - A new card version has been created which makes the card match the search
    /// - A card has been deleted.
    /// All this cases are covered by the unit test class.
    /// </summary>

    internal interface IUserSearchNotifier
    {
        public Task<UserSearchNotifierResult> RunAsync(Guid searchSubscriptionId);
    }
    internal sealed class UserSearchNotifier : IUserSearchNotifier
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        private readonly DateTime runningUtcDate;
        private readonly int maxCountToReport;
        #endregion
        #region Private methods
        private SearchCards.Request GetRequest(SearchSubscription subscription)
        {
            var request = new SearchCards.Request
            {
                UserId = subscription.UserId,
                RequiredText = subscription.RequiredText,
                RequiredTags = subscription.RequiredTags.Select(t => t.TagId),
                PageSize = SearchCards.Request.MaxPageSize,
                PageNo = 0
            };

            if (subscription.excludeAllTags)
                request = request with { ExcludedTags = null };
            else
                request = request with { ExcludedTags = subscription.ExcludedTags.Select(t => t.TagId) };

            if (subscription.ExcludedDeck != Guid.Empty)
                request = request with { Deck = subscription.ExcludedDeck, DeckIsInclusive = false };

            return request;
        }
        #endregion
        public UserSearchNotifier(MemCheckDbContext dbContext, int maxCountToReport, DateTime? runningUtcDate = null)
        {
            this.dbContext = dbContext;
            this.runningUtcDate = runningUtcDate ?? DateTime.UtcNow;
            this.maxCountToReport = maxCountToReport;
        }
        public async Task<UserSearchNotifierResult> RunAsync(Guid searchSubscriptionId)
        {
            var subscription = await dbContext.SearchSubscriptions
                .Include(s => s.ExcludedTags)
                .Include(s => s.RequiredTags)
                .Include(s => s.CardsInLastRun)
                .SingleAsync(s => s.Id == searchSubscriptionId);
            var cardsInLastRun = subscription.CardsInLastRun.Select(c => c.CardId).ToImmutableHashSet();

            var allCardsFromSearchHashSet = new HashSet<CardVersion>();
            SearchCards.Request request = GetRequest(subscription);

            SearchCards.Result searchResult;
            do
            {
                request = request with { PageNo = request.PageNo + 1 };
                var searcher = new SearchCards(dbContext);
                searchResult = await searcher.RunAsync(request);
                allCardsFromSearchHashSet.UnionWith(searchResult.Cards.Select(card => new CardVersion(card.CardId, card.FrontSide, card.VersionCreator.UserName, card.VersionUtcDate, card.VersionDescription, !card.VisibleTo.Any() || card.VisibleTo.Any(u => u.UserId == subscription.UserId))));
            }
            while (searchResult.TotalNbCards > allCardsFromSearchHashSet.Count);

            var allCardsFromSearchHashSetIds = new HashSet<Guid>(allCardsFromSearchHashSet.Select(cardVersion => cardVersion.CardId));

            var cardsNotFoundAnymore = cardsInLastRun.Where(c => !allCardsFromSearchHashSetIds.Contains(c)).ToImmutableArray();
            var newFoundCards = allCardsFromSearchHashSet.Where(c => !cardsInLastRun.Contains(c.CardId)).ToImmutableArray();

            foreach (var cardNotFoundAnymore in cardsNotFoundAnymore)
            {
                var entity = await dbContext.CardsInSearchResults.SingleAsync(c => c.SearchSubscriptionId == searchSubscriptionId && c.CardId == cardNotFoundAnymore);
                dbContext.CardsInSearchResults.Remove(entity);
            }
            foreach (var addedCard in newFoundCards)
                dbContext.CardsInSearchResults.Add(new CardInSearchResult { SearchSubscriptionId = searchSubscriptionId, CardId = addedCard.CardId });

            var countOfCardsNotFoundAnymore_StillExists_UserAllowedToView = 0;
            var cardsNotFoundAnymore_StillExists_UserAllowedToView = new List<CardVersion>();
            var countOfCardsNotFoundAnymore_Deleted_UserAllowedToView = 0;
            var cardsNotFoundAnymore_Deleted_UserAllowedToView = new List<CardDeletion>();
            var countOfCardsNotFoundAnymore_StillExists_UserNotAllowedToView = 0;
            var countOfCardsNotFoundAnymore_Deleted_UserNotAllowedToView = 0;

            foreach (Guid notFoundId in cardsNotFoundAnymore)
            {
                var card = await dbContext.Cards
                    .Include(c => c.UsersWithView)
                    .Include(c => c.VersionCreator)
                    .Where(c => c.Id == notFoundId).SingleOrDefaultAsync();
                if (card != null)
                {
                    if (CardVisibilityHelper.CardIsVisibleToUser(request.UserId, card.UsersWithView))
                    {
                        countOfCardsNotFoundAnymore_StillExists_UserAllowedToView++;
                        cardsNotFoundAnymore_StillExists_UserAllowedToView.Add(new CardVersion(card.Id, card.FrontSide, card.VersionCreator.UserName, card.VersionUtcDate, card.VersionDescription, CardVisibilityHelper.CardIsVisibleToUser(request.UserId, card.UsersWithView)));
                    }
                    else
                        countOfCardsNotFoundAnymore_StillExists_UserNotAllowedToView++;
                }
                else
                {
                    var previousVersion = await dbContext.CardPreviousVersions
                        .Include(previousVersion => previousVersion.UsersWithView)
                        .Include(previousVersion => previousVersion.VersionCreator)
                        .Where(previousVersion => previousVersion.Card == notFoundId && previousVersion.VersionType == CardPreviousVersionType.Deletion)
                        .SingleOrDefaultAsync();
                    if (previousVersion == null)
                        //Strange! Has the card been purged from previous versions?
                        countOfCardsNotFoundAnymore_Deleted_UserNotAllowedToView++;
                    else
                    {
                        var userWithViewIds = previousVersion.UsersWithView.Select(uwv => uwv.AllowedUserId);
                        if (CardVisibilityHelper.CardIsVisibleToUser(request.UserId, userWithViewIds))
                        {
                            countOfCardsNotFoundAnymore_Deleted_UserAllowedToView++;
                            cardsNotFoundAnymore_Deleted_UserAllowedToView.Add(new CardDeletion(previousVersion.FrontSide, previousVersion.VersionCreator.UserName, previousVersion.VersionUtcDate, previousVersion.VersionDescription, CardVisibilityHelper.CardIsVisibleToUser(request.UserId, userWithViewIds)));
                        }
                        else
                            countOfCardsNotFoundAnymore_Deleted_UserNotAllowedToView++;
                    }
                }
            }

            subscription.LastRunUtcDate = runningUtcDate;
            await dbContext.SaveChangesAsync();
            return new UserSearchNotifierResult("SearchSubscriptionNameNotImplementedYet",
                newFoundCards.Length,
                newFoundCards.Take(maxCountToReport),
                countOfCardsNotFoundAnymore_StillExists_UserAllowedToView,
                cardsNotFoundAnymore_StillExists_UserAllowedToView,
                countOfCardsNotFoundAnymore_Deleted_UserAllowedToView,
                cardsNotFoundAnymore_Deleted_UserAllowedToView,
                countOfCardsNotFoundAnymore_StillExists_UserNotAllowedToView,
                countOfCardsNotFoundAnymore_Deleted_UserNotAllowedToView);
        }
    }
}
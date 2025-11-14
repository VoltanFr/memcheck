using MemCheck.Application.QueryValidation;
using MemCheck.Application.Searching;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Notifiying;

/// <summary> This notifier reports cards which appear or disappear in the given search, compared to the previous run.
/// Here is the complete list of possibilities of events which bring this situation if they have occurred since last execution:
/// - A card has been created
/// - A new card version has been created which makes the card match the search
/// - A card has been deleted.
/// All this cases are covered by the unit test class.
/// </summary>

internal interface IUserSearchNotifier
{
    Task<UserSearchNotifierResult> RunAsync(Guid searchSubscriptionId);
}
internal sealed class UserSearchNotifier : IUserSearchNotifier
{
    #region Fields
    private readonly CallContext callContext;
    private readonly DateTime runningUtcDate;
    private readonly int maxCountToReport;
    private readonly ICollection<string> performanceIndicators;
    #endregion
    #region private sealed record DetailsFromCardsNotFoundAnymore
    private sealed record DetailsFromCardsNotFoundAnymore(
         int CountOfCardsNotFoundAnymore_StillExists_UserAllowedToView,
         ImmutableArray<CardVersion> CardsNotFoundAnymore_StillExists_UserAllowedToView,
         int CountOfCardsNotFoundAnymore_Deleted_UserAllowedToView,
         ImmutableArray<CardDeletion> CardsNotFoundAnymore_Deleted_UserAllowedToView,
         int CountOfCardsNotFoundAnymore_StillExists_UserNotAllowedToView,
         int CountOfCardsNotFoundAnymore_Deleted_UserNotAllowedToView
    );
    #endregion
    #region Private methods
    private static SearchCards.Request GetRequest(SearchSubscription subscription)
    {
        var request = new SearchCards.Request
        {
            UserId = subscription.UserId,
            RequiredText = subscription.RequiredText,
            RequiredTags = subscription.RequiredTags.Select(t => t.TagId),
            PageSize = SearchCards.Request.MaxPageSize,
            PageNo = 0
        };

        request = subscription.ExcludeAllTags
            ? (request with { ExcludedTags = null })
            : (request with { ExcludedTags = subscription.ExcludedTags.Select(t => t.TagId) });

        if (subscription.ExcludedDeck != Guid.Empty)
            request = request with { Deck = subscription.ExcludedDeck, DeckIsInclusive = false };

        return request;
    }
    private async Task<SearchSubscription> GetSearchSubscriptionAsync(Guid searchSubscriptionId)
    {
        var chrono = Stopwatch.StartNew();
        var subscription = await callContext.DbContext.SearchSubscriptions
            .Include(s => s.ExcludedTags)
            .Include(s => s.RequiredTags)
            .Include(s => s.CardsInLastRun)
            .SingleAsync(s => s.Id == searchSubscriptionId);
        performanceIndicators.Add($"{GetType().Name} took {chrono.Elapsed} to get a search subscription");
        return subscription;
    }
    private ImmutableHashSet<Guid> GetIdsOfCardsInLastRun(SearchSubscription subscription)
    {
        var chrono = Stopwatch.StartNew();
        var result = subscription.CardsInLastRun.Select(c => c.CardId).ToImmutableHashSet();
        performanceIndicators.Add($"{GetType().Name} took {chrono.Elapsed} to load all {result.Count} card ids in last run");
        return result;
    }
    private async Task<ImmutableHashSet<CardVersion>> GetAllCardsFromSearchAsync(SearchSubscription subscription)
    {
        var chrono = Stopwatch.StartNew();
        var result = new HashSet<CardVersion>();
        var request = GetRequest(subscription);
        SearchCards.Result searchResult;
        do
        {
            request = request with { PageNo = request.PageNo + 1 };
            var searcher = new SearchCards(callContext);
            searchResult = await searcher.RunAsync(request);
            result.UnionWith(
                searchResult.Cards.Select(card => new CardVersion(
                    card.CardId,
                    card.FrontSide,
                    card.VersionCreator.GetUserName(),
                    card.VersionUtcDate,
                    card.VersionDescription,
                    CardVisibilityHelper.CardIsVisibleToUser(subscription.UserId, card),
                    null
                    )
                )
            );
        }
        while (searchResult.TotalNbCards > result.Count);
        performanceIndicators.Add($"{GetType().Name} took {chrono.Elapsed} to load all {result.Count} card versions from search (in {searchResult.PageCount} pages)");
        return result.ToImmutableHashSet();
    }
    private (ImmutableArray<Guid> cardsNotFoundAnymore, ImmutableArray<CardVersion> newFoundCards) FilterCardsFromSearchResult(ImmutableHashSet<CardVersion> allCardsFromSearchHashSet, ImmutableHashSet<Guid> cardsInLastRun)
    {
        var chrono = Stopwatch.StartNew();
        var allCardsFromSearchHashSetIds = new HashSet<Guid>(allCardsFromSearchHashSet.Select(cardVersion => cardVersion.CardId));
        var cardsNotFoundAnymore = cardsInLastRun.Where(c => !allCardsFromSearchHashSetIds.Contains(c)).ToImmutableArray();
        var newFoundCards = allCardsFromSearchHashSet.Where(c => !cardsInLastRun.Contains(c.CardId)).ToImmutableArray();
        performanceIndicators.Add($"{GetType().Name} took {chrono.Elapsed} to filter in memory data");
        return (cardsNotFoundAnymore, newFoundCards);
    }
    private async Task RemoveCardsNotFoundAnymoreFromResultsInDbAsync(ImmutableArray<Guid> cardsNotFoundAnymore, Guid searchSubscriptionId)
    {
        var chrono = Stopwatch.StartNew();
        foreach (var cardNotFoundAnymore in cardsNotFoundAnymore)
        {
            var entity = await callContext.DbContext.CardsInSearchResults.SingleAsync(c => c.SearchSubscriptionId == searchSubscriptionId && c.CardId == cardNotFoundAnymore);
            callContext.DbContext.CardsInSearchResults.Remove(entity);
        }
        performanceIndicators.Add($"{GetType().Name} took {chrono.Elapsed} to mark {cardsNotFoundAnymore.Length} search result cards for deletion in DB");
    }
    private async Task AddNewFoundCardToResultsInDbAsync(ImmutableArray<CardVersion> newFoundCards, Guid searchSubscriptionId)
    {
        var chrono = Stopwatch.StartNew();
        foreach (var addedCard in newFoundCards)
            await callContext.DbContext.CardsInSearchResults.AddAsync(new CardInSearchResult { SearchSubscriptionId = searchSubscriptionId, CardId = addedCard.CardId });
        performanceIndicators.Add($"{GetType().Name} took {chrono.Elapsed} to mark {newFoundCards.Length} search result cards for add to DB");
    }
    private async Task<DetailsFromCardsNotFoundAnymore> GetDetailsFromCardsNotFoundAnymoreAsync(ImmutableArray<Guid> cardsNotFoundAnymore, SearchSubscription subscription)
    {
        var countOfCardsNotFoundAnymore_StillExists_UserAllowedToView = 0;
        var cardsNotFoundAnymore_StillExists_UserAllowedToView = new List<CardVersion>();
        var countOfCardsNotFoundAnymore_Deleted_UserAllowedToView = 0;
        var cardsNotFoundAnymore_Deleted_UserAllowedToView = new List<CardDeletion>();
        var countOfCardsNotFoundAnymore_StillExists_UserNotAllowedToView = 0;
        var countOfCardsNotFoundAnymore_Deleted_UserNotAllowedToView = 0;

        var chrono = Stopwatch.StartNew();
        foreach (var notFoundId in cardsNotFoundAnymore)
        {
            var card = await callContext.DbContext.Cards
                .Include(c => c.UsersWithView)
                .Include(c => c.VersionCreator)
                .Where(c => c.Id == notFoundId)
                .SingleOrDefaultAsync();

            if (card != null)
            {
                if (CardVisibilityHelper.CardIsVisibleToUser(subscription.UserId, card))
                {
                    countOfCardsNotFoundAnymore_StillExists_UserAllowedToView++;
                    cardsNotFoundAnymore_StillExists_UserAllowedToView.Add(new CardVersion(card.Id, card.FrontSide, card.VersionCreator.GetUserName(), card.VersionUtcDate, card.VersionDescription, true, null));
                }
                else
                    countOfCardsNotFoundAnymore_StillExists_UserNotAllowedToView++;
            }
            else
            {
                var previousVersion = await callContext.DbContext.CardPreviousVersions
                    .Include(previousVersion => previousVersion.UsersWithView)
                    .Include(previousVersion => previousVersion.VersionCreator)
                    .Where(previousVersion => previousVersion.Card == notFoundId && previousVersion.VersionType == CardPreviousVersionType.Deletion)
                    .SingleOrDefaultAsync();
                if (previousVersion == null)
                    //Strange! Has the card been purged from previous versions?
                    countOfCardsNotFoundAnymore_Deleted_UserNotAllowedToView++;
                else
                {
                    if (CardVisibilityHelper.CardIsVisibleToUser(subscription.UserId, previousVersion))
                    {
                        countOfCardsNotFoundAnymore_Deleted_UserAllowedToView++;
                        cardsNotFoundAnymore_Deleted_UserAllowedToView.Add(new CardDeletion(previousVersion.FrontSide, previousVersion.VersionCreator.GetUserName(), previousVersion.VersionUtcDate, previousVersion.VersionDescription, true));
                    }
                    else
                        countOfCardsNotFoundAnymore_Deleted_UserNotAllowedToView++;
                }
            }
        }
        performanceIndicators.Add($"{GetType().Name} took {chrono.Elapsed} to work on cards not found anymore");
        return new DetailsFromCardsNotFoundAnymore(
            countOfCardsNotFoundAnymore_StillExists_UserAllowedToView,
            cardsNotFoundAnymore_StillExists_UserAllowedToView.ToImmutableArray(),
            countOfCardsNotFoundAnymore_Deleted_UserAllowedToView,
            cardsNotFoundAnymore_Deleted_UserAllowedToView.ToImmutableArray(),
            countOfCardsNotFoundAnymore_StillExists_UserNotAllowedToView,
            countOfCardsNotFoundAnymore_Deleted_UserNotAllowedToView
         );
    }
    #endregion
    public UserSearchNotifier(CallContext callContext, int maxCountToReport, ICollection<string> performanceIndicators)
    {
        //Prod constructor
        this.callContext = callContext;
        runningUtcDate = DateTime.UtcNow;
        this.maxCountToReport = maxCountToReport;
        this.performanceIndicators = performanceIndicators;
    }
    public UserSearchNotifier(CallContext callContext, int maxCountToReport, DateTime runningUtcDate)
    {
        //Unit tests constructor
        this.callContext = callContext;
        this.runningUtcDate = runningUtcDate;
        this.maxCountToReport = maxCountToReport;
        performanceIndicators = new List<string>();
    }
    public async Task<UserSearchNotifierResult> RunAsync(Guid searchSubscriptionId)
    {
        var subscription = await GetSearchSubscriptionAsync(searchSubscriptionId);
        var cardsInLastRun = GetIdsOfCardsInLastRun(subscription);
        var allCardsFromSearchHashSet = await GetAllCardsFromSearchAsync(subscription);
        (var cardsNotFoundAnymore, var newFoundCards) = FilterCardsFromSearchResult(allCardsFromSearchHashSet, cardsInLastRun);
        await RemoveCardsNotFoundAnymoreFromResultsInDbAsync(cardsNotFoundAnymore, searchSubscriptionId);
        await AddNewFoundCardToResultsInDbAsync(newFoundCards, searchSubscriptionId);
        var detailsFromCardsNotFoundAnymore = await GetDetailsFromCardsNotFoundAnymoreAsync(cardsNotFoundAnymore, subscription);
        subscription.LastRunUtcDate = runningUtcDate;
        await callContext.DbContext.SaveChangesAsync();

        var result = new UserSearchNotifierResult(subscription.Name,
            newFoundCards.Length,
            newFoundCards.Take(maxCountToReport),
            detailsFromCardsNotFoundAnymore.CountOfCardsNotFoundAnymore_StillExists_UserAllowedToView,
            detailsFromCardsNotFoundAnymore.CardsNotFoundAnymore_StillExists_UserAllowedToView,
            detailsFromCardsNotFoundAnymore.CountOfCardsNotFoundAnymore_Deleted_UserAllowedToView,
            detailsFromCardsNotFoundAnymore.CardsNotFoundAnymore_Deleted_UserAllowedToView,
            detailsFromCardsNotFoundAnymore.CountOfCardsNotFoundAnymore_StillExists_UserNotAllowedToView,
            detailsFromCardsNotFoundAnymore.CountOfCardsNotFoundAnymore_Deleted_UserNotAllowedToView);

        callContext.TelemetryClient.TrackEvent("UserSearchNotifier",
            ("searchSubscriptionId", searchSubscriptionId.ToString()),
            ("SubscriptionName", result.SubscriptionName.ToString()),
            ClassWithMetrics.IntMetric("TotalNewlyFoundCardCount", result.TotalNewlyFoundCardCount),
            ClassWithMetrics.IntMetric("NewlyFoundCardsCount", result.NewlyFoundCards.Length),
            ClassWithMetrics.IntMetric("CountOfCardsNotFoundAnymore_StillExists_UserAllowedToView", result.CountOfCardsNotFoundAnymoreStillExistsUserAllowedToView),
            ClassWithMetrics.IntMetric("CardsNotFoundAnymore_StillExists_UserAllowedToViewCount", result.CardsNotFoundAnymoreStillExistsUserAllowedToView.Length),
            ClassWithMetrics.IntMetric("CountOfCardsNotFoundAnymore_Deleted_UserAllowedToView", result.CountOfCardsNotFoundAnymoreDeletedUserAllowedToView),
            ClassWithMetrics.IntMetric("CardsNotFoundAnymore_Deleted_UserAllowedToViewCount", result.CardsNotFoundAnymoreDeletedUserAllowedToView.Length),
            ClassWithMetrics.IntMetric("CountOfCardsNotFoundAnymore_StillExists_UserNotAllowedToView", result.CountOfCardsNotFoundAnymoreStillExistsUserNotAllowedToView),
            ClassWithMetrics.IntMetric("CountOfCardsNotFoundAnymore_Deleted_UserNotAllowedToViewCount", result.CountOfCardsNotFoundAnymoreDeletedUserNotAllowedToView));

        return result;
    }
}

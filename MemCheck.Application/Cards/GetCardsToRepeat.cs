using MemCheck.Application.Heaping;
using MemCheck.Application.QueryValidation;
using MemCheck.Application.Tags;
using MemCheck.Basics;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards;

public sealed class GetCardsToRepeat : RequestRunner<GetCardsToRepeat.Request, GetCardsToRepeat.Result>
{
    #region Fields
    private readonly DateTime? now;
    #endregion
    #region Private methods
    private async Task<List<ResultCard>> RunAsync(Request request, HeapingAlgorithm heapingAlgorithm, ImmutableDictionary<Guid, string> userNames, ImmutableDictionary<Guid, string> tagNames, DateTime now)
    {
        var result = new List<ResultCard>();

        for (var heap = CardInDeck.MaxHeapValue; heap > 0 && result.Count < request.CardsToDownload; heap--)
        {
            var heapResults = await RunForHeapAsync(request, heapingAlgorithm, userNames, tagNames, now, heap, request.CardsToDownload - result.Count);
            result.AddRange(heapResults);
        }

        return result;
    }
    private async Task<IEnumerable<ResultCard>> RunForHeapAsync(Request request, HeapingAlgorithm heapingAlgorithm, ImmutableDictionary<Guid, string> userNames, ImmutableDictionary<Guid, string> tagNames, DateTime now, int heap, int maxCount)
    {
        var cardsOfHeap = DbContext.CardsInDecks.AsNoTracking()
            .Include(cardInDeck => cardInDeck.Card)
            .Where(cardInDeck => cardInDeck.DeckId == request.DeckId && cardInDeck.CurrentHeap == heap)
            .Where(cardInDeck => !request.ExcludedCardIds.Contains(cardInDeck.CardId))
            .Where(cardInDeck => cardInDeck.ExpiryUtcTime <= now)
            .OrderBy(cardInDeck => cardInDeck.ExpiryUtcTime)
            .Take(maxCount)
            .AsSingleQuery()
            .Select(cardInDeck => new
            {
                cardInDeck.CardId,
                cardInDeck.CurrentHeap,
                cardInDeck.LastLearnUtcTime,
                cardInDeck.AddToDeckUtcTime,
                cardInDeck.BiggestHeapReached,
                cardInDeck.NbTimesInNotLearnedHeap,
                cardInDeck.Card.FrontSide,
                cardInDeck.Card.BackSide,
                cardInDeck.Card.AdditionalInfo,
                cardInDeck.Card.References,
                cardInDeck.Card.VersionUtcDate,
                VersionCreator = cardInDeck.Card.VersionCreator.Id,
                tagIds = cardInDeck.Card.TagsInCards.Select(tag => tag.TagId),
                userWithViewIds = cardInDeck.Card.UsersWithView.Select(u => u.UserId),
                cardInDeck.Card.AverageRating,
                cardInDeck.Card.RatingCount,
                LanguageName = cardInDeck.Card.CardLanguage.Name,
                cardInDeck.Card.LatestDiscussionEntry

            }).ToImmutableArray();

        var notifications = new CardRegistrationsLoader(DbContext).RunForCardIds(request.CurrentUserId, cardsOfHeap.Select(c => c.CardId));

        //The following line could be improved with a joint. Not sure this would perform better, to be checked
        var userRatings = await DbContext.UserCardRatings.Where(r => r.UserId == request.CurrentUserId).Select(r => new { r.CardId, r.Rating }).ToDictionaryAsync(r => r.CardId, r => r.Rating);

        var thisHeapResult = cardsOfHeap.Select(oldestCard => new ResultCard(oldestCard.CardId, oldestCard.CurrentHeap, oldestCard.LastLearnUtcTime, oldestCard.AddToDeckUtcTime,
            oldestCard.BiggestHeapReached, oldestCard.NbTimesInNotLearnedHeap,
            oldestCard.FrontSide, oldestCard.BackSide, oldestCard.AdditionalInfo, oldestCard.References,
            oldestCard.VersionUtcDate,
            userNames[oldestCard.VersionCreator],
            oldestCard.tagIds.Select(tagId => tagNames[tagId]),
            oldestCard.userWithViewIds.Select(userWithView => userNames[userWithView]),
            userRatings.TryGetValue(oldestCard.CardId, out var value) ? value : 0,
            oldestCard.AverageRating,
            oldestCard.RatingCount,
            notifications[oldestCard.CardId],
            oldestCard.LanguageName == "Français", //Questionable hardcoding
            GetMoveToHeapExpiryInfos(heapingAlgorithm, oldestCard.LastLearnUtcTime),
            oldestCard.LatestDiscussionEntry?.CreationUtcDate
            )
        ).OrderBy(r => r.LastLearnUtcTime);

        return thisHeapResult;
    }
    private static ImmutableArray<MoveToHeapExpiryInfo> GetMoveToHeapExpiryInfos(HeapingAlgorithm heapingAlgorithm, DateTime lastLearnUtcTime)
    {
        return Enumerable.Range(0, CardInDeck.MaxHeapValue)
            .Select(targetHeapForMove => new MoveToHeapExpiryInfo(targetHeapForMove, targetHeapForMove == 0 ? CardInDeck.NeverLearntLastLearnTime : heapingAlgorithm.ExpiryUtcDate(targetHeapForMove, lastLearnUtcTime)))
            .ToImmutableArray();
    }
    #endregion
    public GetCardsToRepeat(CallContext callContext, DateTime? now = null) : base(callContext)
    {
        this.now = now;
    }
    protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
    {
        var heapingAlgorithm = await HeapingAlgorithm.OfDeckAsync(DbContext, request.DeckId);
        var userNames = DbContext.Users.AsNoTracking().Select(u => new { u.Id, UserName = u.GetUserName() }).ToImmutableDictionary(u => u.Id, u => u.UserName);
        var tagNames = TagLoadingHelper.Run(DbContext);

        var result = await RunAsync(request, heapingAlgorithm, userNames, tagNames, now == null ? DateTime.UtcNow : now.Value);

        return new ResultWithMetrologyProperties<Result>(new Result(result),
            ("DeckId", request.DeckId.ToString()),
            IntMetric("ExcludedCardCount", request.ExcludedCardIds.Count()),
            IntMetric("RequestedCardCount", request.CardsToDownload),
            IntMetric("ResultCount", result.Count));
    }
    #region Request and Result
    public sealed class Request : IRequest
    {
        public Request(Guid currentUserId, Guid deckId, IEnumerable<Guid> excludedCardIds, int cardsToDownload)
        {
            CurrentUserId = currentUserId;
            DeckId = deckId;
            ExcludedCardIds = excludedCardIds;
            CardsToDownload = cardsToDownload;
        }
        public Guid CurrentUserId { get; }
        public Guid DeckId { get; }
        public IEnumerable<Guid> ExcludedCardIds { get; }
        public int CardsToDownload { get; }
        public async Task CheckValidityAsync(CallContext callContext)
        {
            QueryValidationHelper.CheckNotReservedGuid(CurrentUserId);
            QueryValidationHelper.CheckNotReservedGuid(DeckId);
            QueryValidationHelper.CheckContainsNoReservedGuid(ExcludedCardIds);
            if (CardsToDownload is < 1 or > 100)
                throw new RequestInputException($"Invalid CardsToDownload: {CardsToDownload}");
            await QueryValidationHelper.CheckUserIsOwnerOfDeckAsync(callContext.DbContext, CurrentUserId, DeckId);
        }
    }
    public sealed record Result(IEnumerable<ResultCard> Cards);
    public sealed class ResultCard
    {
        public ResultCard(Guid cardId, int heap, DateTime lastLearnUtcTime, DateTime addToDeckUtcTime, int biggestHeapReached, int nbTimesInNotLearnedHeap,
            string frontSide, string backSide, string additionalInfo, string references, DateTime lastChangeUtcTime, string owner, IEnumerable<string> tags, IEnumerable<string> visibleTo,
            int userRating, double averageRating, int countOfUserRatings,
            bool registeredForNotifications, bool isInFrench, ImmutableArray<MoveToHeapExpiryInfo> moveToHeapExpiryInfos, DateTime? latestDiscussionEntryCreationUtcDate)
        {
            DateServices.CheckUTC(lastLearnUtcTime);
            CardId = cardId;
            Heap = heap;
            LastLearnUtcTime = lastLearnUtcTime;
            LastChangeUtcTime = lastChangeUtcTime;
            AddToDeckUtcTime = addToDeckUtcTime;
            BiggestHeapReached = biggestHeapReached;
            NbTimesInNotLearnedHeap = nbTimesInNotLearnedHeap;
            Owner = owner;
            FrontSide = frontSide;
            BackSide = backSide;
            AdditionalInfo = additionalInfo;
            References = references;
            Tags = tags;
            VisibleTo = visibleTo;
            UserRating = userRating;
            AverageRating = averageRating;
            CountOfUserRatings = countOfUserRatings;
            RegisteredForNotifications = registeredForNotifications;
            IsInFrench = isInFrench;
            MoveToHeapExpiryInfos = moveToHeapExpiryInfos;
            LatestDiscussionEntryCreationUtcDate = latestDiscussionEntryCreationUtcDate;
        }
        public Guid CardId { get; }
        public int Heap { get; }
        public DateTime LastLearnUtcTime { get; }
        public DateTime LastChangeUtcTime { get; }
        public DateTime AddToDeckUtcTime { get; }
        public int BiggestHeapReached { get; }
        public int NbTimesInNotLearnedHeap { get; }
        public string FrontSide { get; }
        public string BackSide { get; }
        public string AdditionalInfo { get; }
        public string References { get; }
        public string Owner { get; }
        public int UserRating { get; }
        public double AverageRating { get; }
        public int CountOfUserRatings { get; }
        public bool RegisteredForNotifications { get; }
        public bool IsInFrench { get; }
        public DateTime? LatestDiscussionEntryCreationUtcDate { get; }
        public IEnumerable<string> Tags { get; }
        public IEnumerable<string> VisibleTo { get; }
        public ImmutableArray<MoveToHeapExpiryInfo> MoveToHeapExpiryInfos { get; }
    }
    public sealed record MoveToHeapExpiryInfo(int HeapId, DateTime UtcExpiryDate);
    #endregion
}

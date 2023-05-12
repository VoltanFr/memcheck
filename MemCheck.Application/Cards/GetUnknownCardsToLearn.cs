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

/* Returns a set of unknown cards
 * For cards which have already been learned, the result is sorted by LastLearnUtcTime.
 * For cards which have never been learned, we take a set of 3 times the number of requested cards, sorted by date of adding to the deck, then we shuffle this set.
 *  This choice might look a bit complicated, but this is because we used to get a warning from Entity Framework because we use `Take` without ordering, which may lead to unpredictable results.
 *  And I think it makes sense to begin with learning the cards which have been added the longest time ago (this case happens only when at least 10 cards are in the unknown state at the same time).
 */
public sealed class GetUnknownCardsToLearn : RequestRunner<GetUnknownCardsToLearn.Request, GetUnknownCardsToLearn.Result>
{
    #region Fields
    private readonly DateTime runDate;
    #endregion
    #region Private methods
    private async Task<IEnumerable<ResultCard>> GetUnknownCardsAsync(Guid userId, Guid deckId, IEnumerable<Guid> excludedCardIds, HeapingAlgorithm heapingAlgorithm, ImmutableDictionary<Guid, string> userNames, ImmutableDictionary<Guid, string> tagNames, int cardCount, bool neverLearnt)
    {
        var cardsOfDeck = DbContext.CardsInDecks.AsNoTracking()
            .Include(card => card.Card).AsSingleQuery()
            .Where(card => card.DeckId.Equals(deckId) && card.CurrentHeap == 0 && !excludedCardIds.Contains(card.CardId));

        var countToTake = neverLearnt ? cardCount * 3 : cardCount; //For cards never learnt, we take more cards for shuffling accross more

        var finalSelection = neverLearnt
            ? cardsOfDeck.Where(cardInDeck => cardInDeck.LastLearnUtcTime == CardInDeck.NeverLearntLastLearnTime).OrderBy(cardInDeck => cardInDeck.AddToDeckUtcTime).Take(countToTake)
            : cardsOfDeck.Where(cardInDeck => cardInDeck.LastLearnUtcTime != CardInDeck.NeverLearntLastLearnTime).OrderBy(cardInDeck => cardInDeck.LastLearnUtcTime).Take(countToTake);

        var withDetails = finalSelection.Select(cardInDeck => new
        {
            cardInDeck.CardId,
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
            LatestDiscussionEntryCreationUtcDate = cardInDeck.Card.LatestDiscussionEntry
        });

        var listed = await withDetails.ToListAsync();
        var cardIds = listed.Select(cardInDeck => cardInDeck.CardId);
        var notifications = new CardRegistrationsLoader(DbContext).RunForCardIds(userId, cardIds);

        //The following line could be improved with a joint. Not sure this would perform better, to be checked
        var userRatings = await DbContext.UserCardRatings.Where(r => r.UserId == userId).Select(r => new { r.CardId, r.Rating }).ToDictionaryAsync(r => r.CardId, r => r.Rating);

        var result = listed.Select(cardInDeck => new ResultCard(
            cardInDeck.CardId,
            cardInDeck.LastLearnUtcTime,
            cardInDeck.AddToDeckUtcTime,
            cardInDeck.BiggestHeapReached, cardInDeck.NbTimesInNotLearnedHeap,
            cardInDeck.FrontSide,
            cardInDeck.BackSide,
            cardInDeck.AdditionalInfo,
            cardInDeck.References,
            cardInDeck.VersionUtcDate,
            userNames[cardInDeck.VersionCreator],
            cardInDeck.tagIds.Select(tagId => tagNames[tagId]),
            cardInDeck.userWithViewIds.Select(userWithView => userNames[userWithView]),
            userRatings.TryGetValue(cardInDeck.CardId, out var value) ? value : 0,
            cardInDeck.AverageRating,
            cardInDeck.RatingCount,
            notifications[cardInDeck.CardId],
            cardInDeck.LanguageName == "Français", //Questionable hardcoding
            GetMoveToHeapExpiryInfos(heapingAlgorithm, cardInDeck.LastLearnUtcTime),
            cardInDeck.LatestDiscussionEntryCreationUtcDate?.CreationUtcDate
        ));
        return neverLearnt ? Shuffler.Shuffle(result).Take(cardCount) : result;
    }
    private ImmutableArray<MoveToHeapExpiryInfo> GetMoveToHeapExpiryInfos(HeapingAlgorithm heapingAlgorithm, DateTime lastLearnUtcTime)
    {
        return Enumerable.Range(1, CardInDeck.MaxHeapValue)
            .Select(targetHeapForMove => new MoveToHeapExpiryInfo(targetHeapForMove, lastLearnUtcTime == CardInDeck.NeverLearntLastLearnTime ? runDate : heapingAlgorithm.ExpiryUtcDate(targetHeapForMove, lastLearnUtcTime)))
            .ToImmutableArray();
    }
    #endregion
    public GetUnknownCardsToLearn(CallContext callContext, DateTime? runDate = null) : base(callContext)
    {
        this.runDate = runDate ?? DateTime.UtcNow;
    }
    protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
    {
        var heapingAlgorithm = await HeapingAlgorithm.OfDeckAsync(DbContext, request.DeckId);
        var userNames = DbContext.Users.AsNoTracking().Select(u => new { u.Id, UserName = u.GetUserName() }).ToImmutableDictionary(u => u.Id, u => u.UserName);
        var tagNames = TagLoadingHelper.Run(DbContext);

        var result = new List<ResultCard>();
        result.AddRange(await GetUnknownCardsAsync(request.CurrentUserId, request.DeckId, request.ExcludedCardIds, heapingAlgorithm, userNames, tagNames, request.CardsToDownload, true));
        result.AddRange(await GetUnknownCardsAsync(request.CurrentUserId, request.DeckId, request.ExcludedCardIds, heapingAlgorithm, userNames, tagNames, request.CardsToDownload - result.Count, false));

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
            if (QueryValidationHelper.IsReservedGuid(CurrentUserId))
                throw new RequestInputException($"Invalid user id '{CurrentUserId}'");
            if (QueryValidationHelper.IsReservedGuid(DeckId))
                throw new RequestInputException($"Invalid deck id '{DeckId}'");
            if (ExcludedCardIds.Any(cardId => QueryValidationHelper.IsReservedGuid(cardId)))
                throw new RequestInputException($"Invalid card id");
            if (CardsToDownload is < 1 or > 100)
                throw new RequestInputException($"Invalid CardsToDownload: {CardsToDownload}");
            await QueryValidationHelper.CheckUserIsOwnerOfDeckAsync(callContext.DbContext, CurrentUserId, DeckId);
        }
    }
    public sealed record Result(IEnumerable<ResultCard> Cards);
    public sealed class ResultCard
    {
        public ResultCard(Guid cardId, DateTime lastLearnUtcTime, DateTime addToDeckUtcTime, int biggestHeapReached, int nbTimesInNotLearnedHeap,
            string frontSide, string backSide, string additionalInfo, string references, DateTime lastChangeUtcTime, string owner, IEnumerable<string> tags, IEnumerable<string> visibleTo,
            int userRating, double averageRating, int countOfUserRatings,
            bool registeredForNotifications, bool isInFrench, ImmutableArray<MoveToHeapExpiryInfo> moveToHeapExpiryInfos, DateTime? latestDiscussionEntryCreationUtcDate)
        {
            DateServices.CheckUTC(lastLearnUtcTime);
            CardId = cardId;
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

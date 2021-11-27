using MemCheck.Application.Heaping;
using MemCheck.Application.Images;
using MemCheck.Application.QueryValidation;
using MemCheck.Application.Tags;
using MemCheck.Basics;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards
{
    public sealed class GetCardsToRepeat : RequestRunner<GetCardsToRepeat.Request, GetCardsToRepeat.Result>
    {
        #region Fields
        private readonly DateTime? now;
        #endregion
        #region Private methods
        private async Task<List<ResultCard>> RunAsync(Request request, HeapingAlgorithm heapingAlgorithm, ImmutableDictionary<Guid, string> userNames, ImmutableDictionary<Guid, string> imageNames, ImmutableDictionary<Guid, string> tagNames, DateTime now)
        {
            var result = new List<ResultCard>();

            for (var heap = CardInDeck.MaxHeapValue; heap > 0 && result.Count < request.CardsToDownload; heap--)
            {
                var heapResults = await RunForHeapAsync(request, heapingAlgorithm, userNames, imageNames, tagNames, now, heap, request.CardsToDownload - result.Count);
                result.AddRange(heapResults);
            }

            return result;
        }
        private async Task<IEnumerable<ResultCard>> RunForHeapAsync(Request request, HeapingAlgorithm heapingAlgorithm, ImmutableDictionary<Guid, string> userNames, ImmutableDictionary<Guid, string> imageNames, ImmutableDictionary<Guid, string> tagNames, DateTime now, int heap, int maxCount)
        {
            var cardsOfHeap = DbContext.CardsInDecks.AsNoTracking()
                .Include(cardInDeck => cardInDeck.Card)
                .Where(cardInDeck => cardInDeck.DeckId == request.DeckId && cardInDeck.CurrentHeap == heap)
                .Where(cardInDeck => !request.ExcludedCardIds.Contains(cardInDeck.CardId))
                .Where(cardInDeck => !cardInDeck.Card.TagsInCards.Any(tag => request.ExcludedTagIds.Contains(tag.TagId)))
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
                    cardInDeck.Card.VersionUtcDate,
                    VersionCreator = cardInDeck.Card.VersionCreator.Id,
                    tagIds = cardInDeck.Card.TagsInCards.Select(tag => tag.TagId),
                    userWithViewIds = cardInDeck.Card.UsersWithView.Select(u => u.UserId),
                    imageIdAndCardSides = cardInDeck.Card.Images.Select(img => new { img.ImageId, img.CardSide }),
                    cardInDeck.Card.AverageRating,
                    cardInDeck.Card.RatingCount
                }).ToImmutableArray();

            var notifications = new CardRegistrationsLoader(DbContext).RunForCardIds(request.CurrentUserId, cardsOfHeap.Select(c => c.CardId));

            //The following line could be improved with a joint. Not sure this would perform better, to be checked
            var userRatings = await DbContext.UserCardRatings.Where(r => r.UserId == request.CurrentUserId).Select(r => new { r.CardId, r.Rating }).ToDictionaryAsync(r => r.CardId, r => r.Rating);

            var thisHeapResult = cardsOfHeap.Select(oldestCard => new ResultCard(oldestCard.CardId, oldestCard.CurrentHeap, oldestCard.LastLearnUtcTime, oldestCard.AddToDeckUtcTime,
                oldestCard.BiggestHeapReached, oldestCard.NbTimesInNotLearnedHeap,
                oldestCard.FrontSide, oldestCard.BackSide, oldestCard.AdditionalInfo,
                oldestCard.VersionUtcDate,
                userNames[oldestCard.VersionCreator],
                oldestCard.tagIds.Select(tagId => tagNames[tagId]),
                oldestCard.userWithViewIds.Select(userWithView => userNames[userWithView]),
                oldestCard.imageIdAndCardSides.Select(imageIdAndCardSide => new ResultImageModel(imageIdAndCardSide.ImageId, imageNames[imageIdAndCardSide.ImageId], imageIdAndCardSide.CardSide)),
                heapingAlgorithm,
                userRatings.ContainsKey(oldestCard.CardId) ? userRatings[oldestCard.CardId] : 0,
                oldestCard.AverageRating,
                oldestCard.RatingCount,
                notifications[oldestCard.CardId]
                )
            ).OrderBy(r => r.LastLearnUtcTime);

            return thisHeapResult;
        }
        #endregion
        public GetCardsToRepeat(CallContext callContext, DateTime? now = null) : base(callContext)
        {
            this.now = now;
        }
        protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
        {
            var heapingAlgorithm = await HeapingAlgorithm.OfDeckAsync(DbContext, request.DeckId);
            var userNames = DbContext.Users.AsNoTracking().Select(u => new { u.Id, u.UserName }).ToImmutableDictionary(u => u.Id, u => u.UserName);
            var imageNames = ImageLoadingHelper.GetAllImageNames(DbContext);
            var tagNames = TagLoadingHelper.Run(DbContext);

            var result = await RunAsync(request, heapingAlgorithm, userNames, imageNames, tagNames, now == null ? DateTime.UtcNow : now.Value);

            return new ResultWithMetrologyProperties<Result>(new Result(result),
                ("DeckId", request.DeckId.ToString()),
                ("ExcludedCardCount", request.ExcludedCardIds.Count().ToString()),
                ("ExcludedTagCount", request.ExcludedTagIds.Count().ToString()),
                ("RequestedCardCount", request.CardsToDownload.ToString()),
                ("ResultCount", result.Count.ToString()));
        }
        #region Request and Result
        public sealed class Request : IRequest
        {
            public Request(Guid currentUserId, Guid deckId, IEnumerable<Guid> excludedCardIds, IEnumerable<Guid> excludedTagIds, int cardsToDownload)
            {
                CurrentUserId = currentUserId;
                DeckId = deckId;
                ExcludedCardIds = excludedCardIds;
                ExcludedTagIds = excludedTagIds;
                CardsToDownload = cardsToDownload;
            }
            public Guid CurrentUserId { get; }
            public Guid DeckId { get; }
            public IEnumerable<Guid> ExcludedCardIds { get; }
            public IEnumerable<Guid> ExcludedTagIds { get; }
            public int CardsToDownload { get; }
            public async Task CheckValidityAsync(CallContext callContext)
            {
                QueryValidationHelper.CheckNotReservedGuid(CurrentUserId);
                QueryValidationHelper.CheckNotReservedGuid(DeckId);
                QueryValidationHelper.CheckContainsNoReservedGuid(ExcludedCardIds);
                QueryValidationHelper.CheckContainsNoReservedGuid(ExcludedTagIds);
                if (CardsToDownload < 1 || CardsToDownload > 100)
                    throw new RequestInputException($"Invalid CardsToDownload: {CardsToDownload}");
                await QueryValidationHelper.CheckUserIsOwnerOfDeckAsync(callContext.DbContext, CurrentUserId, DeckId);
            }
        }
        public sealed record Result(IEnumerable<ResultCard> Cards);
        public sealed class ResultCard
        {
            public ResultCard(Guid cardId, int heap, DateTime lastLearnUtcTime, DateTime addToDeckUtcTime, int biggestHeapReached, int nbTimesInNotLearnedHeap,
                string frontSide, string backSide, string additionalInfo, DateTime lastChangeUtcTime, string owner, IEnumerable<string> tags, IEnumerable<string> visibleTo,
                IEnumerable<ResultImageModel> images, HeapingAlgorithm heapingAlgorithm, int userRating, double averageRating, int countOfUserRatings, bool registeredForNotifications)
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
                Tags = tags;
                VisibleTo = visibleTo;
                Images = images;
                UserRating = userRating;
                AverageRating = averageRating;
                CountOfUserRatings = countOfUserRatings;
                RegisteredForNotifications = registeredForNotifications;
                MoveToHeapExpiryInfos = Enumerable.Range(1, CardInDeck.MaxHeapValue)
                    .Where(heapId => heapId != heap)
                    .Select(targetHeapForMove => new MoveToHeapExpiryInfo(targetHeapForMove, heapingAlgorithm.ExpiryUtcDate(targetHeapForMove, lastLearnUtcTime)))
                    .Concat(new MoveToHeapExpiryInfo(0, CardInDeck.NeverLearntLastLearnTime).AsArray());
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
            public string Owner { get; }
            public int UserRating { get; }
            public double AverageRating { get; }
            public int CountOfUserRatings { get; }
            public bool RegisteredForNotifications { get; }
            public IEnumerable<string> Tags { get; }
            public IEnumerable<string> VisibleTo { get; }
            public IEnumerable<ResultImageModel> Images { get; }
            public IEnumerable<MoveToHeapExpiryInfo> MoveToHeapExpiryInfos { get; }
        }
        public sealed class ResultImageModel
        {
            public ResultImageModel(Guid id, string name, int cardSide)
            {
                ImageId = id;
                Name = name;
                CardSide = cardSide;
            }
            public Guid ImageId { get; }
            public string Name { get; }
            public int CardSide { get; set; }   //1 = front side ; 2 = back side ; 3 = AdditionalInfo
        }
        public sealed class MoveToHeapExpiryInfo
        {
            public MoveToHeapExpiryInfo(int heapId, DateTime utcExpiryDate)
            {
                HeapId = heapId;
                UtcExpiryDate = utcExpiryDate;
            }
            public int HeapId { get; }
            public DateTime UtcExpiryDate { get; }
        }
        #endregion
    }
}

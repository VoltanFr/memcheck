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
    public sealed class GetUnknownCardsToLearn
    {
        #region Fields
        private readonly CallContext callContext;
        #endregion
        #region Private methods
        private async Task<IEnumerable<ResultCard>> GetUnknownCardsAsync(Guid userId, Guid deckId, IEnumerable<Guid> excludedCardIds, IEnumerable<Guid> excludedTagIds, HeapingAlgorithm heapingAlgorithm, ImmutableDictionary<Guid, string> userNames, ImmutableDictionary<Guid, string> imageNames, ImmutableDictionary<Guid, string> tagNames, int cardCount, bool neverLearnt)
        {
            var cardsOfDeck = callContext.DbContext.CardsInDecks.AsNoTracking()
                .Include(card => card.Card).AsSingleQuery()
                .Where(card => card.DeckId.Equals(deckId) && card.CurrentHeap == 0 && !excludedCardIds.Contains(card.CardId));

            var withoutExcludedCards = cardsOfDeck;
            foreach (var tag in excludedTagIds)   //I tried to do better with an intersect between the two sets, but that failed
                withoutExcludedCards = withoutExcludedCards.Where(cardInDeck => !cardInDeck.Card.TagsInCards.Where(tagInCard => tagInCard.TagId == tag).Any());

            var countToTake = neverLearnt ? cardCount * 3 : cardCount; //For cards never learnt, we take more cards for shuffling accross more

            IQueryable<CardInDeck>? finalSelection;
            if (neverLearnt)
                finalSelection = withoutExcludedCards.Where(cardInDeck => cardInDeck.LastLearnUtcTime == CardInDeck.NeverLearntLastLearnTime).Take(countToTake);
            else
                finalSelection = withoutExcludedCards.Where(cardInDeck => cardInDeck.LastLearnUtcTime != CardInDeck.NeverLearntLastLearnTime).OrderBy(cardInDeck => cardInDeck.LastLearnUtcTime).Take(countToTake);

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
                cardInDeck.Card.VersionUtcDate,
                VersionCreator = cardInDeck.Card.VersionCreator.Id,
                tagIds = cardInDeck.Card.TagsInCards.Select(tag => tag.TagId),
                userWithViewIds = cardInDeck.Card.UsersWithView.Select(u => u.UserId),
                imageIdAndCardSides = cardInDeck.Card.Images.Select(img => new { img.ImageId, img.CardSide }),
                cardInDeck.Card.AverageRating,
                cardInDeck.Card.RatingCount
            });

            var listed = await withDetails.ToListAsync();
            var cardIds = listed.Select(cardInDeck => cardInDeck.CardId);
            var notifications = new CardRegistrationsLoader(callContext.DbContext).RunForCardIds(userId, cardIds);

            //The following line could be improved with a joint. Not sure this would perform better, to be checked
            var userRatings = await callContext.DbContext.UserCardRatings.Where(r => r.UserId == userId).Select(r => new { r.CardId, r.Rating }).ToDictionaryAsync(r => r.CardId, r => r.Rating);

            var result = listed.Select(cardInDeck => new ResultCard(
                cardInDeck.CardId,
                cardInDeck.LastLearnUtcTime,
                cardInDeck.AddToDeckUtcTime,
                cardInDeck.BiggestHeapReached, cardInDeck.NbTimesInNotLearnedHeap,
                cardInDeck.FrontSide,
                cardInDeck.BackSide,
                cardInDeck.AdditionalInfo,
                cardInDeck.VersionUtcDate,
                userNames[cardInDeck.VersionCreator],
                cardInDeck.tagIds.Select(tagId => tagNames[tagId]),
                cardInDeck.userWithViewIds.Select(userWithView => userNames[userWithView]),
                cardInDeck.imageIdAndCardSides.Select(imageIdAndCardSide => new ResultImageModel(imageIdAndCardSide.ImageId, imageNames[imageIdAndCardSide.ImageId], imageIdAndCardSide.CardSide)),
                heapingAlgorithm,
                userRatings.ContainsKey(cardInDeck.CardId) ? userRatings[cardInDeck.CardId] : 0,
                cardInDeck.AverageRating,
                cardInDeck.RatingCount,
                notifications[cardInDeck.CardId]
            ));
            if (neverLearnt)
                return Shuffler.Shuffle(result).Take(cardCount);
            else
                return result;
        }
        #endregion
        public GetUnknownCardsToLearn(CallContext callContext)
        {
            this.callContext = callContext;
        }
        public async Task<IEnumerable<ResultCard>> RunAsync(Request request)
        {
            request.CheckValidity(callContext.DbContext);

            var heapingAlgorithm = await HeapingAlgorithm.OfDeckAsync(callContext.DbContext, request.DeckId);
            var userNames = callContext.DbContext.Users.AsNoTracking().Select(u => new { u.Id, u.UserName }).ToImmutableDictionary(u => u.Id, u => u.UserName);
            var imageNames = ImageLoadingHelper.GetAllImageNames(callContext.DbContext);
            var tagNames = TagLoadingHelper.Run(callContext.DbContext);

            var result = new List<ResultCard>();
            result.AddRange(await GetUnknownCardsAsync(request.CurrentUserId, request.DeckId, request.ExcludedCardIds, request.ExcludedTagIds, heapingAlgorithm, userNames, imageNames, tagNames, request.CardsToDownload, true));
            result.AddRange(await GetUnknownCardsAsync(request.CurrentUserId, request.DeckId, request.ExcludedCardIds, request.ExcludedTagIds, heapingAlgorithm, userNames, imageNames, tagNames, request.CardsToDownload - result.Count, false));

            callContext.TelemetryClient.TrackEvent("GetUnknownCardsToLearn",
                ("DeckId", request.DeckId.ToString()),
                ("ExcludedCardCount", request.ExcludedCardIds.Count().ToString()),
                ("ExcludedTagCount", request.ExcludedTagIds.Count().ToString()),
                ("RequestedCardCount", request.CardsToDownload.ToString()),
                ("ResultCount", result.Count.ToString()));

            return result;
        }
        #region Request and result classes
        public sealed class Request
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
            public void CheckValidity(MemCheckDbContext dbContext)
            {
                if (QueryValidationHelper.IsReservedGuid(CurrentUserId))
                    throw new RequestInputException($"Invalid user id '{CurrentUserId}'");
                if (QueryValidationHelper.IsReservedGuid(DeckId))
                    throw new RequestInputException($"Invalid deck id '{DeckId}'");
                if (ExcludedCardIds.Any(cardId => QueryValidationHelper.IsReservedGuid(cardId)))
                    throw new RequestInputException($"Invalid card id");
                if (ExcludedTagIds.Any(cardId => QueryValidationHelper.IsReservedGuid(cardId)))
                    throw new RequestInputException($"Invalid tag id");
                if (CardsToDownload < 1 || CardsToDownload > 100)
                    throw new RequestInputException($"Invalid CardsToDownload: {CardsToDownload}");
                QueryValidationHelper.CheckUserIsOwnerOfDeck(dbContext, CurrentUserId, DeckId);
            }
        }
        public sealed class ResultCard
        {
            public ResultCard(Guid cardId, DateTime lastLearnUtcTime, DateTime addToDeckUtcTime, int biggestHeapReached, int nbTimesInNotLearnedHeap,
                string frontSide, string backSide, string additionalInfo, DateTime lastChangeUtcTime, string owner, IEnumerable<string> tags, IEnumerable<string> visibleTo,
                IEnumerable<ResultImageModel> images, HeapingAlgorithm heapingAlgorithm, int userRating, double averageRating, int countOfUserRatings, bool registeredForNotifications)
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
                Tags = tags;
                VisibleTo = visibleTo;
                Images = images;
                UserRating = userRating;
                AverageRating = averageRating;
                CountOfUserRatings = countOfUserRatings;
                RegisteredForNotifications = registeredForNotifications;
                MoveToHeapExpiryInfos = Enumerable.Range(1, CardInDeck.MaxHeapValue)
                    .Select(targetHeapForMove => new MoveToHeapExpiryInfo(targetHeapForMove, heapingAlgorithm.ExpiryUtcDate(targetHeapForMove, lastLearnUtcTime)))
                    .Concat(new MoveToHeapExpiryInfo(0, CardInDeck.NeverLearntLastLearnTime).AsArray());
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
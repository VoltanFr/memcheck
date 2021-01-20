using MemCheck.Application.Heaping;
using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Loading
{
    public sealed class GetUnknownCardsToLearn
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        #region Private methods
        private ImmutableDictionary<Guid, bool> GetNotifications(Guid userId, ImmutableHashSet<Guid> cardIds)
        {
            var notifs = dbContext.CardNotifications.Where(notif => notif.UserId == userId && cardIds.Contains(notif.CardId)).Select(notif => notif.CardId).ToImmutableHashSet();
            return cardIds.Select(cardId => new KeyValuePair<Guid, bool>(cardId, notifs.Contains(cardId))).ToImmutableDictionary();
        }
        private async Task<HeapingAlgorithm> GetHeapingAlgorithmAsync(Guid deckId)
        {
            var heapingAlgorithmId = await dbContext.Decks.Where(deck => deck.Id == deckId).Select(deck => deck.HeapingAlgorithmId).SingleAsync();
            var heapingAlgorithm = HeapingAlgorithms.Instance.FromId(heapingAlgorithmId);
            return heapingAlgorithm;

        }
        private async Task<IEnumerable<ResultCard>> GetUnknownCardsAsync(Guid userId, Guid deckId, IEnumerable<Guid> excludedCardIds, IEnumerable<Guid> excludedTagIds, HeapingAlgorithm heapingAlgorithm, ImmutableDictionary<Guid, string> userNames, ImmutableDictionary<Guid, ImageDetails> imagesDetails, ImmutableDictionary<Guid, string> tagNames, int cardCount)
        {
            var cardsOfDeck = dbContext.CardsInDecks.AsNoTracking()
                .Include(card => card.Card).AsSingleQuery()
                .Where(card => card.DeckId.Equals(deckId) && !excludedCardIds.Contains(card.CardId));

            var onUnknownHeap = cardsOfDeck.Where(cardInDeck => cardInDeck.CurrentHeap == 0);
            var withoutExcludedCards = onUnknownHeap;
            foreach (var tag in excludedTagIds)   //I tried to do better with an intersect between the two sets, but that failed
                withoutExcludedCards = withoutExcludedCards.Where(cardInDeck => !cardInDeck.Card.TagsInCards.Where(tagInCard => tagInCard.TagId == tag).Any());
            var oldest = withoutExcludedCards.OrderBy(cardInDeck => cardInDeck.LastLearnUtcTime).Take(cardCount * 3);   //we take more cards for shuffling accross more

            var withDetails = oldest.Select(cardInDeck => new
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
                imageIdAndCardSides = cardInDeck.Card.Images.Select(img => new { img.ImageId, img.CardSide })
            });

            var listed = await withDetails.ToListAsync();
            var cardIds = listed.Select(cardInDeck => cardInDeck.CardId).ToImmutableHashSet();
            var ratings = CardRatings.Load(dbContext, userId, cardIds);
            var notifications = GetNotifications(userId, cardIds);

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
                cardInDeck.imageIdAndCardSides.Select(imageIdAndCardSide => new ResultImageModel(imagesDetails[imageIdAndCardSide.ImageId], imageIdAndCardSide.CardSide)),
                heapingAlgorithm,
                ratings.User(cardInDeck.CardId),
                ratings.Average(cardInDeck.CardId),
                ratings.Count(cardInDeck.CardId),
                notifications[cardInDeck.CardId]
            ));
            return Shuffler.Shuffle(result).Take(cardCount);
        }
        private ImmutableDictionary<Guid, ImageDetails> GetAllImagesDetails()
        {
            var imageInfos = dbContext.Images.AsNoTracking().Select(i => new ImageDetails(i.Id, i.Name, i.Owner.UserName, i.Description, i.Source));
            return imageInfos.ToImmutableDictionary(img => img.ImageId, img => img);
        }
        #endregion
        #region private classes
        private static class Shuffler
        {
            public static IEnumerable<T> Shuffle<T>(IEnumerable<T> source)
            {
                return Shuffle(source, new Random());
            }
            private static IEnumerable<T> Shuffle<T>(IEnumerable<T> source, Random rng)
            {
                if (source == null) throw new ArgumentNullException(nameof(source));
                if (rng == null) throw new ArgumentNullException(nameof(rng));

                return ShuffleIterator(source, rng);
            }
            private static IEnumerable<T> ShuffleIterator<T>(IEnumerable<T> source, Random rng)
            {
                var buffer = source.ToList();
                for (int i = 0; i < buffer.Count; i++)
                {
                    int j = rng.Next(i, buffer.Count);
                    yield return buffer[j];

                    buffer[j] = buffer[i];
                }
            }
        }
        public sealed class ImageDetails
        {
            public ImageDetails(Guid imageId, string name, string ownerName, string description, string source)
            {
                Name = name;
                OwnerName = ownerName;
                Description = description;
                Source = source;
                ImageId = imageId;
            }
            public Guid ImageId { get; }
            public string Name { get; set; }
            public string OwnerName { get; set; }
            public string Description { get; set; }
            public string Source { get; set; }
        }
        #endregion
        public GetUnknownCardsToLearn(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task<IEnumerable<ResultCard>> RunAsync(Request request)
        {
            request.CheckValidity();
            QueryValidationHelper.CheckUserIsOwnerOfDeck(dbContext, request.CurrentUserId, request.DeckId);

            var heapingAlgorithm = await GetHeapingAlgorithmAsync(request.DeckId);
            var userNames = dbContext.Users.AsNoTracking().Select(u => new { u.Id, u.UserName }).ToImmutableDictionary(u => u.Id, u => u.UserName);
            var imagesDetails = GetAllImagesDetails();
            var tagNames = GetAllAvailableTags.Run(dbContext);

            return await GetUnknownCardsAsync(request.CurrentUserId, request.DeckId, request.ExcludedCardIds, request.ExcludedTagIds, heapingAlgorithm, userNames, imagesDetails, tagNames, request.CardsToDownload);
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
            public void CheckValidity()
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
                    .Concat(new[] { new MoveToHeapExpiryInfo(0, DateTime.MinValue.ToUniversalTime()) });
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
            public ResultImageModel(ImageDetails img, int cardSide)
            {
                ImageId = img.ImageId;
                Name = img.Name;
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
using MemCheck.Application.Heaping;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application
{
    public sealed class SearchCards
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        #region Private class ResultCardBeforeDeckInfo
        private sealed class ResultCardBeforeDeckInfo
        {
            public ResultCardBeforeDeckInfo(Guid cardId, string frontSide, IEnumerable<string> tags, IEnumerable<string> visibleTo, CardRatings cardRatings)
            {
                CardId = cardId;
                FrontSide = frontSide;
                Tags = tags;
                VisibleTo = visibleTo;

                CurrentUserRating = cardRatings.User(cardId);
                AverageRating = cardRatings.Average(cardId);
                CountOfUserRatings = cardRatings.Count(cardId);
            }
            public Guid CardId { get; }
            public string FrontSide { get; }
            public IEnumerable<string> Tags { get; }
            public IEnumerable<string> VisibleTo { get; }
            public int CurrentUserRating { get; }
            public double AverageRating { get; }
            public int CountOfUserRatings { get; }
        }
        #endregion
        #region private methods
        private static ResultCardDeckInfo GetCardDeckInfo(CardInDeck cardInDeck, Dictionary<Guid, HeapingAlgorithm> heapingAlgoCache)
        {
            var heapingAlgo = heapingAlgoCache[cardInDeck.DeckId];
            return new ResultCardDeckInfo(
                cardInDeck.DeckId,
                cardInDeck.Deck.Description,
                cardInDeck.CurrentHeap,
                cardInDeck.BiggestHeapReached,
                cardInDeck.NbTimesInNotLearnedHeap,
                cardInDeck.AddToDeckUtcTime,
                cardInDeck.LastLearnUtcTime,
                cardInDeck.CurrentHeap == 0 ? true : heapingAlgo.HasExpired(cardInDeck.CurrentHeap, cardInDeck.LastLearnUtcTime),
                cardInDeck.CurrentHeap == 0 ? DateTime.MinValue : heapingAlgo.ExpiryUtcDate(cardInDeck.CurrentHeap, cardInDeck.LastLearnUtcTime));
        }
        private IEnumerable<ResultCardDeckInfo> GetCardDeckInfo(ResultCardBeforeDeckInfo card, Guid userId, Dictionary<Guid, HeapingAlgorithm> heapingAlgoCache)
        {
            var cardsInDecksForThisUserAndThisCard = dbContext.CardsInDecks
                .AsNoTracking()
                .Include(card => card.Deck)
                .Where(cardInDeck => cardInDeck.Deck.Owner.Id == userId && cardInDeck.CardId == card.CardId)
                .ToArray();

            foreach (var cardInDeck in cardsInDecksForThisUserAndThisCard)
                if (!heapingAlgoCache.ContainsKey(cardInDeck.DeckId))
                    heapingAlgoCache.Add(cardInDeck.DeckId, HeapingAlgorithms.Instance.FromId(cardInDeck.Deck.HeapingAlgorithmId));

            return cardsInDecksForThisUserAndThisCard.Select(cardInDeck => GetCardDeckInfo(cardInDeck, heapingAlgoCache));
        }
        private IEnumerable<ResultCard> AddDeckInfo(Guid userId, IEnumerable<ResultCardBeforeDeckInfo> resultCards)
        {
            var heapingAlgoCache = new Dictionary<Guid, HeapingAlgorithm>();
            return resultCards.Select(card => new ResultCard(card.CardId, card.FrontSide, card.Tags, card.VisibleTo, GetCardDeckInfo(card, userId, heapingAlgoCache), card.CurrentUserRating, card.AverageRating, card.CountOfUserRatings));
        }
        #endregion
        public SearchCards(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public Result Run(Request request, Guid userId)
        {
            var allCards = dbContext.Cards.AsNoTracking()
                .Include(card => card.TagsInCards)
                .Include(card => card.VersionCreator)
                .Include(card => card.CardLanguage)
                .Include(card => card.UsersWithView)
                .AsSingleQuery();

            var cardsViewableByUser = allCards.Where(
                card =>
                card.VersionCreator.Id == userId
                || !card.UsersWithView.Any()    //card is public
                || card.UsersWithView.Where(userWithView => userWithView.UserId == userId).Any()
                );

            IQueryable<Card> cardsFilteredWithDeck;
            if (request.Deck != Guid.Empty)
            {
                if (request.DeckIsInclusive)
                    cardsFilteredWithDeck = cardsViewableByUser.Where(card =>
                        dbContext.CardsInDecks.AsNoTracking().Where(cardInDeck => cardInDeck.CardId == card.Id && cardInDeck.DeckId == request.Deck).Any()
                        && (request.Heap == -1 || dbContext.CardsInDecks.AsNoTracking().Single(cardInDeck => cardInDeck.CardId == card.Id && cardInDeck.DeckId == request.Deck).CurrentHeap == request.Heap)
                        );
                else
                    cardsFilteredWithDeck = cardsViewableByUser.Where(card => !dbContext.CardsInDecks.AsNoTracking().Where(cardInDeck => cardInDeck.CardId == card.Id && cardInDeck.DeckId == request.Deck).Any());
            }
            else
                cardsFilteredWithDeck = cardsViewableByUser;

            var cardsFilteredWithRequiredTags = cardsFilteredWithDeck;
            foreach (var tag in request.RequiredTags)   //I tried to do better with an intersect between the two sets, but that failed
                cardsFilteredWithRequiredTags = cardsFilteredWithRequiredTags.Where(card => card.TagsInCards.Where(tagInCard => tagInCard.TagId == tag).Any());

            var cardsFilteredWithExludedTags = cardsFilteredWithRequiredTags;
            if (request.ExcludedTags == null)
                cardsFilteredWithExludedTags = cardsFilteredWithExludedTags.Where(card => !card.TagsInCards.Any());
            else
                foreach (var tag in request.ExcludedTags)   //I tried to do better with an intersect between the two sets, but that failed
                    cardsFilteredWithExludedTags = cardsFilteredWithExludedTags.Where(card => !card.TagsInCards.Where(tagInCard => tagInCard.TagId == tag).Any());

            var cardsFilteredWithText = string.IsNullOrEmpty(request.RequiredText) ? cardsFilteredWithExludedTags :
                cardsFilteredWithExludedTags.Where(card =>
                    EF.Functions.Like(card.FrontSide, $"%{request.RequiredText}%")
                    || EF.Functions.Like(card.BackSide, $"%{request.RequiredText}%")
                    || EF.Functions.Like(card.AdditionalInfo, $"%{request.RequiredText}%")
                );

            IQueryable<Card> cardsFilteredWithVisibility;
            if (request.Visibility == 2)
                cardsFilteredWithVisibility = cardsFilteredWithText.Where(card => card.UsersWithView.Count() != 1);
            else
            if (request.Visibility == 3)
                cardsFilteredWithVisibility = cardsFilteredWithText.Where(card => card.UsersWithView.Count() == 1);
            else
                cardsFilteredWithVisibility = cardsFilteredWithText;

            IQueryable<Card> cardsFilteredWithAverageRating;
            CardRatings? cardRatings = null;
            if (request.RatingFilteringMode == 1)
                cardsFilteredWithAverageRating = cardsFilteredWithVisibility;
            else
            {
                if (cardsFilteredWithVisibility.Count() > 20000)
                    throw new SearchResultTooBigForRatingException();

                cardRatings = CardRatings.Load(dbContext, userId, cardsFilteredWithVisibility.Select(card => card.Id).ToImmutableHashSet());
                if (request.RatingFilteringMode == 4)
                    cardsFilteredWithAverageRating = cardsFilteredWithVisibility.Where(card => cardRatings.CardsWithoutEval.Contains(card.Id));
                else
                if (request.RatingFilteringMode == 2)
                    cardsFilteredWithAverageRating = cardsFilteredWithVisibility.Where(card => cardRatings.CardsWithAverageRatingAtLeast(request.RatingFilteringValue).Contains(card.Id));
                else
                    cardsFilteredWithAverageRating = cardsFilteredWithVisibility.Where(card => cardRatings.CardsWithAverageRatingAtMost(request.RatingFilteringValue).Contains(card.Id));
            }

            IQueryable<Card> cardsFilteredWithNotifications;
            if (request.NotificationFiltering == 1)
                cardsFilteredWithNotifications = cardsFilteredWithAverageRating;
            else
            {
                var notifMustExist = request.NotificationFiltering == 2;
                cardsFilteredWithNotifications = cardsFilteredWithAverageRating.Where(card => dbContext.CardNotifications.Where(cardNotif => cardNotif.CardId == card.Id && cardNotif.UserId == userId).Any() == notifMustExist);
            }

            var finalResult = cardsFilteredWithNotifications;
            finalResult = finalResult.OrderByDescending(card => card.VersionUtcDate); //For Take() and Skip(), just below, to work, we need to have an order. In future versions we will offer the user some sorting

            var totalNbCards = finalResult.Count();
            var totalPageCount = (int)Math.Ceiling(((double)totalNbCards) / request.pageSize);

            var pageCards = finalResult.Skip((request.pageNo - 1) * request.pageSize).Take(request.pageSize);

            if (cardRatings == null)
                cardRatings = CardRatings.Load(dbContext, userId, pageCards.Select(card => card.Id).ToImmutableHashSet());

            var resultCards = pageCards.Select(card => new ResultCardBeforeDeckInfo(card.Id, card.FrontSide, card.TagsInCards.Select(tagInCard => tagInCard.Tag.Name), card.UsersWithView.Select(userWithView => userWithView.User.UserName), cardRatings)).ToList();

            var withUserDeckInfo = AddDeckInfo(userId, resultCards);

            return new Result(totalNbCards, totalPageCount, withUserDeckInfo);
        }
        #region Request and result classes
        public sealed class Request
        {
            public Request(Guid deck, bool deckIsInclusive, int heap, int pageNo, int pageSize, string requiredText, IEnumerable<Guid> requiredTags, IEnumerable<Guid>? excludedTags, int visibility, int ratingFilteringMode, int ratingFilteringValue, int notificationFiltering)
            {
                if (pageNo < 1) throw new ArgumentException($"First page is numbered 1, received a request for page {pageNo}");
                RequiredTags = requiredTags;
                ExcludedTags = excludedTags;
                Visibility = visibility;
                Deck = deck;
                DeckIsInclusive = deckIsInclusive;
                Heap = heap;
                this.pageNo = pageNo;
                this.pageSize = pageSize;
                RequiredText = requiredText;
                RatingFilteringMode = ratingFilteringMode;
                RatingFilteringValue = ratingFilteringValue;
                NotificationFiltering = notificationFiltering;
            }
            public Guid Deck { get; }
            public bool DeckIsInclusive { get; }    //Makes sense only if Deck is not Guid.Empty
            public int Heap { get; set; }   //-1 stands for ignore
            public int pageNo { get; }
            public int pageSize { get; }
            public string RequiredText { get; }
            public int Visibility { get; }//1 = ignore this criteria, 2 = cards which can be seen by more than their owner, 3 = cards visible to their owner only
            public int RatingFilteringMode { get; set; } //1 = ignore this criteria, 2 = at least RatingFilteringValue, 3 = at most RatingFilteringValue, 4 = without any rating
            public int RatingFilteringValue { get; set; } //1 to 5
            public IEnumerable<Guid> RequiredTags { get; }
            public IEnumerable<Guid>? ExcludedTags { get; } //null means that we return only cards which have no tag (we exclude all tags)
            public int NotificationFiltering { get; set; } //1 = ignore this criteria, 2 = cards registered for notification, 3 = cards not registered for notification
        }
        public sealed class Result
        {
            public Result(int totalNbCards, int totalPageCount, IEnumerable<ResultCard> cards)
            {
                TotalNbCards = totalNbCards;
                PageCount = totalPageCount;
                Cards = cards;
            }
            public int TotalNbCards { get; }
            public int PageCount { get; }
            public IEnumerable<ResultCard> Cards { get; }
        }
        public sealed class ResultCard
        {
            public ResultCard(Guid cardId, string frontSide, IEnumerable<string> tags, IEnumerable<string> visibleTo, IEnumerable<ResultCardDeckInfo> deckInfo, int currentUserRating, double averageRating, int countOfUserRatings)
            {
                CardId = cardId;
                FrontSide = frontSide.Truncate(200, true);
                Tags = tags;
                VisibleTo = visibleTo;
                DeckInfo = deckInfo;
                CurrentUserRating = currentUserRating;
                AverageRating = averageRating;
                CountOfUserRatings = countOfUserRatings;
            }
            public Guid CardId { get; }
            public string FrontSide { get; }
            public IEnumerable<string> Tags { get; }
            public IEnumerable<string> VisibleTo { get; }
            public IEnumerable<ResultCardDeckInfo> DeckInfo { get; }
            public int CurrentUserRating { get; }
            public double AverageRating { get; }
            public int CountOfUserRatings { get; }
        }
        public sealed class ResultCardDeckInfo
        {
            public ResultCardDeckInfo(Guid deckId, string deckName, int currentHeap, int biggestHeapReached, int nbTimesInNotLearnedHeap, DateTime addToDeckUtcTime, DateTime lastLearnUtcTime, bool expired, DateTime expiryUtcDate)
            {
                DeckId = deckId;
                DeckName = deckName;
                CurrentHeap = currentHeap;
                BiggestHeapReached = biggestHeapReached;
                NbTimesInNotLearnedHeap = nbTimesInNotLearnedHeap;
                AddToDeckUtcTime = addToDeckUtcTime;
                LastLearnUtcTime = lastLearnUtcTime;
                Expired = expired;
                ExpiryUtcDate = expiryUtcDate;
            }
            public Guid DeckId { get; }
            public string DeckName { get; }
            public int CurrentHeap { get; }
            public int BiggestHeapReached { get; }
            public int NbTimesInNotLearnedHeap { get; }
            public DateTime AddToDeckUtcTime { get; }
            public DateTime LastLearnUtcTime { get; }
            public bool Expired { get; }
            public DateTime ExpiryUtcDate { get; }
        }
        #endregion
    }
}
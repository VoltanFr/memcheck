using MemCheck.Application.Heaping;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MemCheck.Application.Searching
{
    public sealed class SearchDeckCards
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public SearchDeckCards(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public SearchResult Run(SearchRequest request)
        {
            if (request.RequireCardsHaveNoTag && request.RequiredTags.Any())
                throw new ArgumentException("request.RequireCardsHaveNoTag && request.RequiredTags.Any() is forbidden");

            var allCards = dbContext.CardsInDecks.Include(cardInDeck => cardInDeck.Card).ThenInclude(card => card.TagsInCards).Include(cardInDeck => cardInDeck.Card.UsersWithView);
            var allCardsInTheDeck = allCards.Where(cardInDeck => cardInDeck.DeckId == request.DeckId);

            var cardFilteredWithHeaps = request.HeapFilter == null ? allCardsInTheDeck : allCardsInTheDeck.Where(cardInDeck => cardInDeck.CurrentHeap == request.HeapFilter);

            var cardsFilteredWithTags = cardFilteredWithHeaps;
            if (request.RequireCardsHaveNoTag)
                cardsFilteredWithTags = cardsFilteredWithTags.Where(card => !card.Card.TagsInCards.Any());
            else
                foreach (var tagId in request.RequiredTags)   //I tried to do better with an intersect between the two sets, but that failed
                    cardsFilteredWithTags = cardsFilteredWithTags.Where(card => card.Card.TagsInCards.Where(tagInCard => tagInCard.TagId == tagId).Count() > 0);

            var cardFilteredWithText = request.TextFilter.Length == 0 ? cardsFilteredWithTags :
                cardsFilteredWithTags.Where(
                    card =>
                    EF.Functions.Like(card.Card.FrontSide, $"%{request.TextFilter}%")
                    || EF.Functions.Like(card.Card.BackSide, $"%{request.TextFilter}%")
                    || EF.Functions.Like(card.Card.AdditionalInfo, $"%{request.TextFilter}%")
                );

            var finalRequest = cardFilteredWithText;

            var totalNbCards = finalRequest.Count();
            var totalPageCount = totalNbCards / request.pageSize + 1;

            var pageCards = finalRequest.Skip((request.pageNo - 1) * request.pageSize).Take(request.pageSize);

            var deck = dbContext.Decks.Where(deck => deck.Id == request.DeckId).Single();
            var heapingAlgo = HeapingAlgorithms.Instance.FromId(deck.HeapingAlgorithmId);

            var resultCards = pageCards.Select(card => new SearchResultCard(
                card.Card.Id,
                card.Card.FrontSide,
                card.Card.TagsInCards.Select(tagInCard => tagInCard.Tag.Name),
                card.CurrentHeap == Deck.UnknownDeckId ? null : (DateTime?)heapingAlgo.ExpiryUtcDate(card.CurrentHeap, card.LastLearnUtcTime),
                card.LastLearnUtcTime,
                card.AddToDeckUtcTime,
                card.CurrentHeap == Deck.UnknownDeckId ? true : heapingAlgo.HasExpired(card.CurrentHeap, card.LastLearnUtcTime),
                card.CurrentHeap,
                card.NbTimesInNotLearnedHeap,
                card.BiggestHeapReached,
                card.Card.UsersWithView.Select(userWithView => userWithView.User.UserName)
                )).ToList();

            var ordered = resultCards.OrderBy(searchResultCard => searchResultCard.ExpiryUtcDate);

            return new SearchResult(totalNbCards, totalPageCount, ordered);

        }
        public sealed class SearchRequest
        {
            public SearchRequest(Guid deckId, IEnumerable<Guid> requiredTags, bool requireCardsHaveNoTag, int? heapFilter, string textFilter, int pageNo, int pageSize)
            {
                DeckId = deckId;
                RequiredTags = requiredTags;
                RequireCardsHaveNoTag = requireCardsHaveNoTag;
                HeapFilter = heapFilter;
                TextFilter = textFilter;
                this.pageNo = pageNo;
                this.pageSize = pageSize;
            }
            public Guid DeckId { get; }
            public IEnumerable<Guid> RequiredTags { get; }
            public bool RequireCardsHaveNoTag { get; }
            public int? HeapFilter { get; }
            public string TextFilter { get; }
            public int pageNo { get; }
            public int pageSize { get; }
        }
        public sealed class SearchResult
        {
            public SearchResult(int totalNbCards, int totalPageCount, IEnumerable<SearchResultCard> cards)
            {
                TotalNbCards = totalNbCards;
                PageCount = totalPageCount;
                Cards = cards;
            }
            public int TotalNbCards { get; }
            public int PageCount { get; }
            public IEnumerable<SearchResultCard> Cards { get; }
        }
        public sealed class SearchResultCard
        {
            public SearchResultCard(Guid cardId, string frontSide, IEnumerable<string> tags, DateTime? expiryUtcDate, DateTime lastLearnUtcDate, DateTime addToDeckUtcTime, bool expired,
                int heap, int nbTimesInNotLearnedHeap, int biggestHeapReached, IEnumerable<string> visibleTo)
            {
                if (expiryUtcDate != null)
                    DateServices.CheckUTC(expiryUtcDate.Value);
                CardId = cardId;
                FrontSide = frontSide;
                Tags = tags;
                ExpiryUtcDate = expiryUtcDate;
                LastLearnUtcDate = lastLearnUtcDate;
                AddToDeckUtcTime = addToDeckUtcTime;
                Expired = expired;
                Heap = heap;
                NbTimesInNotLearnedHeap = nbTimesInNotLearnedHeap;
                BiggestHeapReached = biggestHeapReached;
                VisibleTo = visibleTo;
            }
            public Guid CardId { get; }
            public string FrontSide { get; }
            public IEnumerable<string> Tags { get; }
            public DateTime? ExpiryUtcDate { get; }
            public DateTime LastLearnUtcDate { get; }
            public DateTime AddToDeckUtcTime { get; }
            public bool Expired { get; }
            public int Heap { get; }
            public int NbTimesInNotLearnedHeap { get; }
            public int BiggestHeapReached { get; }
            public IEnumerable<string> VisibleTo { get; }
        }
    }
}

using MemCheck.Application.Heaping;
using MemCheck.Database;
using MemCheck.Domain;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Loading
{
    public sealed class GetDecksWithLearnCounts
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        #region Private methods
        private Result GetDeck(Guid deckId, int heapingAlgorithmId, string description)
        {
            var allCards = dbContext.CardsInDecks.AsNoTracking().Where(cardInDeck => cardInDeck.DeckId == deckId)
                .Select(cardInDeck => new { cardInDeck.CurrentHeap, cardInDeck.LastLearnUtcTime, cardInDeck.DeckId })
                .ToList();
            var groups = allCards.ToLookup(card => card.CurrentHeap == 0);
            HeapingAlgorithm heapingAlgorithm = HeapingAlgorithms.Instance.FromId(heapingAlgorithmId);
            var expiredCardCount = 0;
            var nextExpiryUTCDate = DateTime.MaxValue;
            foreach (var card in groups[false])
            {
                var expiryDate = heapingAlgorithm.ExpiryUtcDate(card.CurrentHeap, card.LastLearnUtcTime);
                if (expiryDate <= DateTime.UtcNow)
                    expiredCardCount++;
                else
                    if (expiryDate < nextExpiryUTCDate)
                    nextExpiryUTCDate = expiryDate;
            }
            return new Result(deckId, description, groups[true].Count(), expiredCardCount, allCards.Count, nextExpiryUTCDate);
        }
        #endregion
        public GetDecksWithLearnCounts(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public IEnumerable<Result> Run(Guid userId)
        {
            var decks = dbContext.Decks.AsNoTracking().Where(deck => deck.Owner.Id == userId).Select(deck => new { deck.Id, deck.Description, deck.HeapingAlgorithmId }).ToList();
            return decks.Select(deck => GetDeck(deck.Id, deck.HeapingAlgorithmId, deck.Description)).ToList();
        }
        #region Result class
        public sealed class Result
        {
            public Result(Guid id, string description, int unknownCardCount, int expiredCardCount, int cardCount, DateTime nextExpiryUTCDate)
            {
                Id = id;
                Description = description;
                UnknownCardCount = unknownCardCount;
                ExpiredCardCount = expiredCardCount;
                CardCount = cardCount;
                NextExpiryUTCDate = nextExpiryUTCDate;
            }
            public Guid Id { get; }
            public string Description { get; }
            public int UnknownCardCount { get; }
            public int ExpiredCardCount { get; }
            public int CardCount { get; }
            public bool IsEmpty { get; }
            public DateTime NextExpiryUTCDate { get; }
        }
        #endregion
    }
}

using MemCheck.Application.Heaping;
using MemCheck.Database;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MemCheck.Application.Decks
{
    public sealed class GetUserDecksWithHeaps
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        #region Private class HeapInfo
        private sealed class HeapInfo
        {
            public HeapInfo(int heapId)
            {
                HeapId = heapId;
                TotalCardCount = 0;
                ExpiredCardCount = 0;
                NextExpiryUtcDate = DateTime.MaxValue;
            }
            public int HeapId { get; set; }
            public int TotalCardCount { get; set; }
            public int ExpiredCardCount { get; set; }
            public DateTime NextExpiryUtcDate { get; set; }
        }
        #endregion
        public GetUserDecksWithHeaps(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public IEnumerable<ResultModel> Run(Guid userId)
        {
            var result = new List<ResultModel>();
            var decks = dbContext.Decks.Where(deck => deck.Owner.Id == userId).OrderBy(deck => deck.Description).Select(
                deck => new { deck.Id, deck.Description, deck.HeapingAlgorithmId, CardCount = deck.CardInDecks.Count }
                ).ToList();

            foreach (var deck in decks)
            {
                var heaps = new Dictionary<int, HeapInfo>();
                var heapingAlgorithm = HeapingAlgorithms.Instance.FromId(deck.HeapingAlgorithmId);

                var cardsInDeck = dbContext.CardsInDecks.Where(cardInDeck => cardInDeck.DeckId == deck.Id).ToList();

                cardsInDeck.ForEach(cardInDeck =>
                {
                    if (!heaps.ContainsKey(cardInDeck.CurrentHeap))
                        heaps.Add(cardInDeck.CurrentHeap, new HeapInfo(cardInDeck.CurrentHeap));
                    var heapInfo = heaps[cardInDeck.CurrentHeap];
                    heapInfo.TotalCardCount++;
                    if (cardInDeck.CurrentHeap != 0)
                    {
                        if (heapingAlgorithm.HasExpired(cardInDeck.CurrentHeap, cardInDeck.LastLearnUtcTime, DateTime.UtcNow))
                            heapInfo.ExpiredCardCount++;
                        var expiryDate = heapingAlgorithm.ExpiryUtcDate(cardInDeck.CurrentHeap, cardInDeck.LastLearnUtcTime);
                        if (expiryDate < heapInfo.NextExpiryUtcDate)
                            heapInfo.NextExpiryUtcDate = expiryDate;
                    }
                });

                result.Add(new ResultModel(deck.Id, deck.Description, deck.HeapingAlgorithmId, deck.CardCount,
                    heaps.Values.Select(heapInfo => new ResultHeapModel(heapInfo.HeapId, heapInfo.TotalCardCount, heapInfo.ExpiredCardCount, heapInfo.NextExpiryUtcDate)))
                    );
            }

            return result;
        }
        public sealed class ResultModel
        {
            public ResultModel(Guid deckId, string description, int heapingAlgorithmId, int cardCount, IEnumerable<ResultHeapModel> heaps)
            {
                DeckId = deckId;
                Description = description;
                HeapingAlgorithmId = heapingAlgorithmId;
                CardCount = cardCount;
                Heaps = heaps;
            }
            public Guid DeckId { get; set; }
            public string Description { get; }
            public int HeapingAlgorithmId { get; }
            public int CardCount { get; }
            public IEnumerable<ResultHeapModel> Heaps { get; }
        }
        public sealed class ResultHeapModel
        {
            public ResultHeapModel(int heapId, int totalCardCount, int expiredCardCount, DateTime nextExpiryUtcDate)
            {
                HeapId = heapId;
                TotalCardCount = totalCardCount;
                ExpiredCardCount = expiredCardCount;
                NextExpiryUtcDate = nextExpiryUtcDate;
            }
            public int HeapId { get; }
            public int TotalCardCount { get; }
            public int ExpiredCardCount { get; }
            public DateTime NextExpiryUtcDate { get; }
        }
    }
}

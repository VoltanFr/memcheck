using MemCheck.Application.Heaping;
using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

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
            public int HeapId { get; }
            public int TotalCardCount { get; set; }
            public int ExpiredCardCount { get; set; }
            public DateTime NextExpiryUtcDate { get; set; }
        }
        #endregion
        public GetUserDecksWithHeaps(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task<IEnumerable<Result>> RunAsync(Request request, DateTime? nowUtc = null)
        {
            await request.CheckValidityAsync(dbContext);
            nowUtc ??= DateTime.UtcNow;

            var result = new List<Result>();
            var decks = dbContext.Decks.Where(deck => deck.Owner.Id == request.UserId).OrderBy(deck => deck.Description).Select(
                deck => new { deck.Id, deck.Description, deck.HeapingAlgorithmId, CardCount = deck.CardInDecks.Count }
                ).ToImmutableArray();

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
                        if (heapingAlgorithm.HasExpired(cardInDeck.CurrentHeap, cardInDeck.LastLearnUtcTime, nowUtc.Value))
                            heapInfo.ExpiredCardCount++;
                        else
                        {
                            var expiryDate = heapingAlgorithm.ExpiryUtcDate(cardInDeck.CurrentHeap, cardInDeck.LastLearnUtcTime);
                            if (expiryDate < heapInfo.NextExpiryUtcDate)
                                heapInfo.NextExpiryUtcDate = expiryDate;
                        }
                    }
                });

                result.Add(new Result(deck.Id, deck.Description, deck.HeapingAlgorithmId, deck.CardCount,
                    heaps.Values.Select(heapInfo => new ResultHeap(heapInfo.HeapId, heapInfo.TotalCardCount, heapInfo.ExpiredCardCount, heapInfo.NextExpiryUtcDate)))
                    );
            }

            return result;
        }
        #region Request & Result
        public sealed record Request(Guid UserId)
        {
            public async Task CheckValidityAsync(MemCheckDbContext dbContext)
            {
                await QueryValidationHelper.CheckUserExistsAsync(dbContext, UserId);
            }
        }
        public sealed record Result(Guid DeckId, string Description, int HeapingAlgorithmId, int CardCount, IEnumerable<ResultHeap> Heaps);
        public sealed record ResultHeap(int HeapId, int TotalCardCount, int ExpiredCardCount, DateTime NextExpiryUtcDate);
        //NextExpiryUtcDate is the first expiry date among the non expired cards in this heap, so it is meaningless (DateTime.MaxValue) if all cards in the heap are expired.
        //ExpiredCardCount and NextExpiryUtcDate are meaningless for heap 0.
        #endregion

    }
}

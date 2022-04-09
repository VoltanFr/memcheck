using MemCheck.Application.QueryValidation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Decks
{
    public sealed class GetUserDecksWithHeaps : RequestRunner<GetUserDecksWithHeaps.Request, IEnumerable<GetUserDecksWithHeaps.Result>>
    {
        #region Fields
        private readonly DateTime? runDate;
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
        public GetUserDecksWithHeaps(CallContext callContext, DateTime? runDate = null) : base(callContext)
        {
            this.runDate = runDate;
        }
        protected override async Task<ResultWithMetrologyProperties<IEnumerable<Result>>> DoRunAsync(Request request)
        {
            var nowUtc = runDate == null ? DateTime.UtcNow : runDate;

            var result = new List<Result>();
            var decks = await DbContext.Decks.Where(deck => deck.Owner.Id == request.UserId).OrderBy(deck => deck.Description).Select(
                deck => new { deck.Id, deck.Description, deck.HeapingAlgorithmId, CardCount = deck.CardInDecks.Count }
                ).ToArrayAsync();

            foreach (var deck in decks)
            {
                var heaps = new Dictionary<int, HeapInfo>();

                var cardsInDeck = await DbContext.CardsInDecks.Where(cardInDeck => cardInDeck.DeckId == deck.Id).ToListAsync();

                cardsInDeck.ForEach(cardInDeck =>
                {
                    if (!heaps.ContainsKey(cardInDeck.CurrentHeap))
                        heaps.Add(cardInDeck.CurrentHeap, new HeapInfo(cardInDeck.CurrentHeap));
                    var heapInfo = heaps[cardInDeck.CurrentHeap];
                    heapInfo.TotalCardCount++;
                    if (cardInDeck.CurrentHeap != 0)
                    {
                        if (cardInDeck.ExpiryUtcTime <= nowUtc.Value)
                            heapInfo.ExpiredCardCount++;
                        else
                        {
                            if (cardInDeck.ExpiryUtcTime < heapInfo.NextExpiryUtcDate)
                                heapInfo.NextExpiryUtcDate = cardInDeck.ExpiryUtcTime;
                        }
                    }
                });

                result.Add(new Result(deck.Id, deck.Description, deck.HeapingAlgorithmId, deck.CardCount,
                    heaps.Values.Select(heapInfo => new ResultHeap(heapInfo.HeapId, heapInfo.TotalCardCount, heapInfo.ExpiredCardCount, heapInfo.NextExpiryUtcDate)))
                    );
            }

            return new ResultWithMetrologyProperties<IEnumerable<Result>>(result, IntMetric("DeckCount", result.Count));
        }
        #region Request & Result
        public sealed record Request(Guid UserId) : IRequest
        {
            public async Task CheckValidityAsync(CallContext callContext)
            {
                await QueryValidationHelper.CheckUserExistsAsync(callContext.DbContext, UserId);
            }
        }
        public sealed record Result(Guid DeckId, string Description, int HeapingAlgorithmId, int CardCount, IEnumerable<ResultHeap> Heaps);
        public sealed record ResultHeap(int HeapId, int TotalCardCount, int ExpiredCardCount, DateTime NextExpiryUtcDate);
        //NextExpiryUtcDate is the first expiry date among the non expired cards in this heap, so it is meaningless (DateTime.MaxValue) if all cards in the heap are expired.
        //ExpiredCardCount and NextExpiryUtcDate are meaningless for heap 0.
        #endregion
    }
}

using MemCheck.Application.QueryValidation;
using MemCheck.Domain;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Heaping
{
    //This is used during manual moves
    //Learning moves are implemented by the sister class MoveCardToHeap
    public sealed class MoveCardsToHeap : RequestRunner<MoveCardsToHeap.Request, MoveCardsToHeap.Result>
    {
        public MoveCardsToHeap(CallContext callContext) : base(callContext)
        {
        }
        protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
        {
            var heapingAlgorithm = await HeapingAlgorithm.OfDeckAsync(DbContext, request.DeckId);
            var cardsInDecks = DbContext.CardsInDecks.Where(card => card.DeckId.Equals(request.DeckId) && request.CardIds.Any(cardId => cardId == card.CardId)).ToImmutableDictionary(c => c.CardId, c => c);

            if (request.CardIds.Any(cardId => !cardsInDecks.ContainsKey(cardId)))
                throw new InvalidOperationException("One card is not in the deck");

            foreach (var cardInDeck in cardsInDecks.Values)
                if (cardInDeck.CurrentHeap != request.TargetHeap)
                {
                    if (cardInDeck.BiggestHeapReached < request.TargetHeap)
                        cardInDeck.BiggestHeapReached = request.TargetHeap;

                    if (request.TargetHeap == CardInDeck.UnknownHeap)
                    {
                        cardInDeck.NbTimesInNotLearnedHeap++;
                        cardInDeck.ExpiryUtcTime = DateTime.MinValue.ToUniversalTime(); //Setting this is useless for normal operations, but would help detect any misuse of this field (bugs)
                    }
                    else
                        cardInDeck.ExpiryUtcTime = heapingAlgorithm.ExpiryUtcDate(request.TargetHeap, cardInDeck.LastLearnUtcTime);

                    cardInDeck.CurrentHeap = request.TargetHeap;
                }

            await DbContext.SaveChangesAsync();
            return new ResultWithMetrologyProperties<Result>(new Result(), ("DeckId", request.DeckId.ToString()), IntMetric("TargetHeap", request.TargetHeap), IntMetric("CardCount", request.CardIds.Count()));
        }
        #region Request & result
        public sealed record Request(Guid UserId, Guid DeckId, int TargetHeap, IEnumerable<Guid> CardIds) : IRequest
        {
            public async Task CheckValidityAsync(CallContext callContext)
            {
                QueryValidationHelper.CheckNotReservedGuid(DeckId);
                if (TargetHeap < CardInDeck.UnknownHeap || TargetHeap > CardInDeck.MaxHeapValue)
                    throw new InvalidOperationException($"Invalid target heap {TargetHeap}");
                await QueryValidationHelper.CheckUserIsOwnerOfDeckAsync(callContext.DbContext, UserId, DeckId);
            }
        }
        public sealed record Result();
        #endregion
    }
}

using MemCheck.Application.QueryValidation;
using MemCheck.Database;
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
    public sealed class MoveCardsToHeap
    {
        #region Fields
        private readonly CallContext callContext;
        #endregion
        public MoveCardsToHeap(CallContext callContext)
        {
            this.callContext = callContext;
        }
        public async Task RunAsync(Request request)
        {
            await request.CheckValidityAsync(callContext.DbContext);

            var heapingAlgorithm = await HeapingAlgorithm.OfDeckAsync(callContext.DbContext, request.DeckId);
            var cardsInDecks = callContext.DbContext.CardsInDecks.Where(card => card.DeckId.Equals(request.DeckId) && request.CardIds.Any(cardId => cardId == card.CardId)).ToImmutableDictionary(c => c.CardId, c => c);

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

            await callContext.DbContext.SaveChangesAsync();
            callContext.TelemetryClient.TrackEvent("MoveCardsToHeap", ("DeckId", request.DeckId.ToString()), ("TargetHeap", request.TargetHeap.ToString()), ("CardCount", request.CardIds.Count().ToString()));
        }
        #region Request
        public sealed record Request(Guid UserId, Guid DeckId, int TargetHeap, IEnumerable<Guid> CardIds)
        {
            public async Task CheckValidityAsync(MemCheckDbContext dbContext)
            {
                QueryValidationHelper.CheckNotReservedGuid(DeckId);
                if (TargetHeap < CardInDeck.UnknownHeap || TargetHeap > CardInDeck.MaxHeapValue)
                    throw new InvalidOperationException($"Invalid target heap {TargetHeap}");
                await QueryValidationHelper.CheckUserIsOwnerOfDeckAsync(dbContext, UserId, DeckId);
            }
        }
        #endregion
    }
}

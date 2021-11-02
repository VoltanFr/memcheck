using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Heaping
{
    //This is used during learning: a card either moves up one heap or down to the unknown heap
    //Manual moves are implemented by the sister class MoveCardsToHeap
    public sealed class MoveCardToHeap
    {
        #region Fields
        private readonly CallContext callContext;
        #endregion
        public MoveCardToHeap(CallContext callContext)
        {
            this.callContext = callContext;
        }
        public async Task RunAsync(Request request, DateTime? lastLearnUtcTime = null)
        {
            await request.CheckValidityAsync(callContext.DbContext);

            var card = await callContext.DbContext.CardsInDecks.SingleAsync(card => card.DeckId.Equals(request.DeckId) && card.CardId.Equals(request.CardId));

            lastLearnUtcTime ??= DateTime.UtcNow;

            if (request.TargetHeap != CardInDeck.UnknownHeap)
            {
                if (request.TargetHeap == card.CurrentHeap)
                    return; //This could happen due to connection problems on client side, or to multiple sessions

                if (request.TargetHeap < card.CurrentHeap || request.TargetHeap - card.CurrentHeap > 1)
                    throw new InvalidOperationException($"Invalid move request (request heap: {request.TargetHeap}, current heap: {card.CurrentHeap}, card: {request.CardId}, last learn UTC time: {card.LastLearnUtcTime})");

                var heapingAlgorithm = await HeapingAlgorithm.OfDeckAsync(callContext.DbContext, request.DeckId);
                card.ExpiryUtcTime = heapingAlgorithm.ExpiryUtcDate(request.TargetHeap, lastLearnUtcTime.Value);

                if (request.TargetHeap > card.BiggestHeapReached)
                    card.BiggestHeapReached = request.TargetHeap;
            }
            else
            {
                card.ExpiryUtcTime = DateTime.MinValue.ToUniversalTime(); //Setting this is useless for normal operations, but would help detect any misuse of this field (bugs)

                if (card.CurrentHeap != CardInDeck.UnknownHeap)
                    card.NbTimesInNotLearnedHeap++;
            }

            card.LastLearnUtcTime = lastLearnUtcTime.Value;
            card.CurrentHeap = request.TargetHeap;

            callContext.TelemetryClient.TrackEvent("MoveCardToHeap", ("DeckId", request.DeckId.ToString()), ("CardId", request.CardId.ToString()), ("TargetHeap", request.TargetHeap.ToString()));
            await callContext.DbContext.SaveChangesAsync();
        }
        #region Request
        public sealed record Request(Guid UserId, Guid DeckId, Guid CardId, int TargetHeap)
        {
            public async Task CheckValidityAsync(MemCheckDbContext dbContext)
            {
                QueryValidationHelper.CheckNotReservedGuid(DeckId);
                QueryValidationHelper.CheckNotReservedGuid(CardId);
                if (TargetHeap < CardInDeck.UnknownHeap || TargetHeap > CardInDeck.MaxHeapValue)
                    throw new InvalidOperationException($"Invalid target heap {TargetHeap}");
                await QueryValidationHelper.CheckUserIsOwnerOfDeckAsync(dbContext, UserId, DeckId);
            }
        }
        #endregion
    }
}

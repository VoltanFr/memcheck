using MemCheck.Application.QueryValidation;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Heaping
{
    //This is used during learning: a card either moves up one heap or down to the unknown heap
    //Manual moves are implemented by the sister class MoveCardsToHeap
    public sealed class MoveCardToHeap : RequestRunner<MoveCardToHeap.Request, MoveCardToHeap.Result>
    {
        #region Fields
        private readonly DateTime? runDate;
        #endregion
        public MoveCardToHeap(CallContext callContext, DateTime? runDate = null) : base(callContext)
        {
            this.runDate = runDate;
        }
        protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
        {
            var card = await DbContext.CardsInDecks.SingleAsync(card => card.DeckId.Equals(request.DeckId) && card.CardId.Equals(request.CardId));

            var lastLearnUtcTime = runDate == null ? DateTime.UtcNow : runDate;

            if (request.TargetHeap != CardInDeck.UnknownHeap)
            {
                if (request.TargetHeap == card.CurrentHeap)
                    return new ResultWithMetrologyProperties<Result>(new Result(), ("DeckId", request.DeckId.ToString()), ("CardId", request.CardId.ToString()), IntMetric("TargetHeap", request.TargetHeap), ("CardWasAlreadyInHeap", true.ToString())); //This could happen due to connection problems on client side, or to multiple sessions

                if (request.TargetHeap < card.CurrentHeap || request.TargetHeap - card.CurrentHeap > 1)
                    throw new InvalidOperationException($"Invalid move request (request heap: {request.TargetHeap}, current heap: {card.CurrentHeap}, card: {request.CardId}, last learn UTC time: {card.LastLearnUtcTime})");

                var heapingAlgorithm = await HeapingAlgorithm.OfDeckAsync(DbContext, request.DeckId);
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

            await DbContext.SaveChangesAsync();
            return new ResultWithMetrologyProperties<Result>(new Result(),
                ("DeckId", request.DeckId.ToString()),
                ("CardId", request.CardId.ToString()),
                IntMetric("TargetHeap", request.TargetHeap),
                ("CardWasAlreadyInHeap", false.ToString()));
        }
        #region Request & result
        public sealed record Request(Guid UserId, Guid DeckId, Guid CardId, int TargetHeap) : IRequest
        {
            public async Task CheckValidityAsync(CallContext callContext)
            {
                QueryValidationHelper.CheckNotReservedGuid(DeckId);
                QueryValidationHelper.CheckNotReservedGuid(CardId);
                if (TargetHeap < CardInDeck.UnknownHeap || TargetHeap > CardInDeck.MaxHeapValue)
                    throw new InvalidOperationException($"Invalid target heap {TargetHeap}");
                await QueryValidationHelper.CheckUserIsOwnerOfDeckAsync(callContext.DbContext, UserId, DeckId);
            }
        }
        public sealed record Result();
        #endregion
    }
}

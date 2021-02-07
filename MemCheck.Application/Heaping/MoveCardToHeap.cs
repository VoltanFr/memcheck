using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Heaping
{
    public sealed class MoveCardToHeap
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public MoveCardToHeap(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task RunAsync(Request request, DateTime? lastLearnUtcTime = null)
        {
            await request.CheckValidityAsync(dbContext);

            var card = await dbContext.CardsInDecks.SingleAsync(card => card.DeckId.Equals(request.DeckId) && card.CardId.Equals(request.CardId));

            if (request.TargetHeap != CardInDeck.UnknownHeap)
            {
                if (request.TargetHeap == card.CurrentHeap)
                    return; //This could happen due to connection problems on client side, or to multiple sessions
                if (!request.ManualMove && (request.TargetHeap < card.CurrentHeap || request.TargetHeap - card.CurrentHeap > 1))
                    throw new InvalidOperationException($"Invalid move request (request heap: {request.TargetHeap}, current heap: {card.CurrentHeap}, card: {request.CardId}, last learn UTC time: {card.LastLearnUtcTime})");
            }

            if (!request.ManualMove)
            {
                lastLearnUtcTime ??= DateTime.UtcNow;
                if (request.TargetHeap != CardInDeck.UnknownHeap)
                {
                    var heapingAlgorithm = await HeapingAlgorithm.OfDeckAsync(dbContext, request.DeckId);
                    card.ExpiryUtcTime = heapingAlgorithm.ExpiryUtcDate(request.TargetHeap, lastLearnUtcTime.Value);
                }
                card.LastLearnUtcTime = lastLearnUtcTime.Value;
            }

            if (request.TargetHeap == CardInDeck.UnknownHeap)
                card.ExpiryUtcTime = DateTime.MinValue.ToUniversalTime(); //Setting this is useless for normal operations, but would help detect any misuse of this field (bugs)

            if (card.CurrentHeap != CardInDeck.UnknownHeap && request.TargetHeap == CardInDeck.UnknownHeap)
                card.NbTimesInNotLearnedHeap++;

            card.CurrentHeap = request.TargetHeap;

            if (card.CurrentHeap > card.BiggestHeapReached)
                card.BiggestHeapReached = card.CurrentHeap;

            await dbContext.SaveChangesAsync();
        }
        #region Request
        public sealed record Request(Guid UserId, Guid DeckId, Guid CardId, int TargetHeap, bool ManualMove)
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

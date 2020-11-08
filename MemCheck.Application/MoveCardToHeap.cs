using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application
{
    public sealed class MoveCardToHeap
    {
        public const int MaxTargetHeapId = 15;
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public MoveCardToHeap(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task RunAsync(Request request)
        {
            await request.CheckValidityAsync(dbContext);

            var card = await dbContext.CardsInDecks.SingleAsync(card => card.DeckId.Equals(request.DeckId) && card.CardId.Equals(request.CardId));

            if (request.TargetHeap != 0)
            {
                if (request.TargetHeap == card.CurrentHeap)
                    return; //This happens due to connection problems on client side
                if (!request.ManualMove && (request.TargetHeap < card.CurrentHeap || request.TargetHeap - card.CurrentHeap > 1))
                    throw new RequestInputException($"Invalid move request (request heap: {request.TargetHeap}, current heap: {card.CurrentHeap}, card: {request.CardId}, last learn UTC time: {card.LastLearnUtcTime})");
            }

            if (!request.ManualMove)
                card.LastLearnUtcTime = DateTime.UtcNow;

            card.CurrentHeap = request.TargetHeap;
            if (request.TargetHeap == 0)
                card.NbTimesInNotLearnedHeap++;
            if (card.CurrentHeap > card.BiggestHeapReached)
                card.BiggestHeapReached = card.CurrentHeap;
            await dbContext.SaveChangesAsync();
        }
        #region Request
        public sealed class Request
        {
            #region Fields
            private const int minHeapId = 0;
            #endregion
            public Request(Guid userId, Guid deckId, Guid cardId, int targetHeap, bool manualMove)
            {
                UserId = userId;
                DeckId = deckId;
                CardId = cardId;
                TargetHeap = targetHeap;
                ManualMove = manualMove;
            }
            public Guid UserId { get; }
            public Guid DeckId { get; }
            public Guid CardId { get; }
            public int TargetHeap { get; }
            public bool ManualMove { get; }
            public async Task CheckValidityAsync(MemCheckDbContext dbContext)
            {
                if (QueryValidationHelper.IsReservedGuid(DeckId))
                    throw new RequestInputException($"Invalid DeckId '{DeckId}'");
                if (QueryValidationHelper.IsReservedGuid(CardId))
                    throw new RequestInputException($"Invalid DeckId '{DeckId}'");
                if (TargetHeap < minHeapId || TargetHeap > MaxTargetHeapId)
                    throw new RequestInputException($"Invalid target heap {TargetHeap}");
                await QueryValidationHelper.CheckUserIsOwnerOfDeckAsync(dbContext, UserId, DeckId);
            }
        }
        #endregion
    }
}

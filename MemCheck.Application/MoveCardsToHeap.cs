using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application
{
    public sealed class MoveCardsToHeap
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public MoveCardsToHeap(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task RunAsync(Request request)
        {
            request.CheckValidity();
            QueryValidationHelper.CheckUserIsOwnerOfDeck(dbContext, request.UserId, request.DeckId);

            var cards = dbContext.CardsInDecks.Where(card => card.DeckId.Equals(request.DeckId) && request.CardIds.Any(cardId => cardId == card.CardId));

            await cards.ForEachAsync(card => { if (card.BiggestHeapReached < request.TargetHeapId) card.BiggestHeapReached = request.TargetHeapId; });

            await cards.ForEachAsync(card => card.CurrentHeap = request.TargetHeapId);

            if (request.TargetHeapId == 0)
                await cards.ForEachAsync(card => card.NbTimesInNotLearnedHeap++);

            await dbContext.SaveChangesAsync();
        }
        public sealed class Request
        {
            public Request(Guid userId, Guid deckId, int targetHeapId, IEnumerable<Guid> cardIds)
            {
                UserId = userId;
                DeckId = deckId;
                TargetHeapId = targetHeapId;
                CardIds = cardIds;
            }

            public Guid UserId { get; }
            public Guid DeckId { get; }
            public int TargetHeapId { get; }
            public IEnumerable<Guid> CardIds { get; }
            public void CheckValidity()
            {
                if (QueryValidationHelper.IsReservedGuid(DeckId))
                    throw new RequestInputException($"Invalid DeckId '{DeckId}'");
                if (TargetHeapId < 0 || TargetHeapId > CardInDeck.MaxHeapValue)
                    throw new RequestInputException($"Invalid target heap {TargetHeapId}");
                if (CardIds.Any(cardId => QueryValidationHelper.IsReservedGuid(cardId)))
                    throw new RequestInputException($"Invalid card id");
            }
        }
    }
}

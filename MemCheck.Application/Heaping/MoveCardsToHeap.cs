﻿using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Heaping
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
            await request.CheckValidityAsync(dbContext);

            var cardsInDecks = dbContext.CardsInDecks.Where(card => card.DeckId.Equals(request.DeckId) && request.CardIds.Any(cardId => cardId == card.CardId)).ToImmutableDictionary(c => c.CardId, c => c);

            if (request.CardIds.Any(cardId => !cardsInDecks.ContainsKey(cardId)))
                throw new InvalidOperationException("One card is not in the deck");

            foreach (var cardInDeck in cardsInDecks.Values)
            {
                if (cardInDeck.BiggestHeapReached < request.TargetHeap)
                    cardInDeck.BiggestHeapReached = request.TargetHeap;
                if (request.TargetHeap == 0 && cardInDeck.CurrentHeap != 0)
                    cardInDeck.NbTimesInNotLearnedHeap++;
                cardInDeck.CurrentHeap = request.TargetHeap;
            }

            await dbContext.SaveChangesAsync();
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
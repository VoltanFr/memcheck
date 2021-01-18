using MemCheck.Database;
using MemCheck.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MemCheck.Application
{
    public sealed class AddCardsInDeck
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public AddCardsInDeck(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public void Run(Guid deckId, IEnumerable<Guid> cardIds)
        {
            var cardsInDeck = dbContext.CardsInDecks.Where(cardInDeck => cardInDeck.DeckId == deckId).Select(cardInDeck => cardInDeck.CardId);

            var toAdd = cardIds.Where(cardId => !cardsInDeck.Contains(cardId)).Select(cardId => new CardInDeck()
            {
                CardId = cardId,
                DeckId = deckId,
                CurrentHeap = 0,
                LastLearnUtcTime = DateTime.MinValue.ToUniversalTime(),
                AddToDeckUtcTime = DateTime.UtcNow,
                NbTimesInNotLearnedHeap = 1,
                BiggestHeapReached = 0
            });
            dbContext.CardsInDecks.AddRange(toAdd);
            dbContext.SaveChanges();
        }
    }
}

using MemCheck.Database;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MemCheck.Application
{
    public sealed class GetHeapsOfDeck
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public GetHeapsOfDeck(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public IEnumerable<int> Run(Guid deckId)
        {
            var cardsInDeck = dbContext.CardsInDecks.Where(cardInDeck => cardInDeck.DeckId == deckId);
            var heaps = cardsInDeck.Select(cardInDeck => cardInDeck.CurrentHeap).Distinct();
            return heaps;
        }
    }
}

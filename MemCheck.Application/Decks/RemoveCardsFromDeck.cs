using MemCheck.Database;
using MemCheck.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MemCheck.Application.Decks
{
    public sealed class RemoveCardsFromDeck
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public RemoveCardsFromDeck(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public void Run(Guid deckId, IEnumerable<Guid> cardIds)
        {
            //By design, this ignores cards not in the deck
            foreach (var cardId in cardIds)
                if (dbContext.CardsInDecks.Where(cardInDeck => cardInDeck.DeckId == deckId && cardInDeck.CardId == cardId).Any())
                    dbContext.CardsInDecks.Remove(new CardInDeck() { CardId = cardId, DeckId = deckId });
            dbContext.SaveChanges();
        }
    }
}

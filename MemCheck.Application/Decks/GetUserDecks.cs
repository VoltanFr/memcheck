using MemCheck.Database;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MemCheck.Application.Decks
{
    public sealed class GetUserDecks
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public GetUserDecks(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public IEnumerable<ViewModel> Run(Guid userId)
        {
            var decks = dbContext.Decks.Where(deck => deck.Owner.Id == userId).OrderBy(deck => deck.Description);
            return decks.Select(deck => new ViewModel(deck.Id, deck.Description, deck.HeapingAlgorithmId, deck.CardInDecks.Count));
        }
        public sealed class ViewModel
        {
            #region Fields
            private Guid deckId;
            private readonly string description;
            private readonly int heapingAlgorithmId;
            private readonly int cardCount;
            #endregion
            public ViewModel(Guid deckId, string description, int heapingAlgorithmId, int cardCount)
            {
                this.deckId = deckId;
                this.description = description;
                this.heapingAlgorithmId = heapingAlgorithmId;
                this.cardCount = cardCount;
            }
            public string Description => description;
            public Guid DeckId => deckId;
            public int HeapingAlgorithmId => heapingAlgorithmId;
            public int CardCount => cardCount;
        }
    }
}

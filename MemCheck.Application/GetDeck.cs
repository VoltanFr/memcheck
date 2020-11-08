using MemCheck.Database;
using System;
using System.Linq;

namespace MemCheck.Application
{
    public sealed class GetDeck
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public GetDeck(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public ViewModel Run(Guid deckId)
        {
            var deck = dbContext.Decks.Where(deck => deck.Id == deckId).Single();
            var cardCount = dbContext.Decks.Where(deck => deck.Id == deckId).Select(deck => deck.CardInDecks.Count).Single();
            return new ViewModel(deck.Id, deck.Description, deck.HeapingAlgorithmId, cardCount);
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

using MemCheck.Database;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MemCheck.Application
{
    public sealed class GetCardsNotInDeck
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public GetCardsNotInDeck(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public IEnumerable<ViewModel> Run(Guid deckId)
        {
            //Nasty implementation: we load all the cards of current deck into memory, which can be very bad
            var cardsInCurrentDeck = dbContext.CardsInDecks.Where(card => card.DeckId.Equals(deckId));
            var cardIdsInCurrentDeck = cardsInCurrentDeck.Select(card => card.CardId).ToHashSet();
            var result = dbContext.Cards.Where(card => !cardIdsInCurrentDeck.Contains(card.Id));
            return result.Select(card => new ViewModel() { Id = card.Id, FrontSide = card.FrontSide, BackSide = card.BackSide });
        }
        public sealed class ViewModel
        {
            public Guid Id { get; set; }
            public string FrontSide { get; set; } = null!;
            public string BackSide { get; set; } = null!;
        }
    }
}

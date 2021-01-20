using MemCheck.Database;
using MemCheck.Domain;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application
{
    public sealed class AddCardInDeck
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public AddCardInDeck(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task<GetCardsInDeck.ViewModel> RunAsync(Guid deckId, Guid cardId)
        {
            var card = dbContext.Cards.Single(card => card.Id.Equals(cardId));
            var cardForUser = new CardInDeck()
            {
                CardId = card.Id,
                Card = card,
                DeckId = deckId,
                CurrentHeap = 0,
                LastLearnUtcTime = CardInDeck.NeverLearntLastLearnTime,
                AddToDeckUtcTime = DateTime.UtcNow,
                NbTimesInNotLearnedHeap = 1,
                BiggestHeapReached = 0
            };
            dbContext.CardsInDecks.Add(cardForUser);
            await dbContext.SaveChangesAsync();
            return new GetCardsInDeck.ViewModel(cardForUser.Card.Id, cardForUser.CurrentHeap, cardForUser.LastLearnUtcTime,
                cardForUser.BiggestHeapReached, cardForUser.NbTimesInNotLearnedHeap, cardForUser.Card.FrontSide, cardForUser.Card.BackSide);
        }
    }
}

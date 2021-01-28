using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Decks
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
        public async Task RunAsync(Request request)
        {
            await request.CheckValidityAsync(dbContext);

            var cardsInDeck = dbContext.CardsInDecks.Where(cardInDeck => cardInDeck.DeckId == request.DeckId).Select(cardInDeck => cardInDeck.CardId);

            var toAdd = request.CardIds.Where(cardId => !cardsInDeck.Contains(cardId)).Select(cardId => new CardInDeck()
            {
                CardId = cardId,
                DeckId = request.DeckId,
                CurrentHeap = 0,
                LastLearnUtcTime = CardInDeck.NeverLearntLastLearnTime,
                AddToDeckUtcTime = DateTime.UtcNow,
                NbTimesInNotLearnedHeap = 1,
                BiggestHeapReached = 0
            });
            dbContext.CardsInDecks.AddRange(toAdd);
            dbContext.SaveChanges();
        }
        #region Request type
        public sealed record Request(Guid UserId, Guid DeckId, params Guid[] CardIds)
        {
            public async Task CheckValidityAsync(MemCheckDbContext dbContext)
            {
                if (QueryValidationHelper.IsReservedGuid(UserId))
                    throw new InvalidOperationException("Invalid user ID");

                await QueryValidationHelper.CheckUserIsOwnerOfDeckAsync(dbContext, UserId, DeckId);
                CardVisibilityHelper.CheckUserIsAllowedToViewCards(dbContext, UserId, CardIds);
            }
        }
        #endregion
    }
}

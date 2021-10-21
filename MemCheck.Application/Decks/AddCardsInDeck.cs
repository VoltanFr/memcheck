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
        private readonly CallContext callContext;
        #endregion
        public AddCardsInDeck(CallContext callContext)
        {
            this.callContext = callContext;
        }
        public async Task RunAsync(Request request)
        {
            await request.CheckValidityAsync(callContext.DbContext);

            var cardsInDeck = callContext.DbContext.CardsInDecks.Where(cardInDeck => cardInDeck.DeckId == request.DeckId).Select(cardInDeck => cardInDeck.CardId);

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
            callContext.DbContext.CardsInDecks.AddRange(toAdd);
            callContext.DbContext.SaveChanges();
            callContext.TelemetryClient.TrackEvent("AddCardsInDeck", ("DeckId", request.DeckId.ToString()), ("CardCount", request.CardIds.Length.ToString()));
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

using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Decks
{
    //This class ignores the cards in the request which are not in the deck
    //Because some cards could have been removed in another session
    public sealed class RemoveCardsFromDeck
    {
        #region Fields
        private readonly CallContext callContext;
        #endregion
        public RemoveCardsFromDeck(CallContext callContext)
        {
            this.callContext = callContext;
        }
        public async Task RunAsync(Request request)
        {
            await request.CheckValidityAsync(callContext.DbContext);

            var existing = request.CardIds
                .Where(cardId => callContext.DbContext.CardsInDecks.Any(cardInDeck => cardInDeck.DeckId == request.DeckId && cardInDeck.CardId == cardId))
                .Select(cardId => new CardInDeck() { CardId = cardId, DeckId = request.DeckId });
            callContext.DbContext.CardsInDecks.RemoveRange(existing);
            callContext.TelemetryClient.TrackEvent("RemoveCardsFromDeck", ("DeckId", request.DeckId.ToString()), ("CardCount", request.CardIds.Count().ToString()));
            callContext.DbContext.SaveChanges();
        }
        #region Request type
        public sealed record Request(Guid CurrentUserId, Guid DeckId, IEnumerable<Guid> CardIds)
        {
            public async Task CheckValidityAsync(MemCheckDbContext dbContext)
            {
                QueryValidationHelper.CheckNotReservedGuid(CurrentUserId);
                QueryValidationHelper.CheckNotReservedGuid(DeckId);
                QueryValidationHelper.CheckContainsNoReservedGuid(CardIds);
                await QueryValidationHelper.CheckUserIsOwnerOfDeckAsync(dbContext, CurrentUserId, DeckId);
            }
        }
        #endregion
    }
}

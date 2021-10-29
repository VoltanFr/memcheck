using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Decks
{
    public sealed class RemoveCardFromDeck
    {
        #region Fields
        private readonly CallContext callContext;
        #endregion
        public RemoveCardFromDeck(CallContext callContext)
        {
            this.callContext = callContext;
        }
        public async Task<Result> RunAsync(Request request)
        {
            await request.CheckValidityAsync(callContext.DbContext);

            var card = callContext.DbContext.CardsInDecks
                .Where(cardInDeck => cardInDeck.CardId == request.CardId && cardInDeck.DeckId == request.DeckId)
                .Include(cardInDeck => cardInDeck.Card)
                .Include(cardInDeck => cardInDeck.Deck)
                .Single();
            callContext.DbContext.CardsInDecks.Remove(card);
            await callContext.DbContext.SaveChangesAsync();
            var result = new Result(card.Card.FrontSide, card.Deck.Description);
            callContext.TelemetryClient.TrackEvent("RemoveCardFromDeck", ("DeckId", request.DeckId.ToString()), ("CardId", request.CardId.ToString()));
            return result;
        }
        #region Request class
        public sealed class Request
        {
            public Request(Guid currentUserId, Guid deckId, Guid cardId)
            {
                CurrentUserId = currentUserId;
                DeckId = deckId;
                CardId = cardId;
            }
            public Guid CurrentUserId { get; }
            public Guid DeckId { get; }
            public Guid CardId { get; }
            public async Task CheckValidityAsync(MemCheckDbContext dbContext)
            {
                QueryValidationHelper.CheckNotReservedGuid(CurrentUserId);
                QueryValidationHelper.CheckNotReservedGuid(DeckId);
                QueryValidationHelper.CheckNotReservedGuid(CardId);
                await QueryValidationHelper.CheckUserIsOwnerOfDeckAsync(dbContext, CurrentUserId, DeckId);
            }
        }
        public sealed class Result
        {
            public Result(string frontSideText, string deckName)
            {
                FrontSideText = frontSideText;
                DeckName = deckName;
            }
            public string FrontSideText { get; }
            public string DeckName { get; }
        }
        #endregion
    }
}

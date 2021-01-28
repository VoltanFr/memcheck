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
        private readonly MemCheckDbContext dbContext;
        #endregion
        public RemoveCardFromDeck(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task<Result> RunAsync(Request request)
        {
            await request.CheckValidityAsync(dbContext);

            var card = dbContext.CardsInDecks
                .Where(cardInDeck => cardInDeck.CardId == request.CardId && cardInDeck.DeckId == request.DeckId)
                .Include(cardInDeck => cardInDeck.Card)
                .Include(cardInDeck => cardInDeck.Deck)
                .Single();
            dbContext.CardsInDecks.Remove(card);
            await dbContext.SaveChangesAsync();

            return new Result(card.Card.FrontSide, card.Deck.Description);
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

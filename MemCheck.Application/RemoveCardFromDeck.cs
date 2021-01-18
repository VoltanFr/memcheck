using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application
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
            request.CheckValidity();
            QueryValidationHelper.CheckUserIsOwnerOfDeck(dbContext, request.CurrentUserId, request.DeckId);

            var cardsInDeck = dbContext.CardsInDecks.Where(cardInDeck => cardInDeck.CardId == request.CardId && cardInDeck.DeckId == request.DeckId);
            if (cardsInDeck.Count() != 1)
                throw new RequestInputException($"There are {cardsInDeck.Count()} cards with the given id");

            var cardInDeck = cardsInDeck.Include(cardInDeck => cardInDeck.Card).Include(cardInDeck => cardInDeck.Deck).Single();

            dbContext.CardsInDecks.Remove(cardInDeck);
            await dbContext.SaveChangesAsync();

            return new Result(cardInDeck.Card.FrontSide, cardInDeck.Deck.Description);
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
            public void CheckValidity()
            {
                if (QueryValidationHelper.IsReservedGuid(CurrentUserId))
                    throw new RequestInputException($"Invalid user id '{CurrentUserId}'");
                if (QueryValidationHelper.IsReservedGuid(DeckId))
                    throw new RequestInputException($"Invalid deck id '{DeckId}'");
                if (QueryValidationHelper.IsReservedGuid(CardId))
                    throw new RequestInputException($"Invalid card id '{DeckId}'");
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

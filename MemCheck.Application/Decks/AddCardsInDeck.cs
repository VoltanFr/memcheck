using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Decks
{
    public sealed class AddCardsInDeck : RequestRunner<AddCardsInDeck.Request, AddCardsInDeck.Result>
    {
        public AddCardsInDeck(CallContext callContext) : base(callContext)
        {
        }
        protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
        {
            var cardsInDeck = DbContext.CardsInDecks.Where(cardInDeck => cardInDeck.DeckId == request.DeckId).Select(cardInDeck => cardInDeck.CardId);

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
            await DbContext.CardsInDecks.AddRangeAsync(toAdd);
            DbContext.SaveChanges();

            return new ResultWithMetrologyProperties<Result>(new Result(), ("DeckId", request.DeckId.ToString()), ("CardCount", request.CardIds.Length.ToString()));
        }
        #region Request & Result
        public sealed record Request(Guid UserId, Guid DeckId, params Guid[] CardIds) : IRequest
        {
            public async Task CheckValidityAsync(CallContext callContext)
            {
                if (QueryValidationHelper.IsReservedGuid(UserId))
                    throw new InvalidOperationException("Invalid user ID");

                await QueryValidationHelper.CheckUserIsOwnerOfDeckAsync(callContext.DbContext, UserId, DeckId);
                CardVisibilityHelper.CheckUserIsAllowedToViewCards(callContext.DbContext, UserId, CardIds);
            }
        }
        public sealed record Result();
        #endregion
    }
}

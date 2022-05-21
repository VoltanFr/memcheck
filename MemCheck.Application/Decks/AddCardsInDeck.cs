using MemCheck.Application.QueryValidation;
using MemCheck.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Decks;

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

        return new ResultWithMetrologyProperties<Result>(new Result(), ("DeckId", request.DeckId.ToString()), IntMetric("CardCount", request.CardIds.Count()));
    }
    #region Request & Result
    public sealed record Request : IRequest
    {
        private readonly Guid[] cardIds;
        public Request(Guid userId, Guid deckId, params Guid[] cardIds)
        {
            UserId = userId;
            DeckId = deckId;
            this.cardIds = cardIds;
        }

        public async Task CheckValidityAsync(CallContext callContext)
        {
            if (QueryValidationHelper.IsReservedGuid(UserId))
                throw new InvalidOperationException("Invalid user ID");
            await QueryValidationHelper.CheckUserIsOwnerOfDeckAsync(callContext.DbContext, UserId, DeckId);
            CardVisibilityHelper.CheckUserIsAllowedToViewCards(callContext.DbContext, UserId, cardIds);
        }
        public IEnumerable<Guid> CardIds => cardIds;

        public Guid UserId { get; }
        public Guid DeckId { get; }
    }
    public sealed record Result();
    #endregion
}

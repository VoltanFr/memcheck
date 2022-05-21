using MemCheck.Application.QueryValidation;
using MemCheck.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Decks;

//This class ignores the cards in the request which are not in the deck
//Because some cards could have been removed in another session
public sealed class RemoveCardsFromDeck : RequestRunner<RemoveCardsFromDeck.Request, RemoveCardsFromDeck.Result>
{
    public RemoveCardsFromDeck(CallContext callContext) : base(callContext)
    {
    }
    protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
    {
        var existing = request.CardIds
            .Where(cardId => DbContext.CardsInDecks.Any(cardInDeck => cardInDeck.DeckId == request.DeckId && cardInDeck.CardId == cardId))
            .Select(cardId => new CardInDeck() { CardId = cardId, DeckId = request.DeckId });
        DbContext.CardsInDecks.RemoveRange(existing);
        await DbContext.SaveChangesAsync();
        return new ResultWithMetrologyProperties<Result>(new Result(), ("DeckId", request.DeckId.ToString()), IntMetric("CardCount", request.CardIds.Count()));
    }
    #region Request type
    public sealed record Request(Guid CurrentUserId, Guid DeckId, IEnumerable<Guid> CardIds) : IRequest
    {
        public async Task CheckValidityAsync(CallContext callContext)
        {
            QueryValidationHelper.CheckNotReservedGuid(CurrentUserId);
            QueryValidationHelper.CheckNotReservedGuid(DeckId);
            QueryValidationHelper.CheckContainsNoReservedGuid(CardIds);
            await QueryValidationHelper.CheckUserIsOwnerOfDeckAsync(callContext.DbContext, CurrentUserId, DeckId);
        }
    }
    public sealed record Result();
    #endregion
}

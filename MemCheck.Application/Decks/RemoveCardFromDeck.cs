using MemCheck.Application.QueryValidation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Decks;

public sealed class RemoveCardFromDeck : RequestRunner<RemoveCardFromDeck.Request, RemoveCardFromDeck.Result>
{
    public RemoveCardFromDeck(CallContext callContext) : base(callContext)
    {
    }
    protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
    {
        var card = DbContext.CardsInDecks
            .Where(cardInDeck => cardInDeck.CardId == request.CardId && cardInDeck.DeckId == request.DeckId)
            .Include(cardInDeck => cardInDeck.Card)
            .Include(cardInDeck => cardInDeck.Deck)
            .Single();
        DbContext.CardsInDecks.Remove(card);
        await DbContext.SaveChangesAsync();
        return new ResultWithMetrologyProperties<Result>(new Result(card.Card.FrontSide, card.Deck.Description), ("DeckId", request.DeckId.ToString()), ("CardId", request.CardId.ToString()));
    }
    #region Request class
    public sealed class Request : IRequest
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
        public async Task CheckValidityAsync(CallContext callContext)
        {
            QueryValidationHelper.CheckNotReservedGuid(CurrentUserId);
            QueryValidationHelper.CheckNotReservedGuid(DeckId);
            QueryValidationHelper.CheckNotReservedGuid(CardId);
            await QueryValidationHelper.CheckUserIsOwnerOfDeckAsync(callContext.DbContext, CurrentUserId, DeckId);
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

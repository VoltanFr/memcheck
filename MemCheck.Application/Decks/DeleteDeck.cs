using MemCheck.Application.QueryValidation;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Decks;

public sealed class DeleteDeck : RequestRunner<DeleteDeck.Request, DeleteDeck.Result>
{
    public DeleteDeck(CallContext callContext) : base(callContext)
    {
    }
    protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
    {
        var deck = DbContext.Decks.Where(deck => deck.Id == request.DeckId).Single();
        DbContext.Decks.Remove(deck);
        await DbContext.SaveChangesAsync();
        return new ResultWithMetrologyProperties<Result>(new Result());
    }
    #region Request type
    public sealed record Request(Guid UserId, Guid DeckId) : IRequest
    {
        public async Task CheckValidityAsync(CallContext callContext)
        {
            await QueryValidationHelper.CheckUserIsOwnerOfDeckAsync(callContext.DbContext, UserId, DeckId);
        }
    }
    public sealed record Result();
    #endregion
}

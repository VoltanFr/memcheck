using MemCheck.Application.QueryValidation;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Decks;

public sealed class UpdateDeck : RequestRunner<UpdateDeck.Request, UpdateDeck.Result>
{
    public UpdateDeck(CallContext callContext) : base(callContext)
    {
    }
    protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
    {
        var deck = DbContext.Decks.Where(deck => deck.Id == request.DeckId).Single();
        deck.Description = request.Name;
        deck.HeapingAlgorithmId = request.HeapingAlgorithmId;
        await DbContext.SaveChangesAsync();
        return new ResultWithMetrologyProperties<Result>(new Result(), ("DeckId", request.DeckId.ToString()), ("Name", request.Name), IntMetric("NameLength", request.Name.Length), IntMetric("HeapingAlgorithmId", request.HeapingAlgorithmId));
    }
    #region Request type
    public sealed record Request(Guid UserId, Guid DeckId, string Name, int HeapingAlgorithmId) : IRequest
    {
        public async Task CheckValidityAsync(CallContext callContext)
        {
            await QueryValidationHelper.CheckCanCreateDeckAsync(UserId, Name, HeapingAlgorithmId, callContext.DbContext, callContext.Localized);
            await QueryValidationHelper.CheckUserIsOwnerOfDeckAsync(callContext.DbContext, UserId, DeckId);
        }
    }
    public sealed record Result();
    #endregion
}

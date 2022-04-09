using MemCheck.Application.QueryValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Decks
{
    public sealed class GetUserDecks : RequestRunner<GetUserDecks.Request, IEnumerable<GetUserDecks.Result>>
    {
        public GetUserDecks(CallContext callContext) : base(callContext)
        {
        }
        protected override async Task<ResultWithMetrologyProperties<IEnumerable<Result>>> DoRunAsync(Request request)
        {
            var decks = DbContext.Decks.Where(deck => deck.Owner.Id == request.UserId).OrderBy(deck => deck.Description);
            var results = decks.Select(deck => new Result(deck.Id, deck.Description, deck.HeapingAlgorithmId, deck.CardInDecks.Count));
            await Task.CompletedTask;
            return new ResultWithMetrologyProperties<IEnumerable<Result>>(results, IntMetric("DeckCount", results.Count()));
        }
        #region Request & Result
        public sealed record Request(Guid UserId) : IRequest
        {
            public async Task CheckValidityAsync(CallContext callContext)
            {
                await QueryValidationHelper.CheckUserExistsAsync(callContext.DbContext, UserId);
            }
        }
        public sealed record Result(Guid DeckId, string Description, int HeapingAlgorithmId, int CardCount);
        #endregion

    }
}

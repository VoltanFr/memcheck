using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Decks
{
    public sealed class GetUserDecks
    {
        #region Fields
        private readonly CallContext callContext;
        #endregion
        public GetUserDecks(CallContext callContext)
        {
            this.callContext = callContext;
        }
        public async Task<IEnumerable<Result>> RunAsync(Request request)
        {
            await request.CheckValidityAsync(callContext.DbContext);
            var decks = callContext.DbContext.Decks.Where(deck => deck.Owner.Id == request.UserId).OrderBy(deck => deck.Description);
            var results = decks.Select(deck => new Result(deck.Id, deck.Description, deck.HeapingAlgorithmId, deck.CardInDecks.Count));
            callContext.TelemetryClient.TrackEvent("GetUserDecks", ("DeckCount", results.Count().ToString()));
            return results;
        }
        #region Request & Result
        public sealed record Request(Guid UserId)
        {
            public async Task CheckValidityAsync(MemCheckDbContext dbContext)
            {
                await QueryValidationHelper.CheckUserExistsAsync(dbContext, UserId);
            }
        }
        public sealed record Result(Guid DeckId, string Description, int HeapingAlgorithmId, int CardCount);
        #endregion

    }
}

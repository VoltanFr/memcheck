using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Decks
{
    public sealed class DeleteDeck
    {
        #region Fields
        private readonly CallContext callContext;
        #endregion
        public DeleteDeck(CallContext callContext)
        {
            this.callContext = callContext;
        }
        public async Task RunAsync(Request request)
        {
            await request.CheckValidityAsync(callContext.DbContext);
            var deck = callContext.DbContext.Decks.Where(deck => deck.Id == request.DeckId).Single();
            callContext.DbContext.Decks.Remove(deck);
            await callContext.DbContext.SaveChangesAsync();
            callContext.TelemetryClient.TrackEvent("DeleteDeck");
        }
        #region Request type
        public sealed record Request(Guid UserId, Guid DeckId)
        {
            public async Task CheckValidityAsync(MemCheckDbContext dbContext)
            {
                await QueryValidationHelper.CheckUserIsOwnerOfDeckAsync(dbContext, UserId, DeckId);
            }
        }
        #endregion
    }
}

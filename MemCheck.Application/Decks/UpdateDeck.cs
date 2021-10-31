using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Decks
{
    public sealed class UpdateDeck
    {
        #region Fields
        private readonly CallContext callContext;
        #endregion
        public UpdateDeck(CallContext callContext)
        {
            this.callContext = callContext;
        }
        public async Task<bool> RunAsync(Request request, ILocalized localizer)
        {
            await request.CheckValidityAsync(localizer, callContext.DbContext);
            var deck = callContext.DbContext.Decks.Where(deck => deck.Id == request.DeckId).Single();
            deck.Description = request.Name;
            deck.HeapingAlgorithmId = request.HeapingAlgorithmId;
            await callContext.DbContext.SaveChangesAsync();
            callContext.TelemetryClient.TrackEvent("UpdateDeck", ("DeckId", request.DeckId.ToString()), ("Name", request.Name), ("NameLength", request.Name.Length.ToString()), ("HeapingAlgorithmId", request.HeapingAlgorithmId.ToString()));
            return true;
        }
        #region Request type
        public sealed record Request(Guid UserId, Guid DeckId, string Name, int HeapingAlgorithmId)
        {
            public async Task CheckValidityAsync(ILocalized localizer, MemCheckDbContext dbContext)
            {
                await QueryValidationHelper.CheckCanCreateDeckAsync(UserId, Name, HeapingAlgorithmId, dbContext, localizer);
                await QueryValidationHelper.CheckUserIsOwnerOfDeckAsync(dbContext, UserId, DeckId);
            }
        }
        #endregion
    }
}

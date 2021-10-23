using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Decks
{
    public sealed class CreateDeck
    {
        #region Fields
        private readonly CallContext callContext;
        #endregion
        public CreateDeck(CallContext callContext)
        {
            this.callContext = callContext;
        }
        public async Task RunAsync(Request request, ILocalized localizer)
        {
            await request.CheckValidityAsync(localizer, callContext.DbContext);
            var user = await callContext.DbContext.Users.SingleAsync(user => user.Id == request.UserId);
            var deck = new Deck() { Owner = user, Description = request.Name, HeapingAlgorithmId = request.HeapingAlgorithmId };
            callContext.DbContext.Decks.Add(deck);
            await callContext.DbContext.SaveChangesAsync();
            callContext.TelemetryClient.TrackEvent("CreateDeck");
        }
        #region Request type
        public sealed record Request(Guid UserId, string Name, int HeapingAlgorithmId)
        {
            public async Task CheckValidityAsync(ILocalized localizer, MemCheckDbContext dbContext)
            {
                await QueryValidationHelper.CheckCanCreateDeckAsync(UserId, Name, HeapingAlgorithmId, dbContext, localizer);
            }
        }
        #endregion

    }
}

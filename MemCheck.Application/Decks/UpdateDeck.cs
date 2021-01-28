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
        private readonly MemCheckDbContext dbContext;
        #endregion
        public UpdateDeck(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task<bool> RunAsync(Request request, ILocalized localizer)
        {
            await request.CheckValidityAsync(localizer, dbContext);
            var deck = dbContext.Decks.Where(deck => deck.Id == request.DeckId).Single();
            deck.Description = request.Name;
            deck.HeapingAlgorithmId = request.HeapingAlgorithmId;
            await dbContext.SaveChangesAsync();
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

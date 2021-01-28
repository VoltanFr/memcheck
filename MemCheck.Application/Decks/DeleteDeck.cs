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
        private readonly MemCheckDbContext dbContext;
        #endregion
        public DeleteDeck(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task RunAsync(Request request)
        {
            await request.CheckValidityAsync(dbContext);
            var deck = dbContext.Decks.Where(deck => deck.Id == request.DeckId).Single();
            dbContext.Decks.Remove(deck);
            await dbContext.SaveChangesAsync();
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

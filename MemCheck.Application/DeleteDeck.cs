using MemCheck.Database;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application
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
        public async Task<bool> RunAsync(Guid deckId)
        {
            var deck = dbContext.Decks.Where(deck => deck.Id == deckId).Single();
            dbContext.Decks.Remove(deck);
            await dbContext.SaveChangesAsync();
            return true;
        }
    }
}

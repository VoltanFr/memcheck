using MemCheck.Database;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application
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
        public async Task<bool> RunAsync(Request request)
        {
            var deck = dbContext.Decks.Where(deck => deck.Id == request.DeckId).Single();
            deck.Description = request.Description;
            deck.HeapingAlgorithmId = request.HeapingAlgorithmId;
            await dbContext.SaveChangesAsync();
            return true;
        }
        public sealed class Request
        {
            public Guid DeckId { get; set; }
            public string Description { get; set; } = null!;
            public int HeapingAlgorithmId { get; set; }
        }
    }
}

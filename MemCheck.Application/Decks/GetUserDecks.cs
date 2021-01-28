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
        private readonly MemCheckDbContext dbContext;
        #endregion
        public GetUserDecks(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task<IEnumerable<Result>> RunAsync(Request request)
        {
            await request.CheckValidityAsync(dbContext);
            var decks = dbContext.Decks.Where(deck => deck.Owner.Id == request.UserId).OrderBy(deck => deck.Description);
            return decks.Select(deck => new Result(deck.Id, deck.Description, deck.HeapingAlgorithmId, deck.CardInDecks.Count));
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

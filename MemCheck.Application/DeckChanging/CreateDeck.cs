using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.DeckChanging
{
    public sealed class CreateDeck
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public CreateDeck(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task RunAsync(Request request, ILocalized localizer)
        {
            await request.CheckValidityAsync(localizer, dbContext);
            var user = await dbContext.Users.SingleAsync(user => user.Id == request.UserId);
            var deck = new Deck() { Owner = user, Description = request.Name, HeapingAlgorithmId = request.HeapingAlgorithmId };
            dbContext.Decks.Add(deck);
            await dbContext.SaveChangesAsync();
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

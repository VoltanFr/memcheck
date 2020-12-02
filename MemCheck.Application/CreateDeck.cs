using System;
using System.Linq;
using System.Threading.Tasks;
using MemCheck.Application.Heaping;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace MemCheck.Application
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
        public async Task<Guid> RunAsync(Request request)
        {
            request.CheckValidity(dbContext);

            var deck = new Deck() { Owner = request.User, Description = request.Description, HeapingAlgorithmId = request.HeapingAlgorithmId };
            dbContext.Decks.Add(deck);
            await dbContext.SaveChangesAsync();
            return deck.Id;
        }
        public sealed class Request
        {
            #region Fields
            private const int minDescriptionLength = 1;
            private const int maxDescriptionLength = 36;
            #endregion
            public Request(MemCheckUser user, string description, int heapingAlgorithmId)
            {
                User = user;
                Description = description;
                HeapingAlgorithmId = heapingAlgorithmId;
            }
            public MemCheckUser User { get; }
            public string Description { get; }
            public int HeapingAlgorithmId { get; }
            public void CheckValidity(MemCheckDbContext dbContext)
            {
                if (QueryValidationHelper.IsReservedGuid(User.Id))
                    throw new RequestInputException("Invalid user");
                if (!HeapingAlgorithms.Instance.Ids.Any(algoId => algoId == HeapingAlgorithmId))
                    throw new RequestInputException($"Invalid algo id '{HeapingAlgorithmId}'");
                if (Description.Length < minDescriptionLength || Description.Length > maxDescriptionLength)
                    throw new InvalidOperationException($"Invalid description '{Description}' (length must be between {minDescriptionLength} and {maxDescriptionLength}, is {Description.Length})");
                if (dbContext.Decks.Where(deck => (deck.Owner.Id == User.Id) && EF.Functions.Like(deck.Description, $"{Description}")).Any())
                    throw new InvalidOperationException($"A deck with description '{Description}' already exists (this is case insensitive)");
            }
        }

    }
}

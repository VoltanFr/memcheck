using MemCheck.Application.Heaping;
using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.DeckChanging
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
        #region Request and result types
        public sealed record Request(Guid UserId, Guid DeckId, string Name, int HeapingAlgorithmId)
        {
            public const int MinNameLength = 3;
            public const int MaxNameLength = 1000;
            public async Task CheckValidityAsync(ILocalized localizer, MemCheckDbContext dbContext)
            {
                if (QueryValidationHelper.IsReservedGuid(UserId))
                    throw new InvalidOperationException("Invalid user ID");

                if (Name != Name.Trim())
                    throw new InvalidOperationException("Invalid Name: not trimmed");
                if (Name.Length < MinNameLength || Name.Length > MaxNameLength)
                    throw new RequestInputException(localizer.Get("InvalidNameLength") + $" {Name.Length}" + localizer.Get("MustBeBetween") + $" {MinNameLength} " + localizer.Get("And") + $" {MaxNameLength}");

                if (!HeapingAlgorithms.Instance.Ids.Contains(HeapingAlgorithmId))
                    throw new InvalidOperationException($"Invalid heaping algorithm: {HeapingAlgorithmId}");

                await QueryValidationHelper.CheckUserIsOwnerOfDeckAsync(dbContext, UserId, DeckId);
            }
        }
        #endregion
    }
}

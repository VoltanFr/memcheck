using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.QueryValidation
{
    internal static class QueryValidationHelper
    {
        #region Fields
        private static readonly ImmutableHashSet<Guid> reservedGuids = GetReservedGuids();
        #endregion
        #region Private methods
        private static ImmutableHashSet<Guid> GetReservedGuids()
        {
            var result = new HashSet<Guid> { Guid.Empty, new Guid("11111111-1111-1111-1111-111111111111") };
            return result.ToImmutableHashSet();
        }
        #endregion
        public static bool IsReservedGuid(Guid g)
        {
            return reservedGuids.Contains(g);
        }
        public static void CheckNotReservedGuid(Guid g)
        {
            if (IsReservedGuid(g))
                throw new RequestInputException("Bad Guid");
        }
        public static void CheckUserIsOwnerOfDeck(MemCheckDbContext dbContext, Guid userId, Guid deckId)
        {
            var owner = dbContext.Decks.AsNoTracking().Where(deck => deck.Id == deckId).Select(deck => deck.Owner.Id).Single();
            if (owner != userId)
                throw new RequestInputException("Current user not owner of deck");
        }
        public static async Task CheckUserIsOwnerOfDeckAsync(MemCheckDbContext dbContext, Guid userId, Guid deckId)
        {
            var deckOwnerId = await dbContext.Decks
                .AsNoTracking()
                .Include(deck => deck.Owner)
                .Where(deck => deck.Id == deckId)
                .Select(deck => deck.Owner.Id)
                .SingleAsync();
            if (deckOwnerId != userId)
                throw new RequestInputException("Current user not owner of deck");
        }
        public static async Task ChecUserDoesNotHaveDeckWithNameAsync(MemCheckDbContext dbContext, Guid userId, string name, ILocalized localizer)
        {
            if (await dbContext.Decks.Where(deck => (deck.Owner.Id == userId) && EF.Functions.Like(deck.Description, name)).AnyAsync())
                throw new RequestInputException($"{localizer.Get("ADeckWithName")} '{name}' {localizer.Get("AlreadyExists")}");
        }
    }
}
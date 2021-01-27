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
        public const int TagMinNameLength = 3;
        public const int TagMaxNameLength = 50;
        public static readonly ImmutableHashSet<char> ForbiddenCharsInTags = new[] { '<', '>' }.ToImmutableHashSet();
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
        public static async Task CheckCanCreateTagWithName(string name, MemCheckDbContext dbContext, ILocalized localizer)
        {
            if (name != name.Trim())
                throw new InvalidOperationException("Invalid Name: not trimmed");
            if (name.Length < TagMinNameLength || name.Length > TagMaxNameLength)
                throw new RequestInputException(localizer.Get("InvalidNameLength") + $" {name.Length}, " + localizer.Get("MustBeBetween") + $" {TagMinNameLength} " + localizer.Get("And") + $" {TagMaxNameLength}");
            foreach (var forbiddenChar in ForbiddenCharsInTags)
                if (name.Contains(forbiddenChar))
                    throw new RequestInputException(localizer.Get("InvalidTagName") + " '" + name + "' ('" + forbiddenChar + ' ' + localizer.Get("IsForbidden") + ")");
            if (await dbContext.Tags.Where(tag => EF.Functions.Like(tag.Name, $"{name}")).AnyAsync())
                throw new RequestInputException(localizer.Get("ATagWithName") + " '" + name + "' " + localizer.Get("AlreadyExistsCaseInsensitive"));
        }
    }
}
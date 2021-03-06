﻿using MemCheck.Application.Heaping;
using MemCheck.Database;
using MemCheck.Domain;
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
        public const int ImageMinNameLength = 3;
        public const int ImageMaxNameLength = 100;
        public const int ImageMinSourceLength = 3;
        public const int ImageMaxSourceLength = 1000;
        public const int ImageMinDescriptionLength = 3;
        public const int ImageMaxDescriptionLength = 5000;
        public const int ImageMinVersionDescriptionLength = 3;
        public const int ImageMaxVersionDescriptionLength = 1000;
        public const int DeckMinNameLength = 3;
        public const int DeckMaxNameLength = 36;
        public const int LanguageMinNameLength = 2;
        public const int LanguageMaxNameLength = 36;
        public static readonly ImmutableHashSet<char> ForbiddenCharsInTags = new[] { '<', '>' }.ToImmutableHashSet();
        public static readonly ImmutableHashSet<char> ForbiddenCharsInImageNames = new[] { '<', '>' }.ToImmutableHashSet();
        public static readonly ImmutableHashSet<char> ForbiddenCharsInImageSource = new[] { '<', '>' }.ToImmutableHashSet();
        public static readonly ImmutableHashSet<char> ForbiddenCharsInImageDescription = new[] { '<', '>' }.ToImmutableHashSet();
        public static readonly ImmutableHashSet<char> ForbiddenCharsInImageVersionDescription = new[] { '<', '>' }.ToImmutableHashSet();
        public static readonly ImmutableHashSet<char> ForbiddenCharsInLanguages = new[] { '<', '>' }.ToImmutableHashSet();
        public static bool IsReservedGuid(Guid g)
        {
            return reservedGuids.Contains(g);
        }
        public static void CheckNotReservedGuid(Guid g)
        {
            if (IsReservedGuid(g))
                throw new InvalidOperationException("Bad Guid");
        }
        public static void CheckContainsNoReservedGuid(IEnumerable<Guid> guids)
        {
            if (guids.Any(g => IsReservedGuid(g)))
                throw new InvalidOperationException("Bad Guid");
        }
        public static async Task CheckCardExistsAsync(MemCheckDbContext dbContext, Guid cardId)
        {
            if (!await dbContext.Cards.AsNoTracking().AnyAsync(card => card.Id == cardId))
                throw new InvalidOperationException("Card noes not exist");
        }
        public static async Task CheckUserExistsAsync(MemCheckDbContext dbContext, Guid userId)
        {
            if (!await dbContext.Users.AsNoTracking().AnyAsync(user => user.Id == userId))
                throw new InvalidOperationException("Current user not owner of deck");
        }
        public static void CheckUserIsOwnerOfDeck(MemCheckDbContext dbContext, Guid userId, Guid deckId)
        {
            var owner = dbContext.Decks.AsNoTracking().Where(deck => deck.Id == deckId).Select(deck => deck.Owner.Id).Single();
            if (owner != userId)
                throw new InvalidOperationException("Current user not owner of deck");
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
                throw new InvalidOperationException("Current user not owner of deck");
        }
        public static async Task CheckUserDoesNotHaveDeckWithNameAsync(MemCheckDbContext dbContext, Guid userId, string name, ILocalized localizer)
        {
            if (await dbContext.Decks.AsNoTracking().Where(deck => (deck.Owner.Id == userId) && EF.Functions.Like(deck.Description, name)).AnyAsync())
                throw new RequestInputException($"{localizer.Get("ADeckWithName")} '{name}' {localizer.Get("AlreadyExists")}");
        }
        public static async Task CheckCanCreateTag(string name, string description, Guid? updatingId, MemCheckDbContext dbContext, ILocalized localizer)
        {
            if (name != name.Trim())
                throw new InvalidOperationException("Invalid Name: not trimmed");
            if (description != description.Trim())
                throw new InvalidOperationException("Invalid Description: not trimmed");
            if (name.Length < Tag.MinNameLength || name.Length > Tag.MaxNameLength)
                throw new RequestInputException(localizer.Get("InvalidNameLength") + $" {name.Length}, " + localizer.Get("MustBeBetween") + $" {Tag.MinNameLength} " + localizer.Get("And") + $" {Tag.MaxNameLength}");
            if (description.Length > Tag.MaxDescriptionLength)
                throw new RequestInputException(localizer.Get("InvalidDescriptionLength") + $" {name.Length}, " + localizer.Get("MustBeNoMoreThan") + $" {Tag.MaxDescriptionLength}");
            foreach (var forbiddenChar in ForbiddenCharsInTags)
                if (name.Contains(forbiddenChar))
                    throw new RequestInputException(localizer.Get("InvalidTagName") + " '" + name + "' ('" + forbiddenChar + ' ' + localizer.Get("IsForbidden") + ")");
            if (updatingId != null)
            {
                var current = await dbContext.Tags.AsNoTracking().SingleAsync(tag => tag.Id == updatingId.Value);

                if (current.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    if (current.Description == description)
                        throw new RequestInputException(localizer.Get("NoDifference"));
                }
                else
                {
                    if (await dbContext.Tags.AsNoTracking().Where(tag => EF.Functions.Like(tag.Name, name)).AnyAsync())
                        throw new RequestInputException(localizer.Get("ATagWithName") + " '" + name + "' " + localizer.Get("AlreadyExistsCaseInsensitive"));
                }
            }
            else
            {
                if (await dbContext.Tags.AsNoTracking().Where(tag => EF.Functions.Like(tag.Name, name)).AnyAsync())
                    throw new RequestInputException(localizer.Get("ATagWithName") + " '" + name + "' " + localizer.Get("AlreadyExistsCaseInsensitive"));
            }
        }
        public static async Task CheckCanCreateLanguageWithName(string name, MemCheckDbContext dbContext, ILocalized localizer)
        {
            if (name != name.Trim())
                throw new InvalidOperationException("Invalid Name: not trimmed");
            if (name.Length < LanguageMinNameLength || name.Length > LanguageMaxNameLength)
                throw new RequestInputException(localizer.Get("InvalidNameLength") + $" {name.Length}, " + localizer.Get("MustBeBetween") + $" {LanguageMinNameLength} " + localizer.Get("And") + $" {LanguageMaxNameLength}");
            foreach (var forbiddenChar in ForbiddenCharsInLanguages)
                if (name.Contains(forbiddenChar))
                    throw new RequestInputException(localizer.Get("InvalidLanguageName") + " '" + name + "' ('" + forbiddenChar + ' ' + localizer.Get("IsForbidden") + ")");
            if (await dbContext.CardLanguages.AsNoTracking().Where(language => EF.Functions.Like(language.Name, name)).AnyAsync())
                throw new RequestInputException(localizer.Get("ALanguageWithName") + " '" + name + "' " + localizer.Get("AlreadyExistsCaseInsensitive"));
        }
        public static async Task CheckCanCreateDeckAsync(Guid userId, string deckName, int heapingAlgorithmId, MemCheckDbContext dbContext, ILocalized localizer)
        {
            if (deckName != deckName.Trim())
                throw new InvalidOperationException("Invalid Name: not trimmed");

            if (deckName.Length < DeckMinNameLength || deckName.Length > DeckMaxNameLength)
                throw new RequestInputException(localizer.Get("InvalidNameLength") + $" {deckName.Length}" + localizer.Get("MustBeBetween") + $" {DeckMinNameLength} " + localizer.Get("And") + $" {DeckMaxNameLength}");

            if (!HeapingAlgorithms.Instance.Ids.Contains(heapingAlgorithmId))
                throw new InvalidOperationException($"Invalid heaping algorithm: {heapingAlgorithmId}");

            await CheckUserExistsAsync(dbContext, userId);
            await CheckUserDoesNotHaveDeckWithNameAsync(dbContext, userId, deckName, localizer);
        }
        public static async Task CheckCanCreateImageWithNameAsync(string name, MemCheckDbContext dbContext, ILocalized localizer)
        {
            if (name != name.Trim())
                throw new InvalidOperationException("Invalid Name: not trimmed");
            if (name.Length < ImageMinNameLength || name.Length > ImageMaxNameLength)
                throw new RequestInputException(localizer.Get("InvalidNameLength") + $" {name.Length}, " + localizer.Get("MustBeBetween") + $" {ImageMinNameLength} " + localizer.Get("And") + $" {ImageMaxNameLength}");
            foreach (var forbiddenChar in ForbiddenCharsInImageNames)
                if (name.Contains(forbiddenChar))
                    throw new RequestInputException(localizer.Get("InvalidImageName") + " '" + name + "' ('" + forbiddenChar + ' ' + localizer.Get("IsForbidden") + ")");
            if (await dbContext.Images.AsNoTracking().Where(img => EF.Functions.Like(img.Name, name)).AnyAsync())
                throw new RequestInputException(localizer.Get("AnImageWithName") + " '" + name + "' " + localizer.Get("AlreadyExistsCaseInsensitive"));
        }
        public static void CheckCanCreateImageWithSource(string source, ILocalized localizer)
        {
            if (source != source.Trim())
                throw new InvalidOperationException("Invalid source: not trimmed");
            if (source.Length < ImageMinSourceLength || source.Length > ImageMaxSourceLength)
                throw new RequestInputException(localizer.Get("InvalidSourceLength") + $" {source.Length}, " + localizer.Get("MustBeBetween") + $" {ImageMinSourceLength} " + localizer.Get("And") + $" {ImageMaxSourceLength}");
            foreach (var forbiddenChar in ForbiddenCharsInImageSource)
                if (source.Contains(forbiddenChar))
                    throw new RequestInputException(localizer.Get("InvalidImageSource") + " '" + source + "' ('" + forbiddenChar + ' ' + localizer.Get("IsForbidden") + ")");
        }
        public static void CheckCanCreateImageWithDescription(string description, ILocalized localizer)
        {
            if (description != description.Trim())
                throw new InvalidOperationException("Invalid description: not trimmed");
            if (description.Length < ImageMinDescriptionLength || description.Length > ImageMaxDescriptionLength)
                throw new RequestInputException(localizer.Get("InvalidDescriptionLength") + $" {description.Length}, " + localizer.Get("MustBeBetween") + $" {ImageMinDescriptionLength} " + localizer.Get("And") + $" {ImageMaxDescriptionLength}");
            foreach (var forbiddenChar in ForbiddenCharsInImageDescription)
                if (description.Contains(forbiddenChar))
                    throw new RequestInputException(localizer.Get("InvalidImageDescription") + " '" + description + "' ('" + forbiddenChar + ' ' + localizer.Get("IsForbidden") + ")");
        }
        public static void CheckCanCreateImageWithVersionDescription(string versionDescription, ILocalized localizer)
        {
            if (versionDescription != versionDescription.Trim())
                throw new InvalidOperationException("Invalid version description: not trimmed");
            if (versionDescription.Length < ImageMinVersionDescriptionLength || versionDescription.Length > ImageMaxVersionDescriptionLength)
                throw new RequestInputException(localizer.Get("InvalidVersionDescriptionLength") + $" {versionDescription.Length}, " + localizer.Get("MustBeBetween") + $" {ImageMinVersionDescriptionLength} " + localizer.Get("And") + $" {ImageMaxVersionDescriptionLength}");
            foreach (var forbiddenChar in ForbiddenCharsInImageVersionDescription)
                if (versionDescription.Contains(forbiddenChar))
                    throw new RequestInputException(localizer.Get("InvalidImageVersionDescription") + " '" + versionDescription + "' ('" + forbiddenChar + ' ' + localizer.Get("IsForbidden") + ")");
        }
    }
}
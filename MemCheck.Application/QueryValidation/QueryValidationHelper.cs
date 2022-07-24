using MemCheck.Application.Heaping;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.QueryValidation;

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
    public const string ExceptionMesg_CardDoesNotExist = "Card noes not exist";
    public const string ExceptionMesg_TagDoesNotExist = "Tag not found";
    public const string ExceptionMesg_UserDoesNotExist = "User not found";
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
            throw new InvalidOperationException(ExceptionMesg_CardDoesNotExist);
    }
    public static async Task CheckCardsExistAsync(MemCheckDbContext dbContext, IEnumerable<Guid> cardIds)
    {
        if (await dbContext.Cards.AsNoTracking().Where(card => cardIds.Contains(card.Id)).CountAsync() != cardIds.Count())
            throw new InvalidOperationException(ExceptionMesg_CardDoesNotExist);
    }
    public static async Task CheckUserExistsAsync(MemCheckDbContext dbContext, Guid userId)
    {
        var user = await dbContext.Users.AsNoTracking().Where(user => user.Id == userId).SingleOrDefaultAsync();
        if (user == null || user.DeletionDate != null)
            throw new InvalidOperationException(ExceptionMesg_UserDoesNotExist);
    }
    public static async Task CheckUsersExistAsync(MemCheckDbContext dbContext, IEnumerable<Guid> userIds)
    {
        if (await dbContext.Users.AsNoTracking().Where(user => userIds.Contains(user.Id)).CountAsync() != userIds.Count())
            throw new InvalidOperationException(ExceptionMesg_UserDoesNotExist);
    }
    public static async Task CheckUserExistsAndIsAdminAsync(MemCheckDbContext dbContext, Guid userId, IRoleChecker roleChecker)
    {
        var user = await dbContext.Users.AsNoTracking().Where(user => user.Id == userId).SingleOrDefaultAsync();
        if (user == null || user.DeletionDate != null)
            throw new InvalidOperationException("User not found");
        if (!await roleChecker.UserIsAdminAsync(user))
            throw new InvalidOperationException("User not admin");
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
            throw new RequestInputException($"{localizer.GetLocalized("ADeckWithName")} '{name}' {localizer.GetLocalized("AlreadyExists")}");
    }
    public static async Task CheckTagExistsAsync(Guid tagId, MemCheckDbContext dbContext)
    {
        if (!await dbContext.Tags.AsNoTracking().AnyAsync(tag => tag.Id == tagId))
            throw new RequestInputException(ExceptionMesg_TagDoesNotExist);
    }
    public static async Task CheckTagsExistAsync(IEnumerable<Guid> tagIds, MemCheckDbContext dbContext)
    {
        if (await dbContext.Tags.AsNoTracking().Where(tag => tagIds.Contains(tag.Id)).CountAsync() != tagIds.Count())
            throw new RequestInputException(ExceptionMesg_TagDoesNotExist);
    }
    public static async Task CheckCanCreateTag(string name, string description, Guid? updatingId, MemCheckDbContext dbContext, ILocalized localizer, IRoleChecker roleChecker, Guid userId)
    {
        if (name != name.Trim())
            throw new InvalidOperationException("Invalid Name: not trimmed");
        if (description != description.Trim())
            throw new InvalidOperationException("Invalid Description: not trimmed");
        if (name.Length is < Tag.MinNameLength or > Tag.MaxNameLength)
            throw new RequestInputException(localizer.GetLocalized("InvalidNameLength") + $" {name.Length}, " + localizer.GetLocalized("MustBeBetween") + $" {Tag.MinNameLength} " + localizer.GetLocalized("And") + $" {Tag.MaxNameLength}");
        if (description.Length > Tag.MaxDescriptionLength)
            throw new RequestInputException(localizer.GetLocalized("InvalidDescriptionLength") + $" {name.Length}, " + localizer.GetLocalized("MustBeNoMoreThan") + $" {Tag.MaxDescriptionLength}");
        foreach (var forbiddenChar in ForbiddenCharsInTags)
            if (name.Contains(forbiddenChar, StringComparison.OrdinalIgnoreCase))
                throw new RequestInputException(localizer.GetLocalized("InvalidTagName") + " '" + name + "' ('" + forbiddenChar + ' ' + localizer.GetLocalized("IsForbidden") + ")");
        if (updatingId != null)
        {
            var current = await dbContext.Tags.AsNoTracking().SingleAsync(tag => tag.Id == updatingId.Value);

            if (TagIsPerso(current.Name))
            {
                if (!await roleChecker.UserIsAdminAsync(dbContext, userId))
                    throw new RequestInputException(localizer.GetLocalized("OnlyAdminsCanModifyPersoTag"));

                if (!TagIsPerso(name))
                    throw new RequestInputException($"Can not rename {Tag.Perso} tag");
            }

            if (current.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                if (current.Description == description)
                    throw new RequestInputException(localizer.GetLocalized("NoDifference"));
            }
            else
            {
                if (await dbContext.Tags.AsNoTracking().Where(tag => EF.Functions.Like(tag.Name, name)).AnyAsync())
                    throw new RequestInputException(localizer.GetLocalized("ATagWithName") + " '" + name + "' " + localizer.GetLocalized("AlreadyExistsCaseInsensitive"));
            }
        }
        else
        {
            if (await dbContext.Tags.AsNoTracking().Where(tag => EF.Functions.Like(tag.Name, name)).AnyAsync())
                throw new RequestInputException(localizer.GetLocalized("ATagWithName") + " '" + name + "' " + localizer.GetLocalized("AlreadyExistsCaseInsensitive"));
        }
    }
    public static async Task CheckCanCreateLanguageWithName(string name, MemCheckDbContext dbContext, ILocalized localizer)
    {
        if (name != name.Trim())
            throw new InvalidOperationException("Invalid Name: not trimmed");
        if (name.Length is < LanguageMinNameLength or > LanguageMaxNameLength)
            throw new RequestInputException(localizer.GetLocalized("InvalidNameLength") + $" {name.Length}, " + localizer.GetLocalized("MustBeBetween") + $" {LanguageMinNameLength} " + localizer.GetLocalized("And") + $" {LanguageMaxNameLength}");
        foreach (var forbiddenChar in ForbiddenCharsInLanguages)
            if (name.Contains(forbiddenChar, StringComparison.OrdinalIgnoreCase))
                throw new RequestInputException(localizer.GetLocalized("InvalidLanguageName") + " '" + name + "' ('" + forbiddenChar + ' ' + localizer.GetLocalized("IsForbidden") + ")");
        if (await dbContext.CardLanguages.AsNoTracking().Where(language => EF.Functions.Like(language.Name, name)).AnyAsync())
            throw new RequestInputException(localizer.GetLocalized("ALanguageWithName") + " '" + name + "' " + localizer.GetLocalized("AlreadyExistsCaseInsensitive"));
    }
    public static async Task CheckCanCreateDeckAsync(Guid userId, string deckName, int heapingAlgorithmId, MemCheckDbContext dbContext, ILocalized localizer)
    {
        if (deckName != deckName.Trim())
            throw new InvalidOperationException("Invalid Name: not trimmed");

        if (deckName.Length is < DeckMinNameLength or > DeckMaxNameLength)
            throw new RequestInputException(localizer.GetLocalized("InvalidNameLength") + $" {deckName.Length}" + localizer.GetLocalized("MustBeBetween") + $" {DeckMinNameLength} " + localizer.GetLocalized("And") + $" {DeckMaxNameLength}");

        if (!HeapingAlgorithms.Instance.Ids.Contains(heapingAlgorithmId))
            throw new InvalidOperationException($"Invalid heaping algorithm: {heapingAlgorithmId}");

        await CheckUserExistsAsync(dbContext, userId);
        await CheckUserDoesNotHaveDeckWithNameAsync(dbContext, userId, deckName, localizer);
    }
    public static async Task CheckCanCreateImageWithNameAsync(string name, MemCheckDbContext dbContext, ILocalized localizer)
    {
        CheckImageNameValidity(name, localizer);
        if (await dbContext.Images.AsNoTracking().Where(img => EF.Functions.Like(img.Name, name)).AnyAsync())
            throw new RequestInputException(localizer.GetLocalized("AnImageWithName") + " '" + name + "' " + localizer.GetLocalized("AlreadyExistsCaseInsensitive"));
    }
    public static void CheckImageNameValidity(string name, ILocalized localizer)
    {
        if (name != name.Trim())
            throw new InvalidOperationException("Invalid Name: not trimmed");
        if (name.Length is < ImageMinNameLength or > ImageMaxNameLength)
            throw new RequestInputException(localizer.GetLocalized("InvalidNameLength") + $" {name.Length}, " + localizer.GetLocalized("MustBeBetween") + $" {ImageMinNameLength} " + localizer.GetLocalized("And") + $" {ImageMaxNameLength}");
        foreach (var forbiddenChar in ForbiddenCharsInImageNames)
            if (name.Contains(forbiddenChar, StringComparison.OrdinalIgnoreCase))
                throw new RequestInputException(localizer.GetLocalized("InvalidImageName") + " '" + name + "' ('" + forbiddenChar + ' ' + localizer.GetLocalized("IsForbidden") + ")");
    }
    public static void CheckCanCreateImageWithSource(string source, ILocalized localizer)
    {
        if (source != source.Trim())
            throw new InvalidOperationException("Invalid source: not trimmed");
        if (source.Length is < ImageMinSourceLength or > ImageMaxSourceLength)
            throw new RequestInputException(localizer.GetLocalized("InvalidSourceLength") + $" {source.Length}, " + localizer.GetLocalized("MustBeBetween") + $" {ImageMinSourceLength} " + localizer.GetLocalized("And") + $" {ImageMaxSourceLength}");
        foreach (var forbiddenChar in ForbiddenCharsInImageSource)
            if (source.Contains(forbiddenChar, StringComparison.OrdinalIgnoreCase))
                throw new RequestInputException(localizer.GetLocalized("InvalidImageSource") + " '" + source + "' ('" + forbiddenChar + ' ' + localizer.GetLocalized("IsForbidden") + ")");
    }
    public static void CheckCanCreateImageWithDescription(string description, ILocalized localizer)
    {
        if (description != description.Trim())
            throw new InvalidOperationException("Invalid description: not trimmed");
        if (description.Length is < ImageMinDescriptionLength or > ImageMaxDescriptionLength)
            throw new RequestInputException(localizer.GetLocalized("InvalidDescriptionLength") + $" {description.Length}" + localizer.GetLocalized("MustBeBetween") + $" {ImageMinDescriptionLength} " + localizer.GetLocalized("And") + $" {ImageMaxDescriptionLength}");
        foreach (var forbiddenChar in ForbiddenCharsInImageDescription)
            if (description.Contains(forbiddenChar, StringComparison.OrdinalIgnoreCase))
                throw new RequestInputException(localizer.GetLocalized("InvalidImageDescription") + " '" + description + "' ('" + forbiddenChar + ' ' + localizer.GetLocalized("IsForbidden") + ")");
    }
    public static void CheckCanCreateImageWithVersionDescription(string versionDescription, ILocalized localizer)
    {
        if (versionDescription != versionDescription.Trim())
            throw new InvalidOperationException("Invalid version description: not trimmed");
        if (versionDescription.Length is < ImageMinVersionDescriptionLength or > ImageMaxVersionDescriptionLength)
            throw new RequestInputException(localizer.GetLocalized("InvalidVersionDescriptionLength") + $" {versionDescription.Length}" + localizer.GetLocalized("MustBeBetween") + $" {ImageMinVersionDescriptionLength} " + localizer.GetLocalized("And") + $" {ImageMaxVersionDescriptionLength}");
        foreach (var forbiddenChar in ForbiddenCharsInImageVersionDescription)
            if (versionDescription.Contains(forbiddenChar, StringComparison.Ordinal))
                throw new RequestInputException(localizer.GetLocalized("InvalidImageVersionDescription") + " '" + versionDescription + "' ('" + forbiddenChar + ' ' + localizer.GetLocalized("IsForbidden") + ")");
    }
    public static bool TagIsPerso(Guid tagId, MemCheckDbContext dbContext)
    {
        var tag = dbContext.Tags.AsNoTracking().Single(tag => tag.Id == tagId);
        return TagIsPerso(tag.Name);
    }
    public static bool TagIsPerso(string tagName)
    {
        //Hardcoding this is highly debatable
        //It would probably be nicer that the admin defines the perso tag somewhere in the config of the app, and we use this tag's id
        //Can be considered later
        //Currently I indulge myself with this trick
        return tagName.Equals(Tag.Perso, StringComparison.OrdinalIgnoreCase);
    }
}

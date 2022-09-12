using MemCheck.Application.Cards;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MemCheck.Application.Helpers;

public static class CardHelper
{
    public static async Task<Card> CreateAsync(DbContextOptions<MemCheckDbContext> testDB,
        Guid versionCreatorId, DateTime? versionDate = null, IEnumerable<Guid>? userWithViewIds = null, Guid? language = null, IEnumerable<Guid>? tagIds = null,
        string? frontSide = null, string? backSide = null, string? additionalInfo = null, string? references = null, string? versionDescription = null)
    {
        //userWithViewIds null means public card

        if (language == null)
            language = await CardLanguageHelper.CreateAsync(testDB);

        var request = new CreateCard.Request(
            versionCreatorId,
            frontSide ?? RandomHelper.String(),
            backSide ?? RandomHelper.String(),
            additionalInfo ?? RandomHelper.String(),
            references ?? RandomHelper.String(),
            language.Value,
            tagIds ?? Array.Empty<Guid>(),
            userWithViewIds ?? Array.Empty<Guid>(),
            versionDescription ?? RandomHelper.String());

        Guid cardId;
        using (var dbContext = new MemCheckDbContext(testDB))
            cardId = (await new CreateCard(dbContext.AsCallContext(), versionDate).RunAsync(request)).CardId;

        var result = await GetCardFromDbWithAllfieldsAsync(testDB, cardId);
        return result;
    }
    public static async Task<Guid> CreateIdAsync(DbContextOptions<MemCheckDbContext> testDB,
        Guid versionCreatorId, DateTime? versionDate = null, IEnumerable<Guid>? userWithViewIds = null, Guid? language = null, IEnumerable<Guid>? tagIds = null,
        string? frontSide = null, string? backSide = null, string? additionalInfo = null, string? references = null, string? versionDescription = null)
    {
        return (await CreateAsync(testDB, versionCreatorId, versionDate, userWithViewIds, language, tagIds, frontSide, backSide, additionalInfo, references, versionDescription)).Id;
    }
    public static async Task AssertCardHasFrontSide(DbContextOptions<MemCheckDbContext> testDB, Guid cardId, string expected)
    {
        using var dbContext = new MemCheckDbContext(testDB);
        var cardFromDb = await dbContext.Cards.SingleAsync(c => c.Id == cardId);
        Assert.AreEqual(expected, cardFromDb.FrontSide);
    }
    public static async Task<Card> GetCardFromDbWithAllfieldsAsync(DbContextOptions<MemCheckDbContext> db, Guid cardId)
    {
        using var dbContext = new MemCheckDbContext(db);

        return await dbContext.Cards
            .Include(card => card.VersionCreator)
            .Include(card => card.CardLanguage)
            .Include(card => card.CardInDecks)
            .ThenInclude(cardInDeck => cardInDeck.Deck)
            .Include(card => card.TagsInCards)
            .Include(card => card.UsersWithView)
            .Include(card => card.UserCardRating)
            .Include(card => card.PreviousVersion)
            .SingleAsync(card => card.Id == cardId);
    }
}

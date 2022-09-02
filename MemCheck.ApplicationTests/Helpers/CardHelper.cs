using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Helpers;

public static class CardHelper
{
    public static async Task<Card> CreateAsync(DbContextOptions<MemCheckDbContext> testDB,
        Guid versionCreatorId, DateTime? versionDate = null, IEnumerable<Guid>? userWithViewIds = null, Guid? language = null, IEnumerable<Guid>? tagIds = null,
        string? frontSide = null, string? backSide = null, string? additionalInfo = null, string? references = null, string? versionDescription = null)
    {
        //userWithViewIds null means public card

        using var dbContext = new MemCheckDbContext(testDB);
        var creator = await dbContext.Users.Where(u => u.Id == versionCreatorId).SingleAsync();

        var result = new Card
        {
            VersionCreator = creator,
            FrontSide = frontSide ?? RandomHelper.String(),
            BackSide = backSide ?? RandomHelper.String(),
            AdditionalInfo = additionalInfo ?? RandomHelper.String(),
            References = references ?? RandomHelper.String(),
            VersionDescription = versionDescription ?? RandomHelper.String(),
            VersionType = CardVersionType.Creation
        };
        if (language == null)
            language = await CardLanguageHelper.CreateAsync(testDB);
        result.CardLanguage = await dbContext.CardLanguages.SingleAsync(l => l.Id == language);
        if (versionDate != null)
        {
            result.InitialCreationUtcDate = versionDate.Value;
            result.VersionUtcDate = versionDate.Value;
        }
        dbContext.Cards.Add(result);

        var usersWithView = new List<UserWithViewOnCard>();
        if (userWithViewIds != null && userWithViewIds.Any())
        {
            Assert.IsTrue(userWithViewIds.Any(id => id == versionCreatorId), "Version creator must be allowed to view");
            foreach (var userWithViewId in userWithViewIds)
            {
                var userWithView = new UserWithViewOnCard { CardId = result.Id, UserId = userWithViewId };
                dbContext.UsersWithViewOnCards.Add(userWithView);
                usersWithView.Add(userWithView);
            }
        }
        result.UsersWithView = usersWithView;

        var tags = new List<TagInCard>();
        if (tagIds != null)
            foreach (var tagId in tagIds)
            {
                var tagInCard = new TagInCard
                {
                    CardId = result.Id,
                    TagId = tagId
                };
                dbContext.TagsInCards.Add(tagInCard);
                tags.Add(tagInCard);
            }
        result.TagsInCards = tags;
        result.Images = new List<ImageInCard>();
        await dbContext.SaveChangesAsync();
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
            .Include(card => card.Images)
            .Include(card => card.UserCardRating)
            .Include(card => card.PreviousVersion)
            .SingleAsync(card => card.Id == cardId);
    }
}

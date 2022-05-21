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
        string? frontSide = null, string? backSide = null, string? additionalInfo = null, string? references = null,
        IEnumerable<Guid>? frontSideImages = null, IEnumerable<Guid>? additionalSideImages = null,
        string? versionDescription = null)
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

        var images = new List<ImageInCard>();
        if (frontSideImages != null)
            foreach (var frontSideImage in frontSideImages)
            {
                var img = new ImageInCard() { ImageId = frontSideImage, CardId = result.Id, CardSide = ImageInCard.FrontSide };
                dbContext.ImagesInCards.Add(img);
                images.Add(img);
            }
        if (additionalSideImages != null)
            foreach (var additionalSideImage in additionalSideImages)
            {
                var img = new ImageInCard() { ImageId = additionalSideImage, CardId = result.Id, CardSide = ImageInCard.AdditionalInfo };
                dbContext.ImagesInCards.Add(img);
                images.Add(img);
            }
        result.Images = images;

        await dbContext.SaveChangesAsync();
        return result;
    }
    public static async Task<Guid> CreateIdAsync(DbContextOptions<MemCheckDbContext> testDB,
        Guid versionCreatorId, DateTime? versionDate = null, IEnumerable<Guid>? userWithViewIds = null, Guid? language = null, IEnumerable<Guid>? tagIds = null,
        string? frontSide = null, string? backSide = null, string? additionalInfo = null, string? references = null,
        IEnumerable<Guid>? frontSideImages = null, IEnumerable<Guid>? additionalSideImages = null,
        string? versionDescription = null)
    {
        return (await CreateAsync(testDB, versionCreatorId, versionDate, userWithViewIds, language, tagIds, frontSide, backSide, additionalInfo, references, frontSideImages, additionalSideImages, versionDescription)).Id;
    }
    public static async Task AssertCardHasFrontSide(DbContextOptions<MemCheckDbContext> testDB, Guid cardId, string expected)
    {
        using var dbContext = new MemCheckDbContext(testDB);
        var cardFromDb = await dbContext.Cards.SingleAsync(c => c.Id == cardId);
        Assert.AreEqual(expected, cardFromDb.FrontSide);
    }
}

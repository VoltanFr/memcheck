using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using MemCheck.Database;
using System.Linq;
using System;
using MemCheck.Domain;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MemCheck.Application.Tests.Helpers
{
    public static class CardHelper
    {
        public static async Task<Card> CreateAsync(DbContextOptions<MemCheckDbContext> testDB,
            Guid versionCreatorId, DateTime? versionDate = null, IEnumerable<Guid>? userWithViewIds = null, Guid? language = null, IEnumerable<Guid>? tagIds = null,
            string? frontSide = null, string? backSide = null, string? additionalInfo = null,
            IEnumerable<Guid>? frontSideImages = null, IEnumerable<Guid>? additionalSideImages = null,
            string? versionDescription = null)
        {
            //userWithViewIds null means public card

            using var dbContext = new MemCheckDbContext(testDB);
            var creator = await dbContext.Users.Where(u => u.Id == versionCreatorId).SingleAsync();

            var result = new Card
            {
                VersionCreator = creator,
                FrontSide = frontSide ?? StringHelper.RandomString(),
                BackSide = backSide ?? StringHelper.RandomString(),
                AdditionalInfo = additionalInfo ?? StringHelper.RandomString(),
                VersionDescription = versionDescription ?? StringHelper.RandomString(),
                VersionType = CardVersionType.Creation
            };
            if (language != null)
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
    }
}

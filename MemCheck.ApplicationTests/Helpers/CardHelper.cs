﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            string? frontSide = null, string? backSide = null, string? additionalInfo = null, string? versionDescription = null)
        {
            //userWithViewIds null means public card

            using var dbContext = new MemCheckDbContext(testDB);
            var creator = await dbContext.Users.Where(u => u.Id == versionCreatorId).SingleAsync();

            var result = new Card();
            result.VersionCreator = creator;
            result.FrontSide = frontSide ?? StringHelper.RandomString();
            result.BackSide = backSide ?? StringHelper.RandomString();
            result.AdditionalInfo = additionalInfo ?? StringHelper.RandomString();
            result.VersionDescription = versionDescription ?? StringHelper.RandomString();
            result.VersionType = CardVersionType.Creation;
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
                    var userWithView = new UserWithViewOnCard();
                    userWithView.CardId = result.Id;
                    userWithView.UserId = userWithViewId;
                    dbContext.UsersWithViewOnCards.Add(userWithView);
                    usersWithView.Add(userWithView);
                }
            }
            result.UsersWithView = usersWithView;

            var tags = new List<TagInCard>();
            if (tagIds != null)
                foreach (var tagId in tagIds)
                {
                    var tagInCard = new TagInCard();
                    tagInCard.CardId = result.Id;
                    tagInCard.TagId = tagId;
                    dbContext.TagsInCards.Add(tagInCard);
                    tags.Add(tagInCard);
                }
            result.TagsInCards = tags;

            result.Images = new ImageInCard[0];

            await dbContext.SaveChangesAsync();
            return result;
        }
    }
}

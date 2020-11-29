﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using MemCheck.Database;
using System.Linq;
using System;
using MemCheck.Domain;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MemCheck.Application.Tests.Notifying
{
    public class CardHelper
    {
        public static async Task<Card> CreateAsync(DbContextOptions<MemCheckDbContext> testDB, Guid versionCreatorId, DateTime versionDate, IEnumerable<Guid>? userWithViewIds = null)
        {
            //userWithViewIds null means public card

            using var dbContext = new MemCheckDbContext(testDB);
            var creator = await dbContext.Users.Where(u => u.Id == versionCreatorId).SingleAsync();

            var result = new Card();
            result.VersionCreator = creator;
            result.FrontSide = StringServices.RandomString();
            result.BackSide = StringServices.RandomString();
            result.AdditionalInfo = StringServices.RandomString();
            result.VersionDescription = StringServices.RandomString();
            result.VersionType = CardVersionType.Creation;
            result.InitialCreationUtcDate = versionDate;
            result.VersionUtcDate = versionDate;
            dbContext.Cards.Add(result);

            var usersWithView = new List<UserWithViewOnCard>();
            if (userWithViewIds != null)
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

            await dbContext.SaveChangesAsync();
            return result;
        }
    }
}
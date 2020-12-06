using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using MemCheck.Database;
using System.Linq;
using System;
using MemCheck.Domain;
using System.Threading.Tasks;
using MemCheck.Application.CardChanging;
using MemCheck.Application.Tests.BasicHelpers;
using MemCheck.Application.Tests.Helpers;

namespace MemCheck.Application.Tests
{
    [TestClass()]
    public class DeleteCardTests
    {
        #region Private methods
        private async Task<MemCheckUser> CreateUserAsync(DbContextOptions<MemCheckDbContext> db)
        {
            using var dbContext = new MemCheckDbContext(db);
            var result = new MemCheckUser();
            dbContext.Users.Add(result);
            await dbContext.SaveChangesAsync();
            return result;
        }
        private async Task CreateCardNotificationAsync(DbContextOptions<MemCheckDbContext> db, Guid subscriberId, Guid cardId)
        {
            using var dbContext = new MemCheckDbContext(db);
            var notif = new CardNotificationSubscription();
            notif.CardId = cardId;
            notif.UserId = subscriberId;
            dbContext.CardNotifications.Add(notif);
            await dbContext.SaveChangesAsync();
        }
        private async Task<Card> CreateCardAsync(DbContextOptions<MemCheckDbContext> db, Guid versionCreatorId, DateTime versionDate)
        {
            using var dbContext = new MemCheckDbContext(db);
            var creator = await dbContext.Users.Where(u => u.Id == versionCreatorId).SingleAsync();
            var result = new Card();
            result.VersionCreator = creator;
            result.FrontSide = StringServices.RandomString();
            result.BackSide = StringServices.RandomString();
            result.AdditionalInfo = StringServices.RandomString();
            result.VersionDescription = StringServices.RandomString();
            result.VersionType = CardVersionType.Creation;
            dbContext.Cards.Add(result);
            await dbContext.SaveChangesAsync();
            return result;
        }
        private async Task DeleteCardAsync(DbContextOptions<MemCheckDbContext> db, Guid userId, Guid cardId, DateTime deletionDate)
        {
            using var dbContext = new MemCheckDbContext(db);
            var deleter = new DeleteCards(dbContext, new TestLocalizer());
            var deletionRequest = new DeleteCards.Request(userId, new[] { cardId });
            await deleter.RunAsync(deletionRequest, deletionDate);
        }
        #endregion
        [TestMethod()]
        public async Task DeletingMustNotDeleteCardNotifications()
        {
            var options = DbHelper.GetEmptyTestDB();
            var user1 = await CreateUserAsync(options);
            var card = await CreateCardAsync(options, user1.Id, new DateTime(2020, 11, 1));
            await CreateCardNotificationAsync(options, user1.Id, card.Id);
            using (var dbContext = new MemCheckDbContext(options))
                Assert.AreEqual(1, dbContext.CardNotifications.Count());
            await DeleteCardAsync(options, user1.Id, card.Id, new DateTime(2020, 11, 2));
            using (var dbContext = new MemCheckDbContext(options))
                Assert.AreEqual(1, dbContext.CardNotifications.Count());
        }
    }
}
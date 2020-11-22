using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using MemCheck.Database;
using System.Linq;
using System;
using MemCheck.Application.Notifying;
using MemCheck.Domain;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Localization;

namespace MemCheck.Application.Tests
{
    [TestClass()]
    public class DeleteCardTests
    {
        #region private sealed class EmptyLocalizer
        private sealed class EmptyLocalizer : IStringLocalizer
        {
            public LocalizedString this[string name] => new LocalizedString(name, "");
            public LocalizedString this[string name, params object[] arguments] => new LocalizedString(name, "");
            public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            {
                return new LocalizedString[0];
            }
        }
        #endregion
        #region Private methods
        private static string RandomString()
        {
            return Guid.NewGuid().ToString();
        }
        private DbContextOptions<MemCheckDbContext> DbContextOptions()
        {
            var connectionString = @$"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog={GetType().Name};Integrated Security=True;Connect Timeout=60;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
            var result = new DbContextOptionsBuilder<MemCheckDbContext>().UseSqlServer(connectionString).Options;
            using (var dbContext = new MemCheckDbContext(result))
            {
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();
            }
            return result;
        }
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
            var notif = new CardNotification();
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
            result.FrontSide = RandomString();
            result.BackSide = RandomString();
            result.AdditionalInfo = RandomString();
            result.VersionDescription = RandomString();
            result.VersionType = CardVersionType.Creation;
            dbContext.Cards.Add(result);
            await dbContext.SaveChangesAsync();
            return result;
        }
        private async Task DeleteCardAsync(DbContextOptions<MemCheckDbContext> db, Guid userId, Guid cardId, DateTime deletionDate)
        {
            using var dbContext = new MemCheckDbContext(db);
            var deleter = new DeleteCards(dbContext, new EmptyLocalizer());
            var deletionRequest = new DeleteCards.Request(dbContext.Users.Where(u => u.Id == userId).Single(), new[] { cardId });
            await deleter.RunAsync(deletionRequest, deletionDate);
        }
        #endregion
        [TestMethod()]
        public async Task DeletingMustNotDeleteCardNotifications()
        {
            var options = DbContextOptions();
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
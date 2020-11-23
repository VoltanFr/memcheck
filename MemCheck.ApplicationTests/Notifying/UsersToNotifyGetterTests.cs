using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using MemCheck.Database;
using System.Linq;
using System;
using MemCheck.Application.Notifying;
using MemCheck.Domain;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MemCheck.Application.Tests.Notifying
{
    [TestClass()]
    public class UsersToNotifyGetterTests
    {
        #region Private methods
        private DbContextOptions<MemCheckDbContext> OptionsForNewDB()
        {
            return new DbContextOptionsBuilder<MemCheckDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        }
        private async Task<MemCheckUser> CreateUserAsync(DbContextOptions<MemCheckDbContext> db)
        {
            using var dbContext = new MemCheckDbContext(db);
            var result = new MemCheckUser();
            dbContext.Users.Add(result);
            await dbContext.SaveChangesAsync();
            return result;
        }
        private async Task<Card> CreateCardAsync(DbContextOptions<MemCheckDbContext> db, MemCheckUser versionCreator)
        {
            using var dbContext = new MemCheckDbContext(db);
            var creator = await dbContext.Users.Where(u => u.Id == versionCreator.Id).SingleAsync();
            var result = new Card();
            result.VersionCreator = creator;
            dbContext.Cards.Add(result);
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
        #endregion
        [TestMethod()]
        public void TestRun_EmptyDB()
        {
            var options = OptionsForNewDB();

            using (var dbContext = new MemCheckDbContext(options))
            {
                var getter = new UsersToNotifyGetter(dbContext);
                var users = getter.Run();
                Assert.AreEqual(0, users.Length);
            }
        }
        [TestMethod()]
        public async Task TestRun_DBWithUsersAndCardsButNoNotification()
        {
            var options = OptionsForNewDB();
            var user1 = await CreateUserAsync(options);
            await CreateCardAsync(options, user1);

            using (var dbContext = new MemCheckDbContext(options))
            {
                var getter = new UsersToNotifyGetter(dbContext);
                var users = getter.Run();
                Assert.AreEqual(0, users.Length);
            }
        }
        [TestMethod()]
        public async Task TestRun_DBWithOneUserWithOneNotification()
        {
            var options = OptionsForNewDB();
            var user1 = await CreateUserAsync(options);
            var user2 = await CreateUserAsync(options);
            var user3 = await CreateUserAsync(options);
            var card1 = await CreateCardAsync(options, user1);
            await CreateCardAsync(options, user1);
            await CreateCardAsync(options, user2);
            await CreateCardNotificationAsync(options, user3.Id, card1.Id);

            using (var dbContext = new MemCheckDbContext(options))
            {
                var getter = new UsersToNotifyGetter(dbContext);
                var users = getter.Run();
                Assert.AreEqual(1, users.Length);
                Assert.AreEqual(user3.Id, users[0].Id);
            }
        }
        [TestMethod()]
        public async Task TestRun_DBWithNotifications()
        {
            var options = OptionsForNewDB();
            var user1 = await CreateUserAsync(options);
            var user2 = await CreateUserAsync(options);
            var user3 = await CreateUserAsync(options);
            var card1 = await CreateCardAsync(options, user1);
            var card2 = await CreateCardAsync(options, user1);
            var card3 = await CreateCardAsync(options, user2);
            await CreateCardNotificationAsync(options, user3.Id, card1.Id);
            await CreateCardNotificationAsync(options, user3.Id, card2.Id);
            await CreateCardNotificationAsync(options, user3.Id, card3.Id);
            await CreateCardNotificationAsync(options, user1.Id, card2.Id);

            using (var dbContext = new MemCheckDbContext(options))
            {
                var getter = new UsersToNotifyGetter(dbContext);
                var users = getter.Run();
                Assert.AreEqual(2, users.Length);
                Assert.IsTrue(users.Any(u => u.Id == user3.Id));
                Assert.IsTrue(users.Any(u => u.Id == user1.Id));
            }
        }
    }
}
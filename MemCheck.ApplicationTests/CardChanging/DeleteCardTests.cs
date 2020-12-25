using Microsoft.VisualStudio.TestTools.UnitTesting;
using MemCheck.Database;
using System.Linq;
using System;
using System.Threading.Tasks;
using MemCheck.Application.Tests.Helpers;

namespace MemCheck.Application.CardChanging
{
    [TestClass()]
    public class DeleteCardTests
    {
        [TestMethod()]
        public async Task DeletingMustNotDeleteCardNotifications()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var card = await CardHelper.CreateAsync(db, user, new DateTime(2020, 11, 1));
            await CardSubscriptionHelper.CreateAsync(db, user, card.Id);

            using (var dbContext = new MemCheckDbContext(db))
                Assert.AreEqual(1, dbContext.CardNotifications.Count());

            await CardDeletionHelper.DeleteCardAsync(db, user, card.Id, new DateTime(2020, 11, 2));

            using (var dbContext = new MemCheckDbContext(db))
                Assert.AreEqual(1, dbContext.CardNotifications.Count());
        }
    }
}
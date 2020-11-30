using Microsoft.VisualStudio.TestTools.UnitTesting;
using MemCheck.Database;
using MemCheck.Application.Notifying;
using System.Threading.Tasks;
using System;
using Microsoft.EntityFrameworkCore;
using MemCheck.Domain;

namespace MemCheck.Application.Tests.Notifying
{
    [TestClass()]
    public class AddCardSubscriptionsTest
    {
        [TestMethod()]
        public async Task TestRun()
        {
            var testDB = DbServices.GetEmptyTestDB(typeof(AddCardSubscriptionsTest));

            var card = await CardHelper.CreateAsync(testDB, await UserHelper.CreateInDbAsync(testDB));
            var otherUserId = await UserHelper.CreateInDbAsync(testDB);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new AddCardSubscriptions.Request(otherUserId, new Guid[] { card.Id });
                await new AddCardSubscriptions(dbContext).RunAsync(request);
            }

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var subscription = await dbContext.CardNotifications.SingleAsync(notif => notif.UserId == otherUserId && notif.CardId == card.Id);
                Assert.AreEqual(CardNotificationSubscription.CardNotificationRegistrationMethod_ExplicitByUser, subscription.RegistrationMethod);
            }
        }
        [TestMethod()]
        public async Task TestRun_UserNotAllowedBecauseHasNoVisibility()
        {
            var testDB = DbServices.GetEmptyTestDB(typeof(AddCardSubscriptionsTest));

            var cardCreatorId = await UserHelper.CreateInDbAsync(testDB);
            var card = await CardHelper.CreateAsync(testDB, cardCreatorId, userWithViewIds: new Guid[] { cardCreatorId });

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var request = new AddCardSubscriptions.Request(await UserHelper.CreateInDbAsync(testDB), new Guid[] { card.Id });
                await Assert.ThrowsExceptionAsync<ApplicationException>(async () => await new AddCardSubscriptions(dbContext).RunAsync(request));
            }
        }
    }
}